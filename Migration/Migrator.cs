using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Haukcode.Migration
{
    public class Migrator
    {
        private const string EmptyChangeScriptPlaceholder = "-- REPLACE WITH CHANGE SCRIPT --";
        private readonly string baseConnectionString;
        private readonly string migrationDirectory;

        public Migrator(string baseConnectionString, string migrationDirectory)
        {
            if (baseConnectionString.Contains("Database=", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("The base connection string shouldn't include the database name");

            this.baseConnectionString = baseConnectionString;
            this.migrationDirectory = migrationDirectory;
        }

        public void InitializeTemplateDatabase(string databaseName, string createScriptFileName)
        {
            // Empty the destination database
            Console.WriteLine($"Empty the template database {databaseName}");
            ScriptExecutor.ExecuteSqlScript(this.baseConnectionString, databaseName, Scripts.DeleteAllTables_SQLServer);

            // Execute the create script
            Console.WriteLine($"Execute the create script {Path.GetFileName(createScriptFileName)} in the template database {databaseName}");
            ScriptExecutor.ExecuteSqlScript(this.baseConnectionString, databaseName, File.ReadAllText(createScriptFileName));
        }

        private byte[] GetHashForString(string input)
        {
            using (var hasher = MD5.Create())
            {
                return hasher.ComputeHash(Encoding.UTF8.GetBytes(input));
            }
        }

        private byte[] GetHashForDataModel(Models.Database dataModel)
        {
            return GetHashForString(SchemaExtractor.GetJson(dataModel, false));
        }

        private string GetHashForDataModel2(Models.Database dataModel)
        {
            return ByteArrayToString(GetHashForString(SchemaExtractor.GetJson(dataModel, false)));
        }

        private string ByteArrayToString(byte[] ba)
        {
            return BitConverter.ToString(ba).Replace("-", "").ToLower();
        }

        public IList<string> FindMigrationPath(string currentHash, string destinationHash)
        {
            var fileNames = Directory.GetFiles(this.migrationDirectory, "From-*.sql", SearchOption.AllDirectories);

            var migrationScripts = new List<(string FromHash, string ToHash)>();
            foreach (var fileName in fileNames)
            {
                if (IsRealMigrationScript(fileName))
                {
                    string toHash = Path.GetFileName(Path.GetDirectoryName(fileName));
                    string fromHash = Path.GetFileNameWithoutExtension(fileName).Substring(5);

                    if (fromHash == toHash)
                        // Invalid
                        continue;

                    migrationScripts.Add((fromHash, toHash));
                }
            }

            // Find all valid paths
            var result = CheckPath(migrationScripts, currentHash, destinationHash, new HashSet<string>());
            var bestPath = result.OrderBy(x => x.Count).FirstOrDefault();

            if (bestPath == null || bestPath.Count == 0)
                return null;

            // Return list of filenames
            return bestPath.Select(x => $"{x.ToHash}{Path.DirectorySeparatorChar}From-{x.FromHash}.sql").ToList();
        }

        private IList<IList<(string FromHash, string ToHash)>> CheckPath(IList<(string FromHash, string ToHash)> fullList, string currentHash, string destinationHash, HashSet<string> used)
        {
            var list = new List<IList<(string FromHash, string ToHash)>>();

            foreach (var fromTo in fullList)
            {
                if (fromTo.FromHash == currentHash)
                {
                    used.Add(fromTo.FromHash);

                    // Prevent Circular reference
                    if (!used.Contains(fromTo.ToHash))
                    {
                        // Matching From
                        var sub = new List<(string FromHash, string ToHash)>();
                        list.Add(sub);
                        sub.Add(fromTo);

                        if (fromTo.ToHash == destinationHash)
                            return list;

                        var newPaths = CheckPath(fullList, fromTo.ToHash, destinationHash, used);

                        foreach (var path in newPaths)
                            sub.AddRange(path);
                    }
                }
            }

            return list;
        }

        private bool IsRealMigrationScript(string fileName)
        {
            var changeScriptLines = File.ReadAllLines(fileName);
            if (!changeScriptLines.Any())
                // Empty
                return false;

            if (changeScriptLines.Any(x => x.StartsWith("USE ", StringComparison.OrdinalIgnoreCase)))
                throw new Exception($"Script {fileName} has a USE keyword in it, not allowed!");

            return !changeScriptLines[0].StartsWith(EmptyChangeScriptPlaceholder);
        }

        public void MigrateDatabase(string databaseName, bool writeFailedMigrationSchema = true, Action<string, string> noMigrationPathAction = null)
        {
            string latestFileName = Path.Combine(this.migrationDirectory, "Latest.txt");
            if (!File.Exists(latestFileName))
                throw new ArgumentException($"Missing Latest.txt in {this.migrationDirectory}");

            string destinationHash = File.ReadAllLines(latestFileName)[0];
            Console.WriteLine($"Migrate database {databaseName} to destination hash {destinationHash}");

            Console.WriteLine($"Ensure the database {databaseName} exists (create if it doesn't)");
            ScriptExecutor.EnsureDatabaseIsCreated(this.baseConnectionString, databaseName);

            Console.WriteLine($"Extract schema from migration database {databaseName}");
            var migrationDataModel = SchemaExtractor.GenerateDataModel($"{this.baseConnectionString}; Database={databaseName}");

            string currentHash = GetHashForDataModel2(migrationDataModel);
            Console.WriteLine($"Current hash {currentHash}");

            if (currentHash == destinationHash)
            {
                Console.WriteLine("Perfect match");

                return;
            }

            // Migrate
            if (migrationDataModel.IsEmpty())
            {
                // Empty migration database, we'll just use the create script to update it

                Console.WriteLine("Empty database, executing from-scratch create script");

                // Execute the create script
                ScriptExecutor.ExecuteSqlScript(baseConnectionString, databaseName, File.ReadAllText(Path.Combine(this.migrationDirectory, destinationHash, "CreateScript.sql")));
            }
            else
            {
                // Migrate via patch scripts
                // See if we can find a path
                var migrationPath = FindMigrationPath(currentHash, destinationHash);

                if (migrationPath == null)
                {
                    Console.WriteLine("No path found to migrate the database");

                    if (noMigrationPathAction != null)
                    {
                        noMigrationPathAction(currentHash, destinationHash);

                        return;
                    }
                    else
                        throw new Exception("Unable to migrate database");
                }

                // Execute all the migration scripts
                foreach (string fileName in migrationPath)
                {
                    string changeScript = File.ReadAllText(Path.Combine(this.migrationDirectory, fileName));

                    ScriptExecutor.ExecuteSqlScript(baseConnectionString, databaseName, changeScript);
                }
            }

            // Verify
            migrationDataModel = SchemaExtractor.GenerateDataModel($"{baseConnectionString}; Database={databaseName}");

            currentHash = GetHashForDataModel2(migrationDataModel);

            if (currentHash == destinationHash)
            {
                Console.WriteLine("Perfect match after migration");

                return;
            }
            else
            {
                Console.WriteLine("Failed to update the migration database to the destination");

                if (writeFailedMigrationSchema)
                {
                    File.WriteAllText(Path.Combine(this.migrationDirectory, "___FailedMigrationSchema___.json"), SchemaExtractor.GetJson(migrationDataModel, true));
                }

                throw new Exception("Failed to update the migration database to the destination");
            }
        }

        private string CreatePlaceholderContent(Models.Database migrationDataModel, string destinationSchemaFileName)
        {
            var sb = new StringBuilder();
            sb.AppendLine(EmptyChangeScriptPlaceholder);
            sb.AppendLine();

            var diffBuilder = new InlineDiffBuilder(new Differ());

            string destinationSchema = File.ReadAllText(destinationSchemaFileName);
            var destinationDataModel = SchemaExtractor.FromJson(destinationSchema);

            var usedTables = new HashSet<string>();
            foreach (var table in destinationDataModel.Tables)
            {
                usedTables.Add(table.Name);

                var migrationTable = migrationDataModel.Tables.FirstOrDefault(x => x.Name == table.Name);

                string destinationTableSchema = SchemaExtractor.GetPrettyJson(table);

                if (migrationTable == null)
                {
                    sb.AppendLine($"*** CREATE TABLE: {table.Name} ***");
                    sb.AppendLine(destinationTableSchema);
                    sb.AppendLine();
                }
                else
                {
                    // Diff
                    var diff = diffBuilder.BuildDiffModel(SchemaExtractor.GetPrettyJson(migrationTable), destinationTableSchema, true);

                    if (diff.Lines.Any(x => x.Type != DiffPlex.DiffBuilder.Model.ChangeType.Unchanged))
                    {
                        // There are differences
                        sb.AppendLine($"*** MODIFY TABLE: {table.Name} ***");
                        foreach (var line in diff.Lines)
                        {
                            switch (line.Type)
                            {
                                case ChangeType.Inserted:
                                    sb.Append("+ ");
                                    break;

                                case ChangeType.Deleted:
                                    sb.Append("- ");
                                    break;

                                default:
                                    sb.Append("  ");
                                    break;
                            }
                            sb.AppendLine(line.Text);
                        }

                        sb.AppendLine();
                    }
                }
            }

            foreach (var table in migrationDataModel.Tables)
            {
                if (usedTables.Contains(table.Name))
                    continue;

                sb.AppendLine($"*** DELETE TABLE: {table.Name} ***");
                sb.AppendLine();
            }

            return sb.ToString();
        }

        public void CreateMigrationScripts(string templateDatabaseName, string migrationDatabaseName, string createScriptFileName)
        {
            InitializeTemplateDatabase(templateDatabaseName, createScriptFileName);

            Console.WriteLine($"Extract schema from template database {templateDatabaseName}");
            var templateDataModel = SchemaExtractor.GenerateDataModel($"{baseConnectionString}; Database={templateDatabaseName}");

            Console.WriteLine($"Extract schema from migration database {migrationDatabaseName}");
            var migrationDataModel = SchemaExtractor.GenerateDataModel($"{baseConnectionString}; Database={migrationDatabaseName}");

            string templateThumbprint = GetHashForDataModel2(templateDataModel);
            string migrationThumbprint = GetHashForDataModel2(migrationDataModel);

            // Make sure we have a copy of the create script for the destination thumbprint
            string destinationFolder = Path.Combine(this.migrationDirectory, templateThumbprint);
            Directory.CreateDirectory(destinationFolder);
            File.Copy(createScriptFileName, Path.Combine(destinationFolder, "CreateScript.sql"), true);
            File.WriteAllText(Path.Combine(destinationFolder, "DestinationSchema.json"), SchemaExtractor.GetJson(templateDataModel, true));

            // Store the head hash
            File.WriteAllText(Path.Combine(this.migrationDirectory, "Latest.txt"), templateThumbprint + Environment.NewLine);

            // Make sure we have the migration schema saved
            string migrationFolder = Path.Combine(this.migrationDirectory, migrationThumbprint);
            Directory.CreateDirectory(migrationFolder);
            File.WriteAllText(Path.Combine(migrationFolder, "DestinationSchema.json"), SchemaExtractor.GetJson(migrationDataModel, true));

            MigrateDatabase(databaseName: migrationDatabaseName, writeFailedMigrationSchema: true,
                noMigrationPathAction: (currentHash, destinationHash) =>
                {
                    // Create placeholder
                    string migrationScriptFileName = $"From-{currentHash}.sql";
                    Directory.CreateDirectory(Path.Combine(this.migrationDirectory, destinationHash));

                    Console.WriteLine($"Create placeholder file {migrationScriptFileName} in the {destinationHash} folder");

                    string content = CreatePlaceholderContent(migrationDataModel, Path.Combine(this.migrationDirectory, destinationHash, "DestinationSchema.json"));

                    File.WriteAllText(Path.Combine(this.migrationDirectory, destinationHash, migrationScriptFileName), content);
                });
        }
    }
}

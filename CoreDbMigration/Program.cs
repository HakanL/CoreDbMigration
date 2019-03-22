using Haukcode.Migration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.FileExtensions;
using Microsoft.Extensions.Configuration.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace Haukcode.CoreDbMigration
{
    public class Program
    {
        public static int Main(string[] args)
        {
            IConfiguration config = new ConfigurationBuilder()
                .SetBasePath(Environment.CurrentDirectory)
                .AddJsonFile("appsettings.json", true)
                .AddJsonFile($"appsettings.{Environment.MachineName}.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            try
            {
                Console.WriteLine("DATABASE MIGRATION");
                Console.WriteLine();

                if (!Directory.Exists(config["BaseDirectory"]))
                    throw new ArgumentException("Base directory invalid");

                Console.WriteLine($"Base directory: {Path.GetFullPath(config["BaseDirectory"])}");

                if (!Directory.Exists(Path.Combine(config["BaseDirectory"], config["MigrationDirectory"])))
                    throw new ArgumentException("Migration directory invalid");

                var migrator = new Migrator(
                    baseConnectionString: config["BaseConnectionString"],
                    migrationDirectory: Path.Combine(config["BaseDirectory"], config["MigrationDirectory"]));

                if (args.Any(x => x.Equals("GENERATE", StringComparison.OrdinalIgnoreCase)))
                {
                    // Generate
                    migrator.CreateMigrationScripts(
                        templateDatabaseName: config["TemplateDatabaseName"],
                        migrationDatabaseName: config["MigrationDatabaseName"],
                        createScriptFileName: Path.Combine(config["BaseDirectory"], config["CreateScript"]));

                    if (!string.IsNullOrEmpty(config["ModelsDirectory"]))
                    {
                        // Generate models for the current destination database
                        Console.WriteLine($"Generate EF.Core data models in folder {config["ModelsDirectory"]} for context {config["ContextName"]} from the template database");
                        ScaffoldGenerator.ExecuteScaffold(config["BaseDirectory"], config["BaseConnectionString"], config["TemplateDatabaseName"], config["ModelsDirectory"], config["ContextName"]);
                    }
                }
                else
                {
                    // Migrate (default)
                    migrator.MigrateDatabase(config["MigrationDatabaseName"]);
                }

                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}");
                return 1;
            }
        }
    }
}

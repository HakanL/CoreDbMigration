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
using System.Threading;

namespace Haukcode.CoreDbMigration
{
    /// <summary>
    /// Main command-line application
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Execute the migration tool
        /// </summary>
        /// <param name="generate">Set to true to generate the EF data model files</param>
        /// <param name="configFile">Set to use an explicit configuration file</param>
        /// <param name="secondsToRetryConnection">How many seconds to retry a DB connection until throwing an error (0 = disabled)</param>
        /// <returns></returns>
        public static int Main(bool generate = false, FileInfo configFile = null, int secondsToRetryConnection = 0)
        {
            var configBuilder = new ConfigurationBuilder()
                .SetBasePath(Environment.CurrentDirectory);

            if (configFile != null)
                configBuilder.AddJsonFile(configFile.FullName, false);
            else
                configBuilder
                    .AddJsonFile("appsettings.json", true)
                    .AddJsonFile($"appsettings.{Environment.MachineName}.json", optional: true);

            IConfiguration config = configBuilder
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

                if (generate)
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
                    var watch = Stopwatch.StartNew();
                    while (true)
                    {
                        try
                        {
                            migrator.MigrateDatabase(config["MigrationDatabaseName"]);
                        }
                        catch (System.Data.SqlClient.SqlException ex)
                        {
                            if (secondsToRetryConnection > 0 && watch.Elapsed.TotalSeconds < secondsToRetryConnection)
                            {
                                Console.WriteLine($"Failed to connect to SQL: {ex.Message}, retrying");

                                Thread.Sleep(2000);
                                continue;
                            }

                            throw;
                        }

                        break;
                    }
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

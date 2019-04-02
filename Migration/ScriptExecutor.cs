using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Haukcode.Migration
{
    public static class ScriptExecutor
    {
        public static void EnsureDatabaseIsCreated(string connectionString, string database)
        {
            var optionsBuilder = new DbContextOptionsBuilder();
            optionsBuilder.UseSqlServer($"{connectionString}; Database={database}");
            using (var context = new DbContext(optionsBuilder.Options))
            {
                context.Database.EnsureCreated();
            }
        }

        public static void ExecuteSqlScript(string connectionString, string database, string script)
        {
            using (var conn = new System.Data.SqlClient.SqlConnection($"{connectionString}; Database={database}"))
            {
                conn.Open();

                var buf = new StringBuilder();
                using (var sr = new StringReader(script))
                {
                    string line;
                    while (true)
                    {
                        line = sr.ReadLine();

                        bool execute = line == null;
                        if (line != null && line.Equals("GO", StringComparison.OrdinalIgnoreCase))
                            execute = true;
                        else
                        {
                            if (line != null)
                                buf.AppendLine(line);
                        }

                        if (execute && buf.Length > 0)
                        {
                            using (var command = new System.Data.SqlClient.SqlCommand(buf.ToString(), conn))
                            {
                                command.ExecuteNonQuery();
                            }

                            buf.Clear();
                        }

                        if (line == null)
                            break;
                    }
                }
            }
        }
    }
}

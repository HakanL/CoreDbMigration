using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Scaffolding;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Haukcode.Migration
{
    public static class SchemaExtractor
    {
        public static Models.Database GenerateDataModel(string connectionString)
        {
#pragma warning disable EF1001 // Internal EF Core API usage.
            var loggerFactory = new NullLoggerFactory();
            var loggingOptions = new Microsoft.EntityFrameworkCore.Diagnostics.Internal.LoggingOptions();
            var loggerSource = new System.Diagnostics.DiagnosticListener(string.Empty);
            var loggingDefinitions = new Microsoft.EntityFrameworkCore.SqlServer.Diagnostics.Internal.SqlServerLoggingDefinitions();
            var contextLogger = new Microsoft.EntityFrameworkCore.Diagnostics.Internal.NullDbContextLogger();
            var logger = new Microsoft.EntityFrameworkCore.Diagnostics.Internal.DiagnosticsLogger<DbLoggerCategory.Scaffolding>(loggerFactory, loggingOptions, loggerSource, loggingDefinitions, contextLogger);
            var factory = new Microsoft.EntityFrameworkCore.SqlServer.Scaffolding.Internal.SqlServerDatabaseModelFactory(logger);
            var dbFactoryOptions = new DatabaseModelFactoryOptions();
            var dataModel = factory.Create(connectionString, dbFactoryOptions);
#pragma warning restore EF1001 // Internal EF Core API usage.

            var model = new Models.Database(dataModel);

            return model;
        }

        public static Models.Database FromJson(string input)
        {
            return JsonConvert.DeserializeObject<Models.Database>(input, new Newtonsoft.Json.Converters.StringEnumConverter());
        }

        public static string GetJson(Models.Database dataModel, bool pretty)
        {
            var jsonSettings = new JsonSerializerSettings
            {
                Converters = new List<JsonConverter>()
                {
                    new Newtonsoft.Json.Converters.StringEnumConverter()
                },
                NullValueHandling = NullValueHandling.Ignore,
                Formatting = pretty ? Formatting.Indented : Formatting.None
            };

            return JsonConvert.SerializeObject(dataModel, jsonSettings);
        }

        public static string GetPrettyJson(Models.Table dataModel)
        {
            var jsonSettings = new JsonSerializerSettings
            {
                Converters = new List<JsonConverter>()
                {
                    new Newtonsoft.Json.Converters.StringEnumConverter()
                },
                NullValueHandling = NullValueHandling.Ignore,
                Formatting = Formatting.Indented
            };

            return JsonConvert.SerializeObject(dataModel, jsonSettings);
        }
    }
}

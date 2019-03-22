using Microsoft.EntityFrameworkCore;
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
            var loggerFactory = new NullLoggerFactory();
            var loggingOptions = new Microsoft.EntityFrameworkCore.Internal.LoggingOptions();
            var loggerSource = new System.Diagnostics.DiagnosticListener(string.Empty);
            var logger = new Microsoft.EntityFrameworkCore.Internal.DiagnosticsLogger<DbLoggerCategory.Scaffolding>(loggerFactory, loggingOptions, loggerSource);
            var factory = new Microsoft.EntityFrameworkCore.SqlServer.Scaffolding.Internal.SqlServerDatabaseModelFactory(logger);
            var dataModel = factory.Create(connectionString, new string[0], new string[0]);

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

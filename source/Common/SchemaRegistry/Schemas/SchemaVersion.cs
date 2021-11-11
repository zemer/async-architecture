using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;

namespace Common.SchemaRegistry.Schemas
{
    internal static class SchemaVersion
    {
        public static JSchema Generate(JSchema dataSchema)
        {
            var schema = new JSchema
            {
                Type = JSchemaType.Object,
                Properties =
                {
                    { "eventId", new JSchema { Type = JSchemaType.String } },
                    { "eventVersion", new JSchema { Type = JSchemaType.Integer, Enum = { 1 } } },
                    { "eventName", new JSchema { Type = JSchemaType.String } },
                    { "eventTime", new JSchema { Type = JSchemaType.String } },
                    { "producer", new JSchema { Type = JSchemaType.String } },
                    { "data", dataSchema }
                },
                ExtensionData =
                {
                    ["references"] = new JObject
                    {
                        ["data"] = dataSchema
                    }
                },
                Required = { "eventId", "eventVersion", "eventName", "eventTime", "producer", "data" }
            };

            return schema;
        }
    }
}
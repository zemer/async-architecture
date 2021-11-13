using Newtonsoft.Json.Schema;

namespace Common.SchemaRegistry.Schemas.Tasks
{
    internal static class TasksStream1Schema
    {
        public static JSchema Generate()
        {
            var dataSchema = new JSchema
            {
                Type = JSchemaType.Object,
                Properties =
                {
                    { "taskId", new JSchema { Type = JSchemaType.String } },
                    { "description", new JSchema { Type = JSchemaType.String } },
                    { "jiraId", new JSchema { Type = JSchemaType.String | JSchemaType.Null } }
                },
                Required = { "taskId", "description" }
            };

            return SchemaVersion.Generate(dataSchema);
        }
    }
}
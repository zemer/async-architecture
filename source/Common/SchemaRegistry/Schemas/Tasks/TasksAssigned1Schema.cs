using Newtonsoft.Json.Schema;

namespace Common.SchemaRegistry.Schemas.Tasks
{
    internal static class TasksAssigned1Schema
    {
        public static JSchema Generate()
        {
            var dataSchema = new JSchema
            {
                Type = JSchemaType.Object,
                Properties =
                {
                    { "taskId", new JSchema { Type = JSchemaType.String } },
                    { "accountId", new JSchema { Type = JSchemaType.String } }
                },
                Required = { "taskId", "accountId" }
            };

            return SchemaVersion.Generate(dataSchema);
        }
    }
}
using Newtonsoft.Json.Schema;

namespace Common.SchemaRegistry.Schemas.Accounting
{
    internal static class AccountingCosts1Schema
    {
        public static JSchema Generate()
        {
            var dataSchema = new JSchema
            {
                Type = JSchemaType.Object,
                Properties =
                {
                    { "taskId", new JSchema { Type = JSchemaType.String } },
                    { "assignCost", new JSchema { Type = JSchemaType.Number } },
                    { "completeCost", new JSchema { Type = JSchemaType.Number } }
                },
                Required = { "taskId", "assignCost", "completeCost" }
            };

            return SchemaVersion.Generate(dataSchema);
        }
    }
}
using Newtonsoft.Json.Schema;

namespace Common.SchemaRegistry.Schemas.Transactions
{
    internal static class TransactionsStream1Schema
    {
        public static JSchema Generate()
        {
            var dataSchema = new JSchema
            {
                Type = JSchemaType.Object,
                Properties =
                {
                    { "transactionId", new JSchema { Type = JSchemaType.String } },
                    { "accountId", new JSchema { Type = JSchemaType.String } },
                    { "taskId", new JSchema { Type = JSchemaType.String } },
                    { "accrued", new JSchema { Type = JSchemaType.Number } },
                    { "writtenOff", new JSchema { Type = JSchemaType.Number } }
                },
                Required = { "transactionId", "accountId", "taskId", "accrued", "writtenOff" }
            };

            return SchemaVersion.Generate(dataSchema);
        }
    }
}
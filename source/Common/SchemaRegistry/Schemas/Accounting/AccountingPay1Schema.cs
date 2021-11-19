using Newtonsoft.Json.Schema;

namespace Common.SchemaRegistry.Schemas.Accounting
{
    internal static class AccountingPay1Schema
    {
        public static JSchema Generate()
        {
            var dataSchema = new JSchema
            {
                Type = JSchemaType.Object,
                Properties =
                {
                    { "accountId", new JSchema { Type = JSchemaType.String } },
                    { "fee", new JSchema { Type = JSchemaType.Number } }
                },
                Required = { "accountId", "fee" }
            };

            return SchemaVersion.Generate(dataSchema);
        }
    }
}
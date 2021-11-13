using Newtonsoft.Json.Schema;

namespace Common.SchemaRegistry.Schemas.Accounts
{
    internal static class AccountsStream1Schema
    {
        public static JSchema Generate()
        {
            var dataSchema = new JSchema
            {
                Type = JSchemaType.Object,
                Properties =
                {
                    { "accountId", new JSchema { Type = JSchemaType.String } },
                    { "username", new JSchema { Type = JSchemaType.String } },
                    { "email", new JSchema { Type = JSchemaType.String } },
                    { "role", new JSchema { Type = JSchemaType.String | JSchemaType.Null } }
                },
                Required = { "accountId", "username", "email", "role" }
            };

            return SchemaVersion.Generate(dataSchema);
        }
    }
}
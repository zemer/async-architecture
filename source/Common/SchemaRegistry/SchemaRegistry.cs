using System.Collections.Generic;
using Common.SchemaRegistry.Schemas.Accounting;
using Common.SchemaRegistry.Schemas.Accounts;
using Common.SchemaRegistry.Schemas.Tasks;
using Common.SchemaRegistry.Schemas.Transactions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;

namespace Common.SchemaRegistry
{
    public interface ISchemaRegistry
    {
        bool Validate(object data, string schema);

        bool Validate(string message, string schema);
    }

    public class SchemaRegistry : ISchemaRegistry
    {
        private IDictionary<string, JSchema> SchemaMap =>
            new Dictionary<string, JSchema>
            {
                { Schemas.Tasks.Stream.V1, TasksStream1Schema.Generate() },
                { Schemas.Tasks.Assigned.V1, TasksAssigned1Schema.Generate() },
                { Schemas.Tasks.Completed.V1, TasksCompleted1Schema.Generate() },
                { Schemas.Accounts.Stream.V1, AccountsStream1Schema.Generate() },
                { Schemas.Transactions.Stream.V1, TransactionsStream1Schema.Generate() },
                { Schemas.Accounting.Pay.V1, AccountingPay1Schema.Generate() }
            };

        #region Implementation of ISchemaRegistry

        public bool Validate(object data, string schema)
        {
            if (SchemaMap.TryGetValue(schema, out var jSchema))
            {
                var message = JsonConvert.SerializeObject(data);
                var jData = JObject.Parse(message);

                var result = jData.IsValid(jSchema, out IList<string> messages);

                return result;
            }

            return false;
        }

        public bool Validate(string message, string schema)
        {
            if (SchemaMap.TryGetValue(schema, out var jSchema))
            {
                var jData = JObject.Parse(message);

                var result = jData.IsValid(jSchema, out IList<string> messages);

                return result;
            }

            return false;
        }

        #endregion

        public static class Schemas
        {
            public static class Tasks
            {
                public static class Stream
                {
                    public const string V1 = "Tasks.Stream.1";
                }

                public static class Assigned
                {
                    public const string V1 = "Tasks.Assigned.1";
                }

                public static class Completed
                {
                    public const string V1 = "Tasks.Completed.1";
                }
            }

            public static class Accounts
            {
                public static class Stream
                {
                    public const string V1 = "Accounts.Stream.1";
                }
            }

            public static class Accounting
            {
                public static class Pay
                {
                    public const string V1 = "Accounting.Pay.1";
                }
            }

            public static class Transactions
            {
                public static class Stream
                {
                    public const string V1 = "Transactions.Stream.1";
                }
            }
        }
    }
}
using System;
using Accounting.Billing;
using Accounting.Context;
using Common.MessageBroker;
using Common.SchemaRegistry;

namespace Accounting.Services
{
    public interface ITasksService
    {
        Task CreateTask(string publicId, string description = "", string jiraId = "");
    }

    public class TasksService : ITasksService
    {
        private readonly DataContext _context;
        private readonly ICostCalculator _costCalculator;
        private readonly ISchemaRegistry _schemaRegistry;
        private readonly IMessageBrokerProducer _messageBrokerProducer;

        public TasksService(DataContext context, ICostCalculator costCalculator, ISchemaRegistry schemaRegistry,
            IMessageBrokerProducer messageBrokerProducer)
        {
            _context = context;
            _costCalculator = costCalculator;
            _schemaRegistry = schemaRegistry;
            _messageBrokerProducer = messageBrokerProducer;
        }

        public Task CreateTask(string publicId, string description, string jiraId)
        {
            var task = new Task
            {
                PublicId = publicId,
                Description = description,
                JiraId = jiraId,
                AssignCost = _costCalculator.GetAssignCost(),
                CompleteCost = _costCalculator.GetCompleteCost()
            };

            _context.Tasks.Add(task);
            _context.SaveChanges();

            ProduceTaskCosts(task);

            return task;
        }

        private void ProduceTaskCosts(Task task)
        {
            var data = new
            {
                eventId = Guid.NewGuid()
                              .ToString(),
                eventVersion = 1,
                eventName = "Accounting.Costs",
                eventTime = DateTimeOffset.Now.ToString(),
                producer = "Accounting",
                data = new
                {
                    taskId = task.PublicId,
                    assignCost = task.AssignCost,
                    completeCost = task.CompleteCost
                }
            };

            if (_schemaRegistry.Validate(data, SchemaRegistry.Schemas.Accounting.Costs.V1))
            {
                _messageBrokerProducer.Produce("accounting-costs", data);
            }
        }
    }
}
using System;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Common;
using Microsoft.AspNetCore.Mvc;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace ReisAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ReizenController : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> Post()
        {
            var connectionFactory = new ConnectionFactory() { HostName = "rabbitmq" };
            IConnection connection = null!;
            await Policy
                .Handle<BrokerUnreachableException>()
                .WaitAndRetryAsync(10, i => TimeSpan.FromSeconds(1))
                .ExecuteAsync(async () => { connection = connectionFactory.CreateConnection(); });
            
            using (connection)
            {
                using var channel = connection.CreateModel();
                channel.ExchangeDeclare("saga", ExchangeType.Topic, true);
                Message message = new() 
                {
                    Id = Guid.NewGuid().ToString()
                };
                channel.BasicPublish("saga", "saga.ticketservice.execute", null,
                    Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message)));
                Console.WriteLine($"Started transaction {message.Id}");
            }

            return Ok();
        }
    }
}
using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Common;
using MongoDB.Driver;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

MongoClient dbClient = new MongoClient("mongodb://ticketservice.db:27017");

var database = dbClient.GetDatabase("TicketService");
var collection = database.GetCollection<Message>("tickets");

var connectionFactory = new ConnectionFactory() { HostName = "rabbitmq" };
IConnection connection = null!;
await Policy
    .Handle<BrokerUnreachableException>()
    .WaitAndRetryAsync(10, i => TimeSpan.FromSeconds(1))
    .ExecuteAsync(async () => { connection = connectionFactory.CreateConnection(); });

var channel = connection.CreateModel();
var queue = channel.QueueDeclare("ticketservicequeue", true);
channel.ExchangeDeclare("saga", ExchangeType.Topic, true);
channel.QueueBind(queue.QueueName, "saga", "saga.ticketservice.*");

var consumer = new EventingBasicConsumer(channel);

consumer.Received += async (sender, eventArgs) =>
{
    var received = Encoding.UTF8.GetString(eventArgs.Body.ToArray());
    Message? parsed = JsonSerializer.Deserialize<Message>(received);
    try
    {
        if (eventArgs.RoutingKey.Contains("execute")) await Execute(parsed!);
        else if (eventArgs.RoutingKey.Contains("compensate")) await Compensate(parsed!);
    }
    catch
    {
        await Policy
            .Handle<Exception>()
            .WaitAndRetryForeverAsync(i => TimeSpan.FromSeconds(1))
            .ExecuteAsync(async () => await Compensate(parsed!));
    }
};


channel.BasicConsume(consumer, queue, true);

Thread.Sleep(Timeout.Infinite);

async Task Execute(Message message)
{
    Console.WriteLine($"Started executing TicketService for {message.Id}");
    await collection.InsertOneAsync(message);
    Console.WriteLine($"Executed TicketService for {message.Id}");
    Next(message);
}

void Next(Message message)
{
    channel.BasicPublish("saga", "saga.huurautoservice.execute", null,
        Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message)));
}

async Task Compensate(Message message)
{
    Console.WriteLine($"Started compensating TicketService for {message.Id}");
    await collection.DeleteOneAsync(document => document.Id == message.Id);
    Console.WriteLine($"Compensated TicketService for {message.Id}");
    Previous(message);
}

void Previous(Message message)
{
    Console.WriteLine($"Compensated transaction for {message.Id} successfully.");
}
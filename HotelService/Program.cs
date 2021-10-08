using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

var connectionFactory = new ConnectionFactory() { HostName = "rabbitmq" };
IConnection connection = null!;
await Policy
    .Handle<BrokerUnreachableException>()
    .WaitAndRetryAsync(10, i => TimeSpan.FromSeconds(1))
    .ExecuteAsync(async () => { connection = connectionFactory.CreateConnection(); });

var channel = connection.CreateModel();
var queue = channel.QueueDeclare("hotelservicequeue", true);
channel.ExchangeDeclare("saga", ExchangeType.Topic, true);
channel.QueueBind(queue.QueueName, "saga", "saga.hotelservice.*");

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
    Console.WriteLine($"Started executing HotelService for {message.Id}");
    await Task.Delay(1000);
    var rand = new Random();
    if (rand.Next(10) < 7) throw new Exception();
    Console.WriteLine($"Executed HotelService for {message.Id}");
    Next(message);
}

void Next(Message message)
{
    Console.WriteLine($"Completed transaction {message.Id}");
}

async Task Compensate(Message message)
{
    Console.WriteLine($"Started compensating HotelService for {message.Id}");
    await Task.Delay(1000);
    Console.WriteLine($"Compensated HotelService for {message.Id}");
    Previous(message);
}

void Previous(Message message)
{
    channel.BasicPublish("saga", "saga.huurautoservice.compensate", null,
        Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message)));
}
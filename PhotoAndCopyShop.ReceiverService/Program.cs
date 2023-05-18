using Microsoft.Extensions.Configuration;
using PhotoAndCopyShop.ReceiverService;

IConfiguration configuration = new ConfigurationBuilder()
    .AddJsonFile("config.json")
    .Build();

string folderPath = configuration["FolderPath"] ?? Environment.CurrentDirectory;
string connectionString = configuration.GetConnectionString("ServiceBus");
string queueName = configuration["ServiceBus:QueueName"];

Console.WriteLine("Receiver daemon is running...");
Console.WriteLine("Press Ctrl+C to stop the receiver...");

ReceiverDaemon daemon = new(connectionString, queueName, folderPath);
await daemon.StartReceivingJobAsync();
using Microsoft.Extensions.Configuration;
using PhotoAndCopyShop.ScanWorker;
using PhotoAndCopyShop.ScanWorker.Handlers;

IConfiguration configuration = new ConfigurationBuilder()
    .AddJsonFile("config.json")
    .Build();

string folderPath = configuration["FolderPath"] ?? Environment.CurrentDirectory;
string connectionString = configuration.GetConnectionString("ServiceBus");
string queueName = configuration["ServiceBus:QueueName"];

Console.WriteLine("File scanner daemon is running...");
Console.WriteLine("Press Ctrl+C to stop the file scanner...");

var printFileInfoHandler = new PrintFileInfoHandler();
var sendFileToAzureServiceBusHandler = new SendToServiceBusHandler(connectionString, queueName);

printFileInfoHandler.SetNext(sendFileToAzureServiceBusHandler);

var fileScanner = new FolderScannerDaemon(folderPath, printFileInfoHandler);
await fileScanner.ScanAsync();
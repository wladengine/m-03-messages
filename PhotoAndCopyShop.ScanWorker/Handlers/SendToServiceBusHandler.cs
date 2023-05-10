using Azure.Messaging.ServiceBus;

namespace PhotoAndCopyShop.ScanWorker.Handlers;

public class SendToServiceBusHandler : HandlerBase<FileInfo>
{
    private readonly string _connectionString;
    private readonly string _queueName;
    private const int MaxChunkSize = 250_000;

    public SendToServiceBusHandler(string connectionString, string queueName)
    {
        _connectionString = connectionString;
        _queueName = queueName;
    }

    public override async Task HandleAsync(FileInfo fileInfo)
    {
        // Create a Service Bus client using the connection string
        await using ServiceBusClient client = new(_connectionString);
        // Create a sender for the queue
        ServiceBusSender sender = client.CreateSender(_queueName);
        try
        {
            // Create a message with the specified body
            byte[] fileData = await File.ReadAllBytesAsync(fileInfo.FullName);
            List<byte[]> chunks = GetChunkedFileData(fileData);

            for (var i = 0; i < chunks.Count; i++)
            {
                var message = new ServiceBusMessage(body: chunks[i]);
                message.ApplicationProperties.Add("fileName", fileInfo.Name);
                message.ApplicationProperties.Add("fileExtenstion", fileInfo.Extension);
                message.ApplicationProperties.Add("fileSize", fileInfo.Length);
                if (chunks.Count > 1)
                {
                    message.ApplicationProperties.Add("fileOffset", (long)i * MaxChunkSize);
                    message.ApplicationProperties.Add("isLastPart", i == chunks.Count - 1);
                }

                // Send the message to the queue
                await sender.SendMessageAsync(message);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

        await base.HandleAsync(fileInfo);
    }

    private static List<byte[]> GetChunkedFileData(byte[] data)
    {
        if (data.Length <= MaxChunkSize)
        {
            return new List<byte[]>(1) { data };
        }

        int capacity = data.Length / MaxChunkSize + 1;
        var list = new List<byte[]>(capacity);
        for (var i = 0; i < capacity; i++)
        {
            int firstByte = i * MaxChunkSize;
            int lastByte = (i + 1) * MaxChunkSize;
            if (lastByte >= data.Length)
            {
                lastByte = data.Length;
            }
            list.Add(data[firstByte..lastByte]);
        }

        return list;
    }
}
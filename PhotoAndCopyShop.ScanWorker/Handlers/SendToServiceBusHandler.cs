using Azure.Messaging.ServiceBus;
using System;

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
            await using FileStream fs = new(fileInfo.FullName, FileMode.Open);
            List<long> chunks = GetChunkedFileOffsets(fs.Length);
            for (var i = 0; i < chunks.Count; i++)
            {
                var chunkData = new byte[MaxChunkSize];
                fs.Seek(chunks[i], SeekOrigin.Begin);
                _ = await fs.ReadAsync(chunkData, 0, MaxChunkSize);
                var message = new ServiceBusMessage(body: chunkData);
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

    private static List<long> GetChunkedFileOffsets(long dataLength)
    {
        if (MaxChunkSize == -1)
        {
            throw new DivideByZeroException("incorrect MaxChunkSize");
        }
        if (dataLength <= MaxChunkSize)
        {
            return new List<long>(1) { dataLength };
        }

        long capacity = dataLength / MaxChunkSize + 1;
        if (capacity > int.MaxValue)
        {
            throw new OverflowException($"File too big to send");
        }

        var list = new List<long>((int)capacity);
        for (var i = 0; i < capacity; i++)
        {
            long firstByte = i * MaxChunkSize;
            list.Add(firstByte);
        }

        return list;
    }
}
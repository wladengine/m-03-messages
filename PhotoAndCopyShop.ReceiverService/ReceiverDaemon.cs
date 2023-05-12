using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;

namespace PhotoAndCopyShop.ReceiverService;

public class ReceiverDaemon
{
    private readonly string _connectionString;
    private readonly string _queueName;
    private readonly string _targetFolder;
    private readonly HashSet<string> _receivedMessages = new();

    public ReceiverDaemon(string connectionString, string queueName, string targetFolder)
    {
        _connectionString = connectionString;
        _queueName = queueName;
        _targetFolder = targetFolder;
    }

    public async Task StartReceivingJobAsync()
    {
        // Create a Service Bus client using the connection string
        await using ServiceBusClient client = new(_connectionString);
        // Create a sender for the queue
        ServiceBusReceiver receiver = client.CreateReceiver(_queueName);

        while (true)
        {
            IAsyncEnumerable<ServiceBusReceivedMessage>? messages = receiver.ReceiveMessagesAsync();
            if (messages == null)
            {
                await Task.Delay(TimeSpan.FromSeconds(10));
                continue;
            }

            await foreach (ServiceBusReceivedMessage message in messages)
            {
                if (_receivedMessages.Contains(message.MessageId))
                {
                    continue;
                }
                
                _receivedMessages.Add(message.MessageId);
                message.ApplicationProperties.TryGetValue("fileName", out object fileName);
                message.ApplicationProperties.TryGetValue("fileExtenstion", out object fileExtenstion);
                message.ApplicationProperties.TryGetValue("fileSize", out object fileSize);
                if (fileName == null || fileExtenstion == null || fileSize == null)
                {
                    continue;
                }

                string targetFileName = Path.Combine(_targetFolder, fileName.ToString());
                message.ApplicationProperties.TryGetValue("fileOffset", out object fileoffset);
                while (File.Exists(targetFileName) && fileoffset == null)
                {
                    targetFileName = Path.Combine(
                        _targetFolder,
                        $"{fileName}_{DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss")}.{fileExtenstion}");
                }

                await using FileStream fileStream = new(fileName.ToString(), FileMode.OpenOrCreate, FileAccess.Write);
                ReadOnlyMemory<byte> buffer = message.Body.ToMemory();
                if (fileoffset != null && (long)fileoffset > 0)
                {
                    fileStream.Seek((long)fileoffset, SeekOrigin.Begin);
                }
                await fileStream.WriteAsync(buffer);

                await receiver.CompleteMessageAsync(message);
            }
        }
    }
}
namespace PhotoAndCopyShop.ScanWorker.Handlers;

public class PrintFileInfoHandler : HandlerBase<FileInfo>
{
    public override async Task HandleAsync(FileInfo request)
    {
        Console.WriteLine($"File info: {request.Name} ({request.Length} bytes, created on {request.CreationTime})");
        await base.HandleAsync(request);
    }
}
using PhotoAndCopyShop.ScanWorker.Handlers;

namespace PhotoAndCopyShop.ScanWorker;

public class FolderScannerDaemon
{
    private readonly string _folderPath;
    private readonly IHandler<FileInfo> _handler;
    private readonly List<FileInfo> _scannedFiles = new();

    public FolderScannerDaemon(string folderPath, IHandler<FileInfo> handler)
    {
        _folderPath = folderPath;
        _handler = handler;
    }

    public async Task ScanAsync()
    {
        Console.WriteLine($"Scanning folder {_folderPath} for new files...");

        while (true)
        {
            string[] files = Directory.GetFiles(_folderPath);

            foreach (string file in files)
            {
                var fileInfo = new FileInfo(file);

                // check if the file was created within the last minute
                if (DateTime.Now - fileInfo.CreationTime < TimeSpan.FromMinutes(1) && !_scannedFiles.Contains(fileInfo))
                {
                    await _handler?.HandleAsync(fileInfo);
                    _scannedFiles.Add(fileInfo);
                }
            }

            await Task.Delay(TimeSpan.FromSeconds(10));
        }
    }
}
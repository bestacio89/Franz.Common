namespace Franz.Common.Http.Client.Files;

public sealed class FileParameter : IDisposable
{
    public string Name { get; }

    public Stream Stream { get; }

    public string MimeType { get; }

    public FileParameter(string name, Stream stream, string mimeType = "application/octet-stream")
    {
        Name = name;
        MimeType = mimeType;

        Stream = new MemoryStream();
        stream.CopyTo(Stream);
        if (Stream.CanSeek)
            Stream.Seek(0, SeekOrigin.Begin);
    }

    public void Dispose()
    {
        if (Stream != null)
            Stream.Dispose();
    }
}

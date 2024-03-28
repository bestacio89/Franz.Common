namespace Franz.Common.IO;

public class DeleteTemporaryFileAfterReadingStream : FileStream
{
    public DeleteTemporaryFileAfterReadingStream()
        : base(Path.GetTempFileName(), FileMode.Create)
    {
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing && File.Exists(Name))
            File.Delete(Name);
    }
}

namespace FlyEngine.Core.Extensions;

public static class StreamExtensions
{
    public static byte[] StreamToByteArray(this Stream stream)
    {
        using var memoryStream = new MemoryStream();
        stream.CopyTo(memoryStream);
        return memoryStream.ToArray();
    }
}
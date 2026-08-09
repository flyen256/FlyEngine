namespace FlyEngine.Core.Serialization;

public class ProgressStream(Stream innerStream, Action<double> onProgressChanged, bool isWriteMode = false)
    : Stream
{
    private readonly Stream _innerStream = innerStream ?? throw new ArgumentNullException(nameof(innerStream));
    private readonly long _totalLength = isWriteMode ? 0 : innerStream.Length;
    private long _bytesProcessed;

    private void UpdateProgress(int bytesCount)
    {
        if (bytesCount <= 0) return;
        
        _bytesProcessed += bytesCount;

        switch (isWriteMode)
        {
            case false when _totalLength > 0:
            {
                var progressPercentage = (double)_bytesProcessed / _totalLength * 100;
                onProgressChanged?.Invoke(System.Math.Min(progressPercentage, 100.0));
                break;
            }
            case true:
                onProgressChanged?.Invoke(_bytesProcessed);
                break;
        }
    }

    #region МЕТОДЫ ЧТЕНИЯ (Десериализация)

    public override int Read(byte[] buffer, int offset, int count)
    {
        var read = _innerStream.Read(buffer, offset, count);
        UpdateProgress(read);
        return read;
    }

    public override int Read(Span<byte> buffer)
    {
        var read = _innerStream.Read(buffer);
        UpdateProgress(read);
        return read;
    }

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        var read = await _innerStream.ReadAsync(buffer, offset, count, cancellationToken);
        UpdateProgress(read);
        return read;
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        var read = await _innerStream.ReadAsync(buffer, cancellationToken);
        UpdateProgress(read);
        return read;
    }

    #endregion

    #region МЕТОДЫ ЗАПИСИ (Сериализация)

    public override void Write(byte[] buffer, int offset, int count)
    {
        _innerStream.Write(buffer, offset, count);
        UpdateProgress(count);
    }

    public override void Write(ReadOnlySpan<byte> buffer)
    {
        _innerStream.Write(buffer);
        UpdateProgress(buffer.Length);
    }

    public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        await _innerStream.WriteAsync(buffer, offset, count, cancellationToken);
        UpdateProgress(count);
    }

    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        await _innerStream.WriteAsync(buffer, cancellationToken);
        UpdateProgress(buffer.Length);
    }

    #endregion

    #region СТАНДАРТНЫЕ СВОЙСТВА STREAM

    public override bool CanRead => _innerStream.CanRead;
    public override bool CanSeek => _innerStream.CanSeek;
    public override bool CanWrite => _innerStream.CanWrite;
    public override long Length => _innerStream.Length;
    public override long Position 
    { 
        get => _innerStream.Position; 
        set => _innerStream.Position = value; 
    }

    public override void Flush() => _innerStream.Flush();
    public override Task FlushAsync(CancellationToken cancellationToken) => _innerStream.FlushAsync(cancellationToken);
    public override long Seek(long offset, SeekOrigin origin) => _innerStream.Seek(offset, origin);
    public override void SetLength(long value) => _innerStream.SetLength(value);

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _innerStream.Dispose();
        base.Dispose(disposing);
    }

    #endregion
}
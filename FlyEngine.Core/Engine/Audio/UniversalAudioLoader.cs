using System.Runtime.InteropServices;
using System.Text;
using FlyEngine.Core.Debugging;
using libFLAC;
using NLayer;
using NVorbis;
using Silk.NET.OpenAL;

namespace FlyEngine.Core.Audio;

public static class NativeAudioLoader
{
    public static DecodedAudio? Load(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLower();
        DecodedAudio? audio = null;
        try
        {
            audio = extension switch
            {
                ".wav" => DecodeWav(filePath),
                ".mp3" => DecodeMp3(filePath),
                ".ogg" => DecodeOgg(filePath),
                ".flac" => DecodeFlac(filePath),
                _ => throw new NotSupportedException($"Формат {extension} не поддерживается.")
            };
        }
        catch (Exception ex)
        {
            Debug.LogError("Failed to load audio file: " + ex.Message);
        }
        return audio;
    }

    private static DecodedAudio DecodeWav(string path)
    {
        using var stream = File.OpenRead(path);
        using var reader = new BinaryReader(stream);

        if (new string(reader.ReadChars(4)) != "RIFF") throw new Exception("Не RIFF файл");
        reader.ReadInt32();
        if (new string(reader.ReadChars(4)) != "WAVE") throw new Exception("Не WAVE файл");
        if (new string(reader.ReadChars(4)) != "fmt ") throw new Exception("Не найден fmt");

        var chunkSize = reader.ReadInt32();
        reader.ReadInt16();
        int channels = reader.ReadInt16();
        var sampleRate = reader.ReadInt32();
        reader.ReadInt32(); reader.ReadInt16();
        int bitsPerSample = reader.ReadInt16();

        if (chunkSize > 16) stream.Position += chunkSize - 16;
        while (new string(reader.ReadChars(4)) != "data")
        {
            stream.Position += reader.ReadInt32();
        }

        var dataSize = reader.ReadInt32();
        var pcm = reader.ReadBytes(dataSize);

        return new DecodedAudio(pcm, GetAlFormat(channels, bitsPerSample), sampleRate);
    }

    private static DecodedAudio DecodeMp3(string path)
    {
        using var mpegStream = new MpegFile(path);
        var pcmChannels = mpegStream.Channels;
        var pcmSampleRate = mpegStream.SampleRate;

        var floatBuffer = new float[1024 * 16];
        using var memStream = new MemoryStream();
        using var writer = new BinaryWriter(memStream);

        int read;
        while ((read = mpegStream.ReadSamples(floatBuffer, 0, floatBuffer.Length)) > 0)
        {
            for (var i = 0; i < read; i++)
            {
                var sample = System.Math.Clamp(floatBuffer[i], -1.0f, 1.0f);
                var pcm16 = (short)(sample >= 0f ? sample * short.MaxValue : sample * -short.MinValue);
                writer.Write(pcm16);
            }
        }

        return new DecodedAudio(memStream.ToArray(),GetAlFormat(pcmChannels, 16), pcmSampleRate);
    }

    private static DecodedAudio DecodeOgg(string path)
    {
        using var vorbis = new VorbisReader(path);
        var floatBuffer = new float[1024 * 16];
        using var memStream = new MemoryStream();
        using var writer = new BinaryWriter(memStream);

        int read;
        while ((read = vorbis.ReadSamples(floatBuffer, 0, floatBuffer.Length)) > 0)
        {
            for (var i = 0; i < read; i++)
            {
                var sample = System.Math.Clamp(floatBuffer[i], -1.0f, 1.0f);
                var pcm16 = (short)(sample >= 0f ? sample * short.MaxValue : sample * -short.MinValue);
                writer.Write(pcm16);
            }
        }

        return new DecodedAudio(memStream.ToArray(), GetAlFormat(vorbis.Channels, 16), vorbis.SampleRate);
    }

    private static BufferFormat GetAlFormat(int channels, int bits) => (channels, bits) switch
    {
        (1, 8) => BufferFormat.Mono8,
        (1, 16) => BufferFormat.Mono16,
        (2, 8) => BufferFormat.Stereo8,
        (2, 16) => BufferFormat.Stereo16,
        _ => throw new NotSupportedException($"Channels: {channels}, bits: {bits} not supported in OpenAL.")
    };
    
    private static DecodedAudio DecodeFlac(string path)
    {
        using var inputStream = File.OpenRead(path);
        var isEof = inputStream.CanSeek && inputStream.Position >= inputStream.Length;

        Span<byte> magicBuffer = stackalloc byte[4];
        inputStream.ReadExactly(magicBuffer);

        if (Encoding.ASCII.GetString(magicBuffer) != "fLaC")
            throw new Exception("Magic does not match");

        var flacDecoder = NativeMethods.FLAC__stream_decoder_new();
        if (flacDecoder == IntPtr.Zero)
            throw new Exception("Failed to obtain decoder");

        NativeMethods.FLAC__stream_decoder_set_metadata_respond_all(flacDecoder);

        var readBuffer = new byte[8192];
        using var outputMemoryStream = new MemoryStream();

        var xx = NativeMethods.FLAC__stream_decoder_init_stream(
            flacDecoder, OnRead, OnSeek, OnTell, OnLength, OnEof, OnWrite, OnMeta, OnError, IntPtr.Zero);

        if (xx != StreamDecoderInitStatus.OK)
            throw new Exception("init failed");

        NativeMethods.FLAC__stream_decoder_set_metadata_respond_all(flacDecoder);

        if (!NativeMethods.FLAC__stream_decoder_process_until_end_of_metadata(flacDecoder)
            || !NativeMethods.FLAC__stream_decoder_process_single(flacDecoder))
            throw new Exception("Failed to read FLAC file: " + NativeMethods.FLAC__stream_decoder_get_state(flacDecoder));

        var sampleRate = NativeMethods.FLAC__stream_decoder_get_sample_rate(flacDecoder);
        var flacChannels = NativeMethods.FLAC__stream_decoder_get_channels(flacDecoder);
        var bitsPerSample = NativeMethods.FLAC__stream_decoder_get_bits_per_sample(flacDecoder);

        var alFormat = (flacChannels, bitsPerSample) switch
        {
            (1, 8) => BufferFormat.Mono8,
            (1, 16) => BufferFormat.Mono16,
            (2, 8) => BufferFormat.Stereo8,
            (2, 16) => BufferFormat.Stereo16,
            _ => throw new NotSupportedException($"Комбинация каналов ({flacChannels}) и битности ({bitsPerSample}) не поддерживается OpenAL.")
        };

        var result = NativeMethods.FLAC__stream_decoder_process_until_end_of_stream(flacDecoder);

        if (!result)
            throw new Exception("Failed to read FLAC file: " + NativeMethods.FLAC__stream_decoder_get_state(flacDecoder));

        NativeMethods.FLAC__stream_decoder_finish(flacDecoder);
        NativeMethods.FLAC__stream_decoder_delete(flacDecoder);

        return new DecodedAudio(outputMemoryStream.ToArray(), alFormat, sampleRate);

        StreamDecoderReadStatus OnRead(IntPtr decoder, IntPtr bufferPtr, ref UIntPtr bytes, IntPtr clientData)
        {
            var requestedBytes = (int)bytes;
            if (requestedBytes <= 0) return StreamDecoderReadStatus.Abort;

            try
            {
                if (readBuffer.Length < requestedBytes)
                    readBuffer = new byte[requestedBytes];

                var numRead = inputStream.Read(readBuffer, 0, requestedBytes);
                Marshal.Copy(readBuffer, 0, bufferPtr, numRead);
                bytes = (UIntPtr)numRead;

                if (numRead != 0) return StreamDecoderReadStatus.Continue;
                isEof = true;
                return StreamDecoderReadStatus.EndOfStream;
            }
            catch
            {
                return StreamDecoderReadStatus.Abort;
            }
        }

        StreamDecoderWriteStatus OnWrite(IntPtr decoder, Frame frame, [MarshalAs(UnmanagedType.LPArray, SizeConst = FLACConstants.MaxChannels)] IntPtr[] buffer, IntPtr clientData)
        {
            var blockSize = frame.Header.BlockSize;
            var channels = frame.Header.Channels;
            var totalSamples = blockSize * channels;

            var pcmBuffer = totalSamples <= 4096 
                ? stackalloc short[totalSamples] 
                : new short[totalSamples];

            var index = 0;
            for (var i = 0; i < blockSize; i++)
            {
                for (var c = 0; c < channels; c++)
                {
                    var sample = Marshal.ReadInt32(buffer[c], i * 4);
                    pcmBuffer[index++] = (short)sample;
                }
            }

            ReadOnlySpan<byte> byteSpan = MemoryMarshal.Cast<short, byte>(pcmBuffer);

            outputMemoryStream.Write(byteSpan);

            return StreamDecoderWriteStatus.Continue;
        }

        StreamDecoderSeekStatus OnSeek(IntPtr decoder, long absoluteByteOffset, IntPtr clientData)
        {
            if (!inputStream.CanSeek) return StreamDecoderSeekStatus.Unsupported;
            try { inputStream.Position = absoluteByteOffset; return StreamDecoderSeekStatus.OK; }
            catch { return StreamDecoderSeekStatus.Error; }
        }

        StreamDecoderTellStatus OnTell(IntPtr decoder, out long absoluteByteOffset, IntPtr clientData)
        {
            try { absoluteByteOffset = inputStream.Position; return StreamDecoderTellStatus.OK; }
            catch { absoluteByteOffset = -1; return StreamDecoderTellStatus.Error; }
        }

        StreamDecoderLengthStatus OnLength(IntPtr decoder, out long streamLength, IntPtr clientData)
        {
            try { streamLength = inputStream.Length; return StreamDecoderLengthStatus.OK; }
            catch { streamLength = -1; return StreamDecoderLengthStatus.Error; }
        }

        bool OnEof(IntPtr decoder, IntPtr clientData) => isEof;
        void OnMeta(IntPtr decoder, IntPtr metadata, IntPtr clientData) { }
        void OnError(IntPtr decoder, StreamDecoderErrorStatus status, IntPtr clientData) { }
    }
}
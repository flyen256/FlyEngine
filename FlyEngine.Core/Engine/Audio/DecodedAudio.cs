using Silk.NET.OpenAL;

namespace FlyEngine.Core.Audio;

public class DecodedAudio(byte[] pcmData, BufferFormat format, int sampleRate)
{
    public byte[] PcmData { get; set; } = pcmData;
    public BufferFormat Format { get; set; } = format;
    public int SampleRate { get; set; } = sampleRate;
}
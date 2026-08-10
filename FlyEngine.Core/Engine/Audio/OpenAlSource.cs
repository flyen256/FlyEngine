using Silk.NET.OpenAL;

namespace FlyEngine.Core.Audio;

public class OpenAlSource(AL al)
{
    private readonly uint _source = al.GenSource();

    public void Play() => al.SourcePlay(_source);
}
using System.Diagnostics.CodeAnalysis;
using FlyEngine.Core.Components;
using FlyEngine.Core.Debugging;

namespace FlyEngine.Core.Audio;

public class AudioSource : Component
{
    public bool PlayOnStartup { get; set; } = true;

    private OpenAlSource? _source;
    
    public override void OnEnable()
    {
        SetupSource();
        if (PlayOnStartup) Play();
    }

    private void SetupSource()
    {
        if (Application.Window == null || Application.Window.OpenAl == null) return;
        _source = Application.Window.OpenAl.CreateSource();
    }

    private void Play()
    {
        _source?.Play();
    }
}
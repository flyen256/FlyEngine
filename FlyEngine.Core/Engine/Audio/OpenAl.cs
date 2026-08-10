using FlyEngine.Core.Debugging;
using Silk.NET.OpenAL;

namespace FlyEngine.Core.Audio;

public unsafe class OpenAl
{
    private readonly ALContext _alc = ALContext.GetApi();
    private readonly AL _al = AL.GetApi();
    
    private Device* _device;
    private Context* _context;

    public void Initialize()
    {
        _device = _alc.OpenDevice(null);
        if (_device == null)
        {
            Debug.LogError("Failed to open audio device");
            return;
        }

        _context = _alc.CreateContext(_device, null);
        _alc.MakeContextCurrent(_context);
    }
    
    public void Dispose()
    {
        _alc.MakeContextCurrent(null);
        _alc.DestroyContext(_context);
        _alc.CloseDevice(_device);
        _alc.Dispose();
        _al.Dispose();
    }

    public OpenAlSource CreateSource() => new(_al);
}
using System.IO;
using System.Media;
using HajimaoDesktopShop.Desktop.ViewModels;

namespace HajimaoDesktopShop.Desktop.Services;

public sealed class PixelGameSoundOutput : IGameSoundOutput, IDisposable
{
    private readonly IReadOnlyDictionary<GameFeedbackKind, CachedCue> _cues;
    private bool _disposed;

    public PixelGameSoundOutput()
    {
        _cues = Enum.GetValues<GameFeedbackKind>()
            .ToDictionary(kind => kind, kind => new CachedCue(PixelGameSoundBank.CreateWave(kind)));
    }

    public void Play(GameFeedbackKind kind)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _cues[kind].Player.Play();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        foreach (var cue in _cues.Values)
        {
            cue.Dispose();
        }
    }

    private sealed class CachedCue : IDisposable
    {
        private readonly MemoryStream _stream;

        public CachedCue(byte[] wave)
        {
            _stream = new MemoryStream(wave, writable: false);
            Player = new SoundPlayer(_stream);
            Player.Load();
        }

        public SoundPlayer Player { get; }

        public void Dispose()
        {
            Player.Dispose();
            _stream.Dispose();
        }
    }
}

using System.IO;
using System.Text;
using HajimaoDesktopShop.Desktop.ViewModels;
using HajimaoDesktopShop.Rendering.PixelArt;

namespace HajimaoDesktopShop.Desktop.Services;

public static class PixelGameSoundBank
{
    private const int SampleRate = 22_050;
    private const short BitsPerSample = 16;
    private const short Channels = 1;

    public static byte[] CreateWave(GameFeedbackKind kind)
    {
        var cue = GetCue(kind);
        var sampleCount = SampleRate * cue.DurationMilliseconds / 1_000;
        var dataLength = sampleCount * sizeof(short);
        using var stream = new MemoryStream(44 + dataLength);
        using var writer = new BinaryWriter(stream, Encoding.ASCII, leaveOpen: true);

        writer.Write(Encoding.ASCII.GetBytes("RIFF"));
        writer.Write(36 + dataLength);
        writer.Write(Encoding.ASCII.GetBytes("WAVE"));
        writer.Write(Encoding.ASCII.GetBytes("fmt "));
        writer.Write(16);
        writer.Write((short)1);
        writer.Write(Channels);
        writer.Write(SampleRate);
        writer.Write(SampleRate * Channels * BitsPerSample / 8);
        writer.Write((short)(Channels * BitsPerSample / 8));
        writer.Write(BitsPerSample);
        writer.Write(Encoding.ASCII.GetBytes("data"));
        writer.Write(dataLength);

        for (var sampleIndex = 0; sampleIndex < sampleCount; sampleIndex++)
        {
            var noteIndex = Math.Min(
                cue.Frequencies.Length - 1,
                sampleIndex * cue.Frequencies.Length / sampleCount);
            var noteStart = noteIndex * sampleCount / cue.Frequencies.Length;
            var noteEnd = (noteIndex + 1) * sampleCount / cue.Frequencies.Length;
            var noteLength = Math.Max(1, noteEnd - noteStart);
            var notePosition = sampleIndex - noteStart;
            var phase = 2 * Math.PI * cue.Frequencies[noteIndex] * notePosition / SampleRate;
            var waveform = cue.UseTriangle
                ? 2 / Math.PI * Math.Asin(Math.Sin(phase))
                : Math.Sin(phase) >= 0 ? 1d : -1d;
            var attack = Math.Min(1d, notePosition / Math.Max(1d, noteLength * 0.08));
            var release = Math.Min(1d, (noteLength - notePosition) / Math.Max(1d, noteLength * 0.24));
            var envelope = attack * release;
            writer.Write((short)(waveform * envelope * short.MaxValue * 0.18));
        }

        writer.Flush();
        var bytes = stream.ToArray();
        if (bytes.Length > PixelArtBudget.MaximumSoundBytes)
        {
            throw new InvalidOperationException(
                $"Generated cue {kind} exceeds {PixelArtBudget.MaximumSoundBytes} bytes.");
        }

        return bytes;
    }

    private static SoundCue GetCue(GameFeedbackKind kind) => kind switch
    {
        GameFeedbackKind.RestockQueued => new(140, false, [392, 523]),
        GameFeedbackKind.PriceChanged => new(90, true, [440]),
        GameFeedbackKind.SaleCompleted => new(160, false, [659, 784]),
        GameFeedbackKind.ProcurementOrdered => new(200, true, [330, 440, 523]),
        GameFeedbackKind.AutoRestockChanged => new(140, true, [440, 550]),
        GameFeedbackKind.EmployeeChanged => new(160, true, [523, 659]),
        GameFeedbackKind.StoreGrowthChanged => new(210, false, [392, 523, 659]),
        GameFeedbackKind.PromotionStarted => new(210, true, [523, 659, 784]),
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
    };

    private sealed record SoundCue(int DurationMilliseconds, bool UseTriangle, int[] Frequencies);
}

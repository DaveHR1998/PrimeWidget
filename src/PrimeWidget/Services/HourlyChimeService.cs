using System;
using System.IO;
using System.Media;
using System.Threading.Tasks;
using PrimeWidget.Models;

namespace PrimeWidget.Services;

public class HourlyChimeService
{
    private int _lastChimedHour = -1;

    public void CheckAndPlayChime(DateTime now, WidgetSettings settings)
    {
        if (!settings.EnableHourlyChime) return;

        // Check if on the hour (minute == 0, second == 0)
        if (now.Minute != 0 || now.Second != 0) return;

        // Prevent double chimes in the same hour
        if (_lastChimedHour == now.Hour) return;
        _lastChimedHour = now.Hour;

        // Quiet hours check
        if (settings.ChimeQuietHoursEnabled)
        {
            if (IsInQuietHours(now.Hour, settings.ChimeQuietStartHour, settings.ChimeQuietEndHour))
            {
                return;
            }
        }

        PlayChime(settings.ChimeSound);
    }

    public static bool IsInQuietHours(int currentHour, int startHour, int endHour)
    {
        if (startHour == endHour) return false;

        if (startHour > endHour)
        {
            // e.g. 22:00 to 07:00 (crosses midnight)
            return currentHour >= startHour || currentHour < endHour;
        }
        else
        {
            // e.g. 13:00 to 15:00
            return currentHour >= startHour && currentHour < endHour;
        }
    }

    public void PlayChime(HourlyChimeSound sound)
    {
        Task.Run(() =>
        {
            try
            {
                using var stream = GenerateChimeStream(sound);
                using var player = new SoundPlayer(stream);
                player.Play();
            }
            catch { }
        });
    }

    public static MemoryStream GenerateChimeStream(HourlyChimeSound sound)
    {
        const int sampleRate = 44100;
        double duration;
        Func<double, double> waveform;

        switch (sound)
        {
            case HourlyChimeSound.CrystalDigital:
                duration = 0.5;
                waveform = (t) =>
                {
                    if (t < 0.22)
                    {
                        double env = Math.Exp(-12.0 * t);
                        return Math.Sin(2 * Math.PI * 1046.5 * t) * env; // C6
                    }
                    else if (t >= 0.25 && t < 0.47)
                    {
                        double t2 = t - 0.25;
                        double env = Math.Exp(-12.0 * t2);
                        return Math.Sin(2 * Math.PI * 1318.5 * t2) * env; // E6
                    }
                    return 0;
                };
                break;

            case HourlyChimeSound.ChurchBell:
                duration = 2.0;
                waveform = (t) =>
                {
                    double env = Math.Exp(-2.2 * t);
                    double f1 = Math.Sin(2 * Math.PI * 440.0 * t);
                    double f2 = 0.5 * Math.Sin(2 * Math.PI * 880.0 * t);
                    double f3 = 0.25 * Math.Sin(2 * Math.PI * 1320.0 * t);
                    return (f1 + f2 + f3) * env * 0.7;
                };
                break;

            case HourlyChimeSound.AcousticBell:
            default:
                duration = 1.4;
                waveform = (t) =>
                {
                    double env = Math.Exp(-3.2 * t);
                    double f1 = Math.Sin(2 * Math.PI * 587.33 * t); // D5
                    double f2 = 0.45 * Math.Sin(2 * Math.PI * 880.0 * t); // A5
                    return (f1 + f2) * env * 0.75;
                };
                break;
        }

        int totalSamples = (int)(sampleRate * duration);
        var ms = new MemoryStream();
        using (var writer = new BinaryWriter(ms, System.Text.Encoding.UTF8, leaveOpen: true))
        {
            // RIFF chunk descriptor
            writer.Write("RIFF"u8.ToArray());
            writer.Write(36 + totalSamples * 2); // File size - 8
            writer.Write("WAVE"u8.ToArray());

            // fmt sub-chunk
            writer.Write("fmt "u8.ToArray());
            writer.Write(16); // Subchunk1Size (16 for PCM)
            writer.Write((short)1); // AudioFormat (1 = PCM)
            writer.Write((short)1); // NumChannels (1 = Mono)
            writer.Write(sampleRate);
            writer.Write(sampleRate * 2); // ByteRate (SampleRate * NumChannels * BitsPerSample/8)
            writer.Write((short)2); // BlockAlign (NumChannels * BitsPerSample/8)
            writer.Write((short)16); // BitsPerSample

            // data sub-chunk
            writer.Write("data"u8.ToArray());
            writer.Write(totalSamples * 2);

            for (int i = 0; i < totalSamples; i++)
            {
                double t = (double)i / sampleRate;
                double sample = waveform(t);
                sample = Math.Clamp(sample, -1.0, 1.0);
                short pcm = (short)(sample * 32767);
                writer.Write(pcm);
            }
        }

        ms.Position = 0;
        return ms;
    }
}

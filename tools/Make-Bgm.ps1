# Original synthesized loop: no external samples. 8 bars, 120 BPM, PCM16 mono.
$destination = Join-Path $PSScriptRoot '../resource/sound/bgm/scrapyard_loop.wav'
Add-Type -TypeDefinition @'
using System;
using System.IO;
public static class ScrapMusic {
    public static void Write(string path) {
        const int rate = 44100, seconds = 16;
        int count = rate * seconds;
        using (var w = new BinaryWriter(File.Create(path))) {
            w.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); w.Write(36 + count * 2);
            w.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt ")); w.Write(16);
            w.Write((short)1); w.Write((short)1); w.Write(rate); w.Write(rate * 2);
            w.Write((short)2); w.Write((short)16);
            w.Write(System.Text.Encoding.ASCII.GetBytes("data")); w.Write(count * 2);
            double[] roots = { 110, 87.307, 130.813, 97.999 };
            int[] melody = { 0, 7, 12, 7, 3, 7, 15, 12 };
            var random = new Random(31030);
            for (int i = 0; i < count; i++) {
                double t = (double)i / rate, beat = t % .5, step = t % .25;
                double root = roots[(int)(t / 4) % 4];
                double bass = Math.Sin(2 * Math.PI * root * t) * Math.Exp(-beat * 9) * .13;
                double freq = root * 2 * Math.Pow(2, melody[(int)(t * 4) % 8] / 12.0);
                double note = Math.Sin(2 * Math.PI * freq * step) * Math.Exp(-step * 18) * Math.Min(1, step * 150) * .065;
                double kick = Math.Sin(2 * Math.PI * (48 * beat + 4 * (1 - Math.Exp(-beat * 28)))) * Math.Exp(-beat * 23) * .13;
                double hat = (random.NextDouble() * 2 - 1) * Math.Exp(-step * 95) * .035;
                double fade = Math.Min(1, Math.Min(t, seconds - t) * 100);
                w.Write((short)(Math.Clamp((bass + note + kick + hat) * fade, -1, 1) * 32767));
            }
        }
    }
}
'@
[ScrapMusic]::Write([IO.Path]::GetFullPath($destination))

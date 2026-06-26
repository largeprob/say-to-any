namespace pc.Services;

internal static class AudioLevelCalculator
{
    public static double Calculate(byte[] buffer, int bytesRecorded)
    {
        var sampleCount = bytesRecorded / 2;
        if (sampleCount == 0)
        {
            return 0;
        }

        double sumSquares = 0;
        for (var offset = 0; offset + 1 < bytesRecorded; offset += 2)
        {
            var sample = BitConverter.ToInt16(buffer, offset) / 32768d;
            sumSquares += sample * sample;
        }

        var rms = Math.Sqrt(sumSquares / sampleCount);
        const double silenceThreshold = 0.006;
        if (rms < silenceThreshold)
        {
            return 0;
        }

        var normalized = Math.Clamp((rms - silenceThreshold) * 18, 0, 1);
        return Math.Pow(normalized, 0.72);
    }
}

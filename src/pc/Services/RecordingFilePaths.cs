namespace pc.Services;

internal static class RecordingFilePaths
{
    public static string CreateRecordingFilePath()
    {
        var fileName = $"dictation-{DateTimeOffset.Now:yyyyMMdd-HHmmss-fff}.wav";

        foreach (var folder in GetRecordingFolders())
        {
            if (TryPrepareWritableFolder(folder))
            {
                return Path.Combine(folder, fileName);
            }
        }

        throw new InvalidOperationException("Cannot create a writable recordings folder.");
    }

    private static IEnumerable<string> GetRecordingFolders()
    {
        yield return Path.Combine(AppContext.BaseDirectory, "recordings");
        yield return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SayToAny",
            "recordings");
    }

    private static bool TryPrepareWritableFolder(string folder)
    {
        try
        {
            Directory.CreateDirectory(folder);
            var probe = Path.Combine(folder, $".write-test-{Guid.NewGuid():N}.tmp");
            using (File.Create(probe))
            {
            }

            File.Delete(probe);
            return true;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }
}

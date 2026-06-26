using System.Text.Json;
using pc.Models;

namespace pc.Services;

public sealed class SettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public string FilePath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "SayToAny",
        "settings.json");

    public ApplicationDataFile Load()
    {
        try
        {
            if (!File.Exists(FilePath))
            {
                return Normalize(new ApplicationDataFile());
            }

            var json = File.ReadAllText(FilePath);
            var data = JsonSerializer.Deserialize<ApplicationDataFile>(json, JsonOptions);
            return Normalize(data);
        }
        catch
        {
            return Normalize(new ApplicationDataFile());
        }
    }

    public void Save(ApplicationDataFile data)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        var json = JsonSerializer.Serialize(Normalize(data), JsonOptions);
        File.WriteAllText(FilePath, json);
    }

    private static ApplicationDataFile Normalize(ApplicationDataFile? data)
    {
        data ??= new ApplicationDataFile();
        data.Settings ??= new AppSettings();
        data.History ??= [];

        if (data.Settings.TimeoutSeconds < 5)
        {
            data.Settings.TimeoutSeconds = 60;
        }

        if (data.Settings.MaxRecordingSeconds < 5)
        {
            data.Settings.MaxRecordingSeconds = 120;
        }

        if (string.IsNullOrWhiteSpace(data.Settings.BaseUrl))
        {
            data.Settings.BaseUrl = AppSettings.DefaultBaseUrl;
        }

        if (string.IsNullOrWhiteSpace(data.Settings.LmBaseUrl))
        {
            data.Settings.LmBaseUrl = data.Settings.BaseUrl;
        }

        if (string.IsNullOrWhiteSpace(data.Settings.AsrBaseUrl))
        {
            data.Settings.AsrBaseUrl = data.Settings.BaseUrl;
        }

        if (string.IsNullOrWhiteSpace(data.Settings.LmApiKey) &&
            !string.IsNullOrWhiteSpace(data.Settings.ApiKey))
        {
            data.Settings.LmApiKey = data.Settings.ApiKey;
        }

        if (string.IsNullOrWhiteSpace(data.Settings.AsrApiKey) &&
            !string.IsNullOrWhiteSpace(data.Settings.ApiKey))
        {
            data.Settings.AsrApiKey = data.Settings.ApiKey;
        }

        if (string.IsNullOrWhiteSpace(data.Settings.AsrModel))
        {
            data.Settings.AsrModel = "qwen3-asr-flash";
        }

        if (string.IsNullOrWhiteSpace(data.Settings.LmModel))
        {
            data.Settings.LmModel = string.IsNullOrWhiteSpace(data.Settings.LlmModel)
                ? "gpt-4o-mini"
                : data.Settings.LlmModel;
        }

        if (string.IsNullOrWhiteSpace(data.Settings.LlmModel))
        {
            data.Settings.LlmModel = data.Settings.LmModel;
        }

        if (double.IsNaN(data.Settings.LmTemperature))
        {
            data.Settings.LmTemperature = data.Settings.Temperature;
        }

        data.Settings.LmTemperature = Math.Clamp(data.Settings.LmTemperature, 0, 2);
        data.Settings.LlmModel = data.Settings.LmModel;
        data.Settings.Temperature = data.Settings.LmTemperature;

        if (string.IsNullOrWhiteSpace(data.Settings.Language))
        {
            data.Settings.Language = "auto";
        }

        if (string.IsNullOrWhiteSpace(data.Settings.AppLanguage))
        {
            data.Settings.AppLanguage = "简体中文";
        }

        if (string.IsNullOrWhiteSpace(data.Settings.Hotkey))
        {
            data.Settings.Hotkey = "双击 Alt";
        }

        if (data.Settings.MicrophoneDeviceNumber < AudioDeviceInfo.AutomaticDeviceNumber)
        {
            data.Settings.MicrophoneDeviceNumber = AudioDeviceInfo.AutomaticDeviceNumber;
        }

        if (data.Settings.HistoryRetention is not ("Never" or "24Hours" or "OneWeek" or "OneMonth" or "Forever"))
        {
            data.Settings.HistoryRetention = "Forever";
        }

        return data;
    }
}

namespace pc.Models;

public sealed record AudioDeviceInfo(int DeviceNumber, string Name, string? DefaultDeviceName = null)
{
    public const int AutomaticDeviceNumber = -1;

    public static AudioDeviceInfo Automatic { get; } = new(AutomaticDeviceNumber, "自动检测");

    public static AudioDeviceInfo CreateAutomatic(string? defaultDeviceName)
    {
        return new AudioDeviceInfo(AutomaticDeviceNumber, "自动检测", defaultDeviceName);
    }

    public bool IsAutomatic => DeviceNumber == AutomaticDeviceNumber;

    public string DisplayName => IsAutomatic ? CreateAutomaticDisplayName() : Name;

    public string Description => IsAutomatic
        ? "使用系统当前默认输入设备。"
        : "手动指定此输入设备。";

    private string CreateAutomaticDisplayName()
    {
        return string.IsNullOrWhiteSpace(DefaultDeviceName)
            ? "自动检测"
            : $"自动检测（当前默认：{DefaultDeviceName}）";
    }
}

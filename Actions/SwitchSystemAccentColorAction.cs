using ClassIsland.Core.Abstractions.Automation;
using ClassIsland.Core.Attributes;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System;
using System.Globalization;
using System.Threading.Tasks;
using SystemTools.Settings;

namespace SystemTools.Actions;

[ActionInfo("SystemTools.SwitchSystemAccentColor", "切换系统强调色", "\uE790", false)]
public class SwitchSystemAccentColorAction(ILogger<SwitchSystemAccentColorAction> logger) : ActionBase<AccentColorSettings>
{
    private readonly ILogger<SwitchSystemAccentColorAction> _logger = logger;

    protected override async Task OnInvoke()
    {
        if (Settings == null || string.IsNullOrWhiteSpace(Settings.ColorHex)) return;

        try
        {
            var color = ParseColor(Settings.ColorHex);
            var dword = ((uint)color.A << 24) | ((uint)color.R << 16) | ((uint)color.G << 8) | color.B;

            using var dwmKey = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\DWM");
            dwmKey?.SetValue("AccentColor", dword, RegistryValueKind.DWord);
            dwmKey?.SetValue("ColorPrevalence", 1, RegistryValueKind.DWord);

            using var explorerKey = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Accent");
            explorerKey?.SetValue("AccentColorMenu", dword, RegistryValueKind.DWord);

            _logger.LogInformation("系统强调色已切换为 {Color}", Settings.ColorHex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "切换系统强调色失败");
            throw;
        }

        await base.OnInvoke();
    }

    private static (byte A, byte R, byte G, byte B) ParseColor(string colorHex)
    {
        var hex = colorHex.Trim().TrimStart('#');
        if (hex.Length == 6) hex = "FF" + hex;
        if (hex.Length != 8) throw new FormatException("颜色格式无效");

        var value = uint.Parse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        return ((byte)(value >> 24), (byte)(value >> 16), (byte)(value >> 8), (byte)value);
    }
}

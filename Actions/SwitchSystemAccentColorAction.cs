using ClassIsland.Core.Abstractions.Automation;
using ClassIsland.Core.Attributes;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using SystemTools.Settings;

namespace SystemTools.Actions;

[ActionInfo("SystemTools.SwitchSystemAccentColor", "切换系统强调色", "\uE790", false)]
public class SwitchSystemAccentColorAction(ILogger<SwitchSystemAccentColorAction> logger) : ActionBase<AccentColorSettings>
{
    private readonly ILogger<SwitchSystemAccentColorAction> _logger = logger;

    [DllImport("user32.dll", SetLastError = true)]
    static extern IntPtr SendMessageTimeout(IntPtr hWnd, uint Msg, UIntPtr wParam, string lParam, uint fuFlags, uint uTimeout, out UIntPtr lpdwResult);

    const uint HWND_BROADCAST = 0xFFFF;
    const uint WM_SETTINGCHANGE = 0x001A;
    const uint SMTO_ABORTIFHUNG = 0x0002;

    protected override async Task OnInvoke()
    {
        if (Settings == null || string.IsNullOrWhiteSpace(Settings.ColorHex)) return;

        try
        {
            var color = ParseColor(Settings.ColorHex);
            // Windows 使用 ABGR 格式（低位字节是 R）
            var dword = ((uint)color.A << 24) | ((uint)color.B << 16) | ((uint)color.G << 8) | color.R;
            // ColorizationColor 通常使用 C4 (196) 作为 Alpha
            var colorizationDword = (0xC4u << 24) | ((uint)color.B << 16) | ((uint)color.G << 8) | color.R;

            using var dwmKey = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\DWM");
            dwmKey?.SetValue("AccentColor", unchecked((int)dword), RegistryValueKind.DWord);
            dwmKey?.SetValue("ColorizationColor", unchecked((int)colorizationDword), RegistryValueKind.DWord);
            dwmKey?.SetValue("ColorizationAfterglow", unchecked((int)colorizationDword), RegistryValueKind.DWord);
            dwmKey?.SetValue("ColorPrevalence", 1, RegistryValueKind.DWord);

            using var explorerKey = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Accent");
            explorerKey?.SetValue("AccentColorMenu", unchecked((int)dword), RegistryValueKind.DWord);

            // 通知 Windows 刷新主题色
            SendMessageTimeout((IntPtr)HWND_BROADCAST, WM_SETTINGCHANGE, (UIntPtr)0, "ImmersiveColorSet", SMTO_ABORTIFHUNG, 5000, out _);

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

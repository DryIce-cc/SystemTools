using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using ClassIsland.Core.Abstractions.Automation;
using ClassIsland.Core.Attributes;
using Microsoft.Extensions.Logging;
using Windows.Win32;

namespace SystemTools.Actions;

[ActionInfo("SystemTools.CtrlZ", "按下 Ctrl+Z", "\uEA0B", false)]
public class CtrlZAction(ILogger<CtrlZAction> logger) : ActionBase
{
    private readonly ILogger<CtrlZAction> _logger = logger;

    private const byte VK_CONTROL = 0x11; // Ctrl 键
    private const byte VK_Z = 0x5A; // Z 键

    protected override async Task OnInvoke()
    {
        try
        {
            _logger.LogInformation("正在模拟按下 Ctrl+Z");

            // 按下 Ctrl
            PInvoke.keybd_event(VK_CONTROL, 0, 0, UIntPtr.Zero);
            await Task.Delay(20);

            // 按下 Z
            PInvoke.keybd_event(VK_Z, 0, 0, UIntPtr.Zero);
            await Task.Delay(20);

            // 释放 Z
            PInvoke.keybd_event(VK_Z, 0, Windows.Win32.UI.Input.KeyboardAndMouse.KEYBD_EVENT_FLAGS.KEYEVENTF_KEYUP,
                UIntPtr.Zero);
            await Task.Delay(20);

            // 释放 Ctrl
            PInvoke.keybd_event(VK_CONTROL, 0, Windows.Win32.UI.Input.KeyboardAndMouse.KEYBD_EVENT_FLAGS.KEYEVENTF_KEYUP,
                UIntPtr.Zero);

            _logger.LogInformation("Ctrl+Z 已成功发送");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发送 Ctrl+Z 失败");
            throw;
        }

        await base.OnInvoke();
    }
}
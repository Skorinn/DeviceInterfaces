# DeviceInterfaces

A small .NET Framework class library that wraps Win32 device-management APIs for Windows desktop
applications. It currently provides USB device arrival and removal notifications for a window.

## Requirements

- .NET Framework 4.8
- Windows, with a window handle available (WinForms, WPF via `HwndSource`, or any HWND)

## Building

The project uses the legacy (non-SDK) MSBuild format, so build it with Visual Studio or MSBuild
rather than the `dotnet` CLI:

```
msbuild DeviceInterfaces.sln -p:Configuration=Release
```

The compiled `DeviceInterfaces.dll` is written to `bin\Release\` and copied to `bin\` at the
solution root.

## Usage

`USBDeviceNotification` registers a window with Windows; the window itself receives the resulting
messages, so the host application handles them in its own window procedure.

1. Register a window handle and keep the returned notification handle.
2. Watch for `WM_DEVICECHANGE` and compare `wParam` against the exposed constants.
3. Unregister with the stored handle during teardown.

```csharp
using System;
using System.Windows.Forms;
using DeviceInterfaces;

public class MainForm : Form
{
    private IntPtr m_pNotificationHandle = IntPtr.Zero;

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        m_pNotificationHandle = USBDeviceNotification.RegisterUsbDeviceNotification(Handle);
    }

    protected override void OnHandleDestroyed(EventArgs e)
    {
        USBDeviceNotification.UnregisterUsbDeviceNotification(m_pNotificationHandle);
        m_pNotificationHandle = IntPtr.Zero;
        base.OnHandleDestroyed(e);
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == USBDeviceNotification.iWM_DEVICECHANGE)
        {
            switch ((int)m.WParam)
            {
                case USBDeviceNotification.iDEVICE_CONNECTED:
                    // A USB device was connected
                    break;

                case USBDeviceNotification.iDEVICE_REMOVED:
                    // A USB device was removed
                    break;
            }
        }

        base.WndProc(ref m);
    }
}
```

## API

| Member | Description |
| --- | --- |
| `RegisterUsbDeviceNotification(IntPtr)` | Registers the given window handle for USB device interface notifications and returns the notification handle. Throws `Win32Exception` if registration fails. |
| `UnregisterUsbDeviceNotification(IntPtr)` | Unregisters a notification handle. Ignores `IntPtr.Zero`, so it is safe to call unconditionally on teardown. |
| `iWM_DEVICECHANGE` | `0x0219` — the device change window message. |
| `iDEVICE_CONNECTED` | `0x8000` — `wParam` value for a device arrival. |
| `iDEVICE_REMOVED` | `0x8004` — `wParam` value for a completed device removal. |

Notifications are filtered to the USB device interface class
(`A5DCBF10-6530-11D2-901F-00C04FB951ED`), so the window is notified about USB devices only.

## License

Released under the [MIT License](LICENSE).

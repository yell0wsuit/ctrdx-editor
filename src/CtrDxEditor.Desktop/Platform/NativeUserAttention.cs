using System;
using System.Runtime.InteropServices;

using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;

using CtrDxEditor.Platform;

namespace CtrDxEditor.Desktop.Platform
{
    /// <summary>
    /// Alerts the user through the OS window manager: a flashing taskbar button on Windows
    /// (<c>FlashWindowEx</c>) and a bouncing dock icon on macOS
    /// (<c>-[NSApplication requestUserAttention:]</c>). Used when something the user must act on - the
    /// incompatible-playtest dialog - appears while their focus is on the just-launched game window
    /// instead of the editor.
    /// </summary>
    /// <remarks>
    /// Linux is a deliberate no-op: raising the window-manager urgency hint reliably needs an X11/Wayland
    /// dependency this app does not carry. Every path is best-effort and swallows a missing-library
    /// failure - the alert is cosmetic and must never take the app down.
    /// </remarks>
    public sealed partial class NativeUserAttention : IUserAttention
    {
        /// <inheritdoc />
        public void Demand()
        {
            try
            {
                switch (OperatingSystem.IsWindows(), OperatingSystem.IsMacOS())
                {
                    case (true, _):
                        DemandWindows();
                        break;
                    case (_, true):
                        DemandMacOS();
                        break;
                    default:
                        // Other platforms: no dependency-free attention signal, so nothing to do.
                        break;
                }
            }
            catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException)
            {
                // The platform library or entry point is unavailable; the alert is simply skipped.
            }
        }

        // Windows: flash the taskbar button until the editor is brought to the foreground.

        [StructLayout(LayoutKind.Sequential)]
        private struct FlashInfo
        {
            public uint Size;
            public IntPtr Handle;
            public uint Flags;
            public uint Count;
            public uint Timeout;
        }

        private const uint FlashAll = 0x3;            // Flash both the caption and the taskbar button.
        private const uint FlashTimerNoForeground = 0xC; // Keep flashing until the window is foregrounded.

        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool FlashWindowEx(ref FlashInfo info);

        private static void DemandWindows()
        {
            if (MainWindowHandle() is not { } hwnd || hwnd == IntPtr.Zero)
            {
                return;
            }

            FlashInfo info = new()
            {
                Size = (uint)Marshal.SizeOf<FlashInfo>(),
                Handle = hwnd,
                Flags = FlashAll | FlashTimerNoForeground,
                Count = uint.MaxValue,
                Timeout = 0, // Use the system default flash rate.
            };
            _ = FlashWindowEx(ref info);
        }

        private static IntPtr? MainWindowHandle()
        {
            return (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?
                .MainWindow?.TryGetPlatformHandle()?.Handle;
        }

        // macOS: bounce the dock icon until the app is activated. App-level, so no window is needed.

        private const string LibObjC = "/usr/lib/libobjc.dylib";
        private const long NSCriticalRequest = 0; // Bounce until the user switches to the app.

        [LibraryImport(LibObjC, EntryPoint = "objc_getClass")]
        private static partial IntPtr GetClass([MarshalAs(UnmanagedType.LPUTF8Str)] string name);

        [LibraryImport(LibObjC, EntryPoint = "sel_registerName")]
        private static partial IntPtr RegisterName([MarshalAs(UnmanagedType.LPUTF8Str)] string name);

        [LibraryImport(LibObjC, EntryPoint = "objc_msgSend")]
        private static partial IntPtr SendMessage(IntPtr receiver, IntPtr selector);

        [LibraryImport(LibObjC, EntryPoint = "objc_msgSend")]
        private static partial IntPtr SendMessage(IntPtr receiver, IntPtr selector, long arg);

        private static void DemandMacOS()
        {
            IntPtr nsApplicationClass = GetClass("NSApplication");
            if (nsApplicationClass == IntPtr.Zero)
            {
                return;
            }

            IntPtr sharedApplication = SendMessage(nsApplicationClass, RegisterName("sharedApplication"));
            if (sharedApplication == IntPtr.Zero)
            {
                return;
            }

            _ = SendMessage(sharedApplication, RegisterName("requestUserAttention:"), NSCriticalRequest);
        }
    }
}

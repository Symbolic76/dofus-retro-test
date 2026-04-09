using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using DofusRetroManager.Models;

namespace DofusRetroManager.Services
{
    public class WindowInfo
    {
        public IntPtr Handle { get; set; }
        public string Title { get; set; } = string.Empty;
        public string ProcessName { get; set; } = string.Empty;
        public string HandleHex => $"0x{Handle.ToInt64():X8}";
    }

    public class NativeWindowManager
    {
        // ---- Win32 imports ----
        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        private const int SW_RESTORE = 9;

        // ---- Public API ----

        /// <summary>
        /// Returns all visible windows whose title contains <paramref name="titlePattern"/>.
        /// </summary>
        public List<WindowInfo> GetDofusWindows(string titlePattern = "Dofus")
        {
            var windows = new List<WindowInfo>();

            EnumWindows((hWnd, _) =>
            {
                if (!IsWindowVisible(hWnd)) return true;

                var sb = new StringBuilder(512);
                if (GetWindowText(hWnd, sb, 512) == 0) return true;

                var title = sb.ToString();
                if (!title.Contains(titlePattern, StringComparison.OrdinalIgnoreCase))
                    return true;

                GetWindowThreadProcessId(hWnd, out uint pid);
                string processName = string.Empty;
                try { processName = Process.GetProcessById((int)pid).ProcessName; }
                catch { /* process may have exited */ }

                windows.Add(new WindowInfo
                {
                    Handle = hWnd,
                    Title = title,
                    ProcessName = processName
                });

                return true;
            }, IntPtr.Zero);

            return windows;
        }

        /// <summary>Bring a window to the foreground, restoring it if minimised.</summary>
        public bool SwitchToWindow(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero) return false;
            ShowWindow(hWnd, SW_RESTORE);
            return SetForegroundWindow(hWnd);
        }

        /// <summary>
        /// Find the best window for <paramref name="character"/> by matching its
        /// <see cref="Character.WindowTitlePattern"/> against all Dofus windows.
        /// Returns null if no match found.
        /// </summary>
        public WindowInfo? FindWindowForCharacter(Character character, string gameWindowPattern)
        {
            var windows = GetDofusWindows(gameWindowPattern);
            foreach (var window in windows)
            {
                if (!string.IsNullOrEmpty(character.WindowTitlePattern) &&
                    window.Title.Contains(character.WindowTitlePattern, StringComparison.OrdinalIgnoreCase))
                {
                    return window;
                }
            }
            return null;
        }

        public IntPtr GetForegroundWindowHandle() => GetForegroundWindow();
    }
}

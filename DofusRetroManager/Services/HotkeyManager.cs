using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace DofusRetroManager.Services
{
    /// <summary>Win32 modifier-key bit flags for RegisterHotKey.</summary>
    public static class WinModifierKeys
    {
        public const uint None    = 0x0000;
        public const uint Alt     = 0x0001;
        public const uint Control = 0x0002;
        public const uint Shift   = 0x0004;
        public const uint Win     = 0x0008;
        /// <summary>Prevents the hotkey from repeating while held down.</summary>
        public const uint NoRepeat = 0x4000;
    }

    /// <summary>
    /// Registers a global hotkey that fires even when the application is not focused.
    /// Must be initialised with a visible <see cref="Window"/> handle (call
    /// <see cref="Initialize"/> from <c>OnSourceInitialized</c>).
    /// </summary>
    public sealed class HotkeyManager : IDisposable
    {
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private const int WM_HOTKEY  = 0x0312;
        private const int HOTKEY_ID  = 9001;

        private HwndSource? _source;
        private Action?     _callback;
        private bool        _registered;

        // ---- Public ----

        public void Initialize(Window window)
        {
            _source = HwndSource.FromHwnd(new WindowInteropHelper(window).Handle);
            _source.AddHook(WndProc);
        }

        /// <summary>
        /// Register (or replace) the global hotkey.
        /// </summary>
        /// <param name="modifiers">Combination of <see cref="WinModifierKeys"/> flags.</param>
        /// <param name="virtualKey">Virtual-key code (e.g. from <c>KeyInterop.VirtualKeyFromKey</c>).</param>
        /// <param name="callback">Action invoked when the hotkey is pressed.</param>
        /// <returns>True if registration succeeded.</returns>
        public bool RegisterHotkey(uint modifiers, uint virtualKey, Action callback)
        {
            if (_source == null) return false;

            Unregister();
            _callback   = callback;
            _registered = RegisterHotKey(_source.Handle, HOTKEY_ID, modifiers, virtualKey);
            return _registered;
        }

        public void Unregister()
        {
            if (_source != null && _registered)
            {
                UnregisterHotKey(_source.Handle, HOTKEY_ID);
                _registered = false;
            }
        }

        public void Dispose()
        {
            Unregister();
            _source?.RemoveHook(WndProc);
        }

        // ---- Private ----

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID)
            {
                _callback?.Invoke();
                handled = true;
            }
            return IntPtr.Zero;
        }
    }
}

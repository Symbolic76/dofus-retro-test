using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using DofusRetroManager.Models;

namespace DofusRetroManager.Services
{
    public class NotificationEventArgs : EventArgs
    {
        public string           WindowTitle      { get; init; } = string.Empty;
        public string           CharacterName    { get; init; } = string.Empty;
        public NotificationType NotificationType { get; init; }
        public IntPtr           WindowHandle     { get; init; }
        public DateTime         Timestamp        { get; init; } = DateTime.Now;
    }

    /// <summary>
    /// Monitors Dofus window-title changes via a WinEvent hook
    /// (<c>EVENT_OBJECT_NAMECHANGE</c>) registered on the UI thread.
    ///
    /// <para>
    /// When a Dofus window title changes and matches one of the configured
    /// <see cref="NotificationPattern"/> keywords, <see cref="NotificationDetected"/>
    /// is raised on the UI thread (no extra marshalling needed).
    /// </para>
    ///
    /// <para>
    /// Call <see cref="Start"/> and <see cref="Stop"/> from the WPF UI thread.
    /// </para>
    /// </summary>
    public sealed class NotificationMonitor : IDisposable
    {
        // ---- Win32 ----
        private delegate void WinEventDelegate(
            IntPtr hWinEventHook, uint eventType,
            IntPtr hwnd, int idObject, int idChild,
            uint dwEventThread, uint dwmsEventTime);

        [DllImport("user32.dll")]
        private static extern IntPtr SetWinEventHook(
            uint eventMin, uint eventMax,
            IntPtr hmodWinEventProc,
            WinEventDelegate lpfnWinEventProc,
            uint idProcess, uint idThread,
            uint dwFlags);

        [DllImport("user32.dll")]
        private static extern bool UnhookWinEvent(IntPtr hWinEventHook);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        private const uint EVENT_OBJECT_NAMECHANGE = 0x800C;
        /// <summary>Hook lives in the hooking process; events are posted asynchronously.</summary>
        private const uint WINEVENT_OUTOFCONTEXT   = 0x0000;

        // ---- State ----
        private IntPtr             _hook        = IntPtr.Zero;
        // Keep a hard reference so the delegate is never GC'd while the hook is active.
        private WinEventDelegate?  _delegate;

        private List<NotificationPattern> _patterns    = new();
        private string                    _gamePattern = "Dofus";
        private List<Character>           _characters  = new();

        // ---- Events ----
        public event EventHandler<NotificationEventArgs>? NotificationDetected;

        // ---- Public API ----

        public void Configure(
            List<NotificationPattern> patterns,
            string                    gameWindowPattern,
            List<Character>           characters)
        {
            _patterns    = patterns;
            _gamePattern = gameWindowPattern;
            _characters  = characters;
        }

        /// <summary>
        /// Starts the WinEvent hook. Must be called from the UI thread.
        /// </summary>
        public void Start()
        {
            if (_hook != IntPtr.Zero) return;

            _delegate = WinEventCallback;          // prevent GC
            _hook = SetWinEventHook(
                EVENT_OBJECT_NAMECHANGE,
                EVENT_OBJECT_NAMECHANGE,
                IntPtr.Zero,
                _delegate,
                0, 0,
                WINEVENT_OUTOFCONTEXT);
        }

        /// <summary>Stops the WinEvent hook.</summary>
        public void Stop()
        {
            if (_hook != IntPtr.Zero)
            {
                UnhookWinEvent(_hook);
                _hook     = IntPtr.Zero;
                _delegate = null;
            }
        }

        public bool IsRunning => _hook != IntPtr.Zero;

        public void Dispose() => Stop();

        // ---- Private ----

        private void WinEventCallback(
            IntPtr hWinEventHook, uint eventType,
            IntPtr hwnd, int idObject, int idChild,
            uint dwEventThread, uint dwmsEventTime)
        {
            if (hwnd == IntPtr.Zero) return;

            var sb = new StringBuilder(512);
            if (GetWindowText(hwnd, sb, 512) == 0) return;

            var title = sb.ToString();
            if (!title.Contains(_gamePattern, StringComparison.OrdinalIgnoreCase)) return;

            foreach (var pattern in _patterns)
            {
                if (title.Contains(pattern.Pattern, StringComparison.OrdinalIgnoreCase))
                {
                    var charName = ExtractCharacterName(title);
                    NotificationDetected?.Invoke(this, new NotificationEventArgs
                    {
                        WindowTitle      = title,
                        CharacterName    = charName,
                        NotificationType = pattern.Type,
                        WindowHandle     = hwnd
                    });
                    break; // only one notification per title change
                }
            }
        }

        /// <summary>
        /// Tries to extract character name from titles like
        /// "Dofus Retro - CharacterName - C'est votre tour!".
        /// </summary>
        private static string ExtractCharacterName(string title)
        {
            var parts = title.Split(" - ", StringSplitOptions.RemoveEmptyEntries);
            return parts.Length >= 2 ? parts[1].Trim() : string.Empty;
        }
    }
}

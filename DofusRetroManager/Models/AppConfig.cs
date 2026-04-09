using System.Collections.Generic;

namespace DofusRetroManager.Models
{
    public class AppConfig
    {
        /// <summary>Ordered list of characters.</summary>
        public List<Character> Characters { get; set; } = new();

        // ----- Hotkey -----
        /// <summary>WPF Key enum name (e.g. "Tab", "F1", "D1").</summary>
        public string HotkeyKey { get; set; } = "Tab";
        public bool HotkeyCtrl { get; set; } = false;
        public bool HotkeyAlt { get; set; } = false;
        public bool HotkeyShift { get; set; } = false;

        // ----- Notification monitoring -----
        public bool AutoSwapEnabled { get; set; } = true;

        /// <summary>Substring used to identify Dofus windows (e.g. "Dofus").</summary>
        public string GameWindowTitlePattern { get; set; } = "Dofus";

        /// <summary>
        /// Patterns searched in the window title to identify a notification type.
        /// Matching is case-insensitive.
        /// </summary>
        public List<NotificationPattern> NotificationPatterns { get; set; } = new()
        {
            new NotificationPattern { Pattern = "tour",       Type = NotificationType.TurnStart },
            new NotificationPattern { Pattern = "invitation", Type = NotificationType.GroupInvitation },
            new NotificationPattern { Pattern = "échange",    Type = NotificationType.Trade },
            new NotificationPattern { Pattern = "echange",    Type = NotificationType.Trade },
            new NotificationPattern { Pattern = "combat",     Type = NotificationType.TurnStart },
        };
    }

    public class NotificationPattern
    {
        public string Pattern { get; set; } = string.Empty;
        public NotificationType Type { get; set; }
    }

    public enum NotificationType
    {
        TurnStart,
        GroupInvitation,
        Trade,
        Other
    }
}

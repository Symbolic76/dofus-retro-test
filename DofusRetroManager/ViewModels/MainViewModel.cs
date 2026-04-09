using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using DofusRetroManager.Models;
using DofusRetroManager.Services;

namespace DofusRetroManager.ViewModels
{
    // -----------------------------------------------------------------------
    //  Simple relay command
    // -----------------------------------------------------------------------
    public sealed class RelayCommand : ICommand
    {
        private readonly Action<object?>          _execute;
        private readonly Func<object?, bool>?     _canExecute;

        public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
        {
            _execute    = execute;
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;
        public void Execute(object? parameter)    => _execute(parameter);

        public event EventHandler? CanExecuteChanged
        {
            add    => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
    }

    // -----------------------------------------------------------------------
    //  Main view-model
    // -----------------------------------------------------------------------
    public sealed class MainViewModel : INotifyPropertyChanged, IDisposable
    {
        // ---- Services ----
        private readonly ConfigService        _configService;
        private readonly NativeWindowManager  _windowManager;
        private readonly HotkeyManager        _hotkeyManager;
        private readonly NotificationMonitor  _notificationMonitor;

        // ---- State ----
        private AppConfig  _config;
        private bool       _isMonitoring;
        private string     _statusMessage       = "Prêt";
        private Character? _selectedCharacter;
        private string     _newCharacterName    = string.Empty;
        private string     _newWindowPattern    = string.Empty;
        private string     _hotkeySetting       = "Tab";
        private bool       _hotkeyCtrl;
        private bool       _hotkeyAlt;
        private bool       _hotkeyShift;
        private int        _currentCharacterIndex;

        // ---- Bindable collections ----
        public ObservableCollection<Character>  Characters      { get; } = new();
        public ObservableCollection<string>     LogEntries      { get; } = new();
        public ObservableCollection<WindowInfo> DetectedWindows { get; } = new();

        // ---- Properties ----

        public bool IsMonitoring
        {
            get => _isMonitoring;
            set { _isMonitoring = value; OnPropertyChanged(); OnPropertyChanged(nameof(MonitoringButtonText)); }
        }

        public string MonitoringButtonText => IsMonitoring ? "⏹ Arrêter" : "▶ Démarrer";

        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        public Character? SelectedCharacter
        {
            get => _selectedCharacter;
            set { _selectedCharacter = value; OnPropertyChanged(); }
        }

        public string NewCharacterName
        {
            get => _newCharacterName;
            set { _newCharacterName = value; OnPropertyChanged(); }
        }

        public string NewWindowPattern
        {
            get => _newWindowPattern;
            set { _newWindowPattern = value; OnPropertyChanged(); }
        }

        public string HotkeySetting
        {
            get => _hotkeySetting;
            set { _hotkeySetting = value; OnPropertyChanged(); }
        }

        public bool HotkeyCtrl
        {
            get => _hotkeyCtrl;
            set { _hotkeyCtrl = value; OnPropertyChanged(); }
        }

        public bool HotkeyAlt
        {
            get => _hotkeyAlt;
            set { _hotkeyAlt = value; OnPropertyChanged(); }
        }

        public bool HotkeyShift
        {
            get => _hotkeyShift;
            set { _hotkeyShift = value; OnPropertyChanged(); }
        }

        public bool AutoSwapEnabled
        {
            get => _config.AutoSwapEnabled;
            set { _config.AutoSwapEnabled = value; OnPropertyChanged(); }
        }

        public string GameWindowPattern
        {
            get => _config.GameWindowTitlePattern;
            set { _config.GameWindowTitlePattern = value; OnPropertyChanged(); }
        }

        // ---- Commands ----
        public ICommand AddCharacterCommand      { get; }
        public ICommand RemoveCharacterCommand   { get; }
        public ICommand MoveUpCommand            { get; }
        public ICommand MoveDownCommand          { get; }
        public ICommand ToggleMonitoringCommand  { get; }
        public ICommand SaveConfigCommand        { get; }
        public ICommand RefreshWindowsCommand    { get; }
        public ICommand SwitchToNextCommand      { get; }
        public ICommand ClearLogCommand          { get; }
        public ICommand ApplyHotkeyCommand       { get; }

        // ---- Constructor ----
        public MainViewModel()
        {
            _configService       = new ConfigService();
            _windowManager       = new NativeWindowManager();
            _hotkeyManager       = new HotkeyManager();
            _notificationMonitor = new NotificationMonitor();

            _config = _configService.Load();

            foreach (var ch in _config.Characters.OrderBy(c => c.Order))
                Characters.Add(ch);

            HotkeySetting = _config.HotkeyKey;
            HotkeyCtrl    = _config.HotkeyCtrl;
            HotkeyAlt     = _config.HotkeyAlt;
            HotkeyShift   = _config.HotkeyShift;

            AddCharacterCommand    = new RelayCommand(_ => AddCharacter(),    _ => !string.IsNullOrWhiteSpace(NewCharacterName));
            RemoveCharacterCommand = new RelayCommand(_ => RemoveCharacter(), _ => SelectedCharacter != null);
            MoveUpCommand          = new RelayCommand(_ => MoveUp(),          _ => SelectedCharacter != null && Characters.IndexOf(SelectedCharacter) > 0);
            MoveDownCommand        = new RelayCommand(_ => MoveDown(),        _ => SelectedCharacter != null && Characters.IndexOf(SelectedCharacter) < Characters.Count - 1);
            ToggleMonitoringCommand = new RelayCommand(_ => ToggleMonitoring());
            SaveConfigCommand      = new RelayCommand(_ => SaveConfig());
            RefreshWindowsCommand  = new RelayCommand(_ => RefreshWindows());
            SwitchToNextCommand    = new RelayCommand(_ => SwitchToNextCharacter());
            ClearLogCommand        = new RelayCommand(_ => LogEntries.Clear());
            ApplyHotkeyCommand     = new RelayCommand(_ => ApplyHotkey());

            _notificationMonitor.NotificationDetected += OnNotificationDetected;

            AddLog("Application démarrée");
            AddLog($"Configuration chargée depuis : {_configService.ConfigPath}");
        }

        /// <summary>Must be called from OnSourceInitialized after the window handle is available.</summary>
        public void InitializeHotkey(Window window)
        {
            _hotkeyManager.Initialize(window);
            ApplyHotkey();
        }

        // ---- Character management ----

        private void AddCharacter()
        {
            if (string.IsNullOrWhiteSpace(NewCharacterName)) return;

            var character = new Character
            {
                Name               = NewCharacterName.Trim(),
                WindowTitlePattern = NewWindowPattern.Trim(),
                Order              = Characters.Count
            };

            Characters.Add(character);
            SyncConfigCharacters();
            AddLog($"Personnage ajouté : {character.Name}");
            NewCharacterName = string.Empty;
            NewWindowPattern = string.Empty;
        }

        private void RemoveCharacter()
        {
            if (SelectedCharacter == null) return;
            var name = SelectedCharacter.Name;
            Characters.Remove(SelectedCharacter);
            SelectedCharacter = null;
            ReorderCharacters();
            SyncConfigCharacters();
            AddLog($"Personnage supprimé : {name}");
        }

        private void MoveUp()
        {
            if (SelectedCharacter == null) return;
            var idx = Characters.IndexOf(SelectedCharacter);
            if (idx <= 0) return;
            Characters.Move(idx, idx - 1);
            ReorderCharacters();
            SyncConfigCharacters();
        }

        private void MoveDown()
        {
            if (SelectedCharacter == null) return;
            var idx = Characters.IndexOf(SelectedCharacter);
            if (idx >= Characters.Count - 1) return;
            Characters.Move(idx, idx + 1);
            ReorderCharacters();
            SyncConfigCharacters();
        }

        private void ReorderCharacters()
        {
            for (int i = 0; i < Characters.Count; i++)
                Characters[i].Order = i;
        }

        // ---- Monitoring ----

        private void ToggleMonitoring()
        {
            if (IsMonitoring)
            {
                _notificationMonitor.Stop();
                IsMonitoring  = false;
                StatusMessage = "Monitoring arrêté";
                AddLog("Monitoring des notifications arrêté");
            }
            else
            {
                _notificationMonitor.Configure(
                    _config.NotificationPatterns,
                    _config.GameWindowTitlePattern,
                    _config.Characters);
                _notificationMonitor.Start();
                IsMonitoring  = true;
                StatusMessage = "Monitoring actif";
                AddLog("Monitoring des notifications démarré");
            }
        }

        // ---- Save / Refresh ----

        private void SaveConfig()
        {
            try
            {
                SyncConfigCharacters();
                _config.HotkeyKey   = HotkeySetting;
                _config.HotkeyCtrl  = HotkeyCtrl;
                _config.HotkeyAlt   = HotkeyAlt;
                _config.HotkeyShift = HotkeyShift;
                _configService.Save(_config);
                StatusMessage = "Configuration sauvegardée ✓";
                AddLog($"Configuration sauvegardée dans : {_configService.ConfigPath}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la sauvegarde :\n{ex.Message}", "Erreur",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SyncConfigCharacters()
        {
            _config.Characters.Clear();
            foreach (var ch in Characters)
                _config.Characters.Add(ch);
        }

        public void RefreshWindows()
        {
            DetectedWindows.Clear();
            var windows = _windowManager.GetDofusWindows(_config.GameWindowTitlePattern);
            foreach (var w in windows)
                DetectedWindows.Add(w);
            AddLog($"Fenêtres détectées : {windows.Count}");
        }

        // ---- Window switching ----

        public void SwitchToNextCharacter()
        {
            var active = Characters.Where(c => c.IsActive).ToList();
            if (active.Count == 0)
            {
                AddLog("Aucun personnage actif configuré");
                return;
            }

            _currentCharacterIndex = (_currentCharacterIndex + 1) % active.Count;
            SwitchToCharacter(active[_currentCharacterIndex]);
        }

        public void SwitchToCharacter(Character character)
        {
            var window = _windowManager.FindWindowForCharacter(character, _config.GameWindowTitlePattern);
            if (window != null)
            {
                _windowManager.SwitchToWindow(window.Handle);
                AddLog($"↩ Basculé vers : {character.Name}  ({window.Title})");
                StatusMessage = $"Fenêtre active : {character.Name}";
            }
            else
            {
                // Fallback: cycle through all detected Dofus windows
                var all = _windowManager.GetDofusWindows(_config.GameWindowTitlePattern);
                if (all.Count > 0)
                {
                    var w = all[_currentCharacterIndex % all.Count];
                    _windowManager.SwitchToWindow(w.Handle);
                    AddLog($"↩ Basculé vers (fenêtre générique) : {w.Title}");
                }
                else
                {
                    AddLog($"⚠ Fenêtre introuvable pour : {character.Name}");
                }
            }
        }

        public void SwitchToWindowHandle(IntPtr handle)
        {
            _windowManager.SwitchToWindow(handle);
            AddLog($"↩ Basculé vers la fenêtre sélectionnée");
        }

        // ---- Notification handler ----

        private void OnNotificationDetected(object? sender, NotificationEventArgs e)
        {
            // Already on UI thread because the hook was registered from the UI thread.
            var typeName = e.NotificationType switch
            {
                NotificationType.TurnStart       => "Début de tour",
                NotificationType.GroupInvitation => "Invitation de groupe",
                NotificationType.Trade           => "Échange",
                _                                => "Notification"
            };

            AddLog($"🔔 {typeName} — {e.CharacterName}  [{e.WindowTitle}]");

            if (AutoSwapEnabled && e.WindowHandle != IntPtr.Zero)
            {
                _windowManager.SwitchToWindow(e.WindowHandle);
                AddLog($"↩ Auto-swap vers : {e.WindowTitle}");
                StatusMessage = $"Auto-swap : {e.CharacterName}";
            }
        }

        // ---- Hotkey ----

        public void ApplyHotkey()
        {
            if (string.IsNullOrWhiteSpace(HotkeySetting)) return;

            try
            {
                var key = (Key)Enum.Parse(typeof(Key), HotkeySetting, ignoreCase: true);
                var vk  = (uint)KeyInterop.VirtualKeyFromKey(key);

                uint modifiers = WinModifierKeys.NoRepeat;
                if (HotkeyCtrl)  modifiers |= WinModifierKeys.Control;
                if (HotkeyAlt)   modifiers |= WinModifierKeys.Alt;
                if (HotkeyShift) modifiers |= WinModifierKeys.Shift;

                var success = _hotkeyManager.RegisterHotkey(modifiers, vk, SwitchToNextCharacter);
                if (success)
                {
                    var combo = $"{(HotkeyCtrl ? "Ctrl+" : "")}{(HotkeyAlt ? "Alt+" : "")}{(HotkeyShift ? "Shift+" : "")}{HotkeySetting}";
                    AddLog($"⌨ Raccourci enregistré : {combo}");
                }
                else
                {
                    AddLog("⚠ Impossible d'enregistrer le raccourci (touche déjà utilisée par une autre application)");
                }
            }
            catch (Exception ex)
            {
                AddLog($"⚠ Erreur raccourci : {ex.Message}");
            }
        }

        // ---- Logging ----

        private void AddLog(string message)
        {
            var entry = $"[{DateTime.Now:HH:mm:ss}] {message}";
            LogEntries.Insert(0, entry);
            while (LogEntries.Count > 200)
                LogEntries.RemoveAt(LogEntries.Count - 1);
        }

        // ---- INotifyPropertyChanged ----

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        // ---- IDisposable ----

        public void Dispose()
        {
            _notificationMonitor.Dispose();
            _hotkeyManager.Dispose();
        }
    }
}

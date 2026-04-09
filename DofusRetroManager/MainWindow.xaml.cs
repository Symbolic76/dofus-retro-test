using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using DofusRetroManager.Services;
using DofusRetroManager.ViewModels;

namespace DofusRetroManager
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _vm;
        private bool _capturingHotkey;

        public MainWindow()
        {
            InitializeComponent();
            _vm = new MainViewModel();
            DataContext = _vm;
        }

        // Called by WPF once the Win32 handle is available — needed for hotkey registration.
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            _vm.InitializeHotkey(this);
        }

        protected override void OnClosed(EventArgs e)
        {
            _vm.Dispose();
            base.OnClosed(e);
        }

        // ---------------------------------------------------------------
        //  Hotkey capture
        // ---------------------------------------------------------------

        private void HotkeyTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            _capturingHotkey = true;
            HotkeyTextBox.Background = new SolidColorBrush(Color.FromRgb(0xFF, 0xF5, 0xCC));
            HotkeyTextBox.Text = "Appuyez sur une touche…";
        }

        private void HotkeyTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            _capturingHotkey = false;
            HotkeyTextBox.Background = new SolidColorBrush(Color.FromRgb(0xE3, 0xF2, 0xFD));
        }

        private void HotkeyTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (!_capturingHotkey) return;
            e.Handled = true;

            // Resolve the actual key (handles AltGr / system keys)
            var key = e.Key == Key.System ? e.SystemKey : e.Key;

            // Ignore lone modifier keys
            if (key is Key.LeftCtrl or Key.RightCtrl
                    or Key.LeftAlt  or Key.RightAlt
                    or Key.LeftShift or Key.RightShift
                    or Key.LWin     or Key.RWin)
            {
                return;
            }

            _vm.HotkeySetting = key.ToString();
            _capturingHotkey  = false;
            HotkeyTextBox.Background = new SolidColorBrush(Color.FromRgb(0xE3, 0xF2, 0xFD));

            // Move focus away so LostFocus fires cleanly
            Keyboard.ClearFocus();
        }

        // ---------------------------------------------------------------
        //  Windows tab — double-click to switch
        // ---------------------------------------------------------------

        private void WindowsListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (WindowsListView.SelectedItem is WindowInfo window)
                _vm.SwitchToWindowHandle(window.Handle);
        }
    }
}

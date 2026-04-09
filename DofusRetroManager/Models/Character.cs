using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DofusRetroManager.Models
{
    public class Character : INotifyPropertyChanged
    {
        private string _name = string.Empty;
        private string _windowTitlePattern = string.Empty;
        private int _order;
        private bool _isActive = true;

        public Guid Id { get; set; } = Guid.NewGuid();

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Substring to match against the Dofus window title (e.g. the character name).
        /// </summary>
        public string WindowTitlePattern
        {
            get => _windowTitlePattern;
            set { _windowTitlePattern = value; OnPropertyChanged(); }
        }

        public int Order
        {
            get => _order;
            set { _order = value; OnPropertyChanged(); }
        }

        public bool IsActive
        {
            get => _isActive;
            set { _isActive = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

using System.Windows;

namespace DofusRetroManager
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            DispatcherUnhandledException += (_, ex) =>
            {
                MessageBox.Show(
                    $"Erreur inattendue :\n{ex.Exception.Message}",
                    "Dofus Retro Manager — Erreur",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                ex.Handled = true;
            };
        }
    }
}

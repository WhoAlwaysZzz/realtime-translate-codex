using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace RealtimeTranslate.App;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow(bool isDesktopTestHost = false)
    {
        InitializeComponent();
        if (isDesktopTestHost)
        {
            Title = "Realtime Translate Codex · Desktop Test Host";
            DesktopTestText.Visibility = Visibility.Visible;
            return;
        }

        DesktopTestText.Visibility = Visibility.Collapsed;
    }
}

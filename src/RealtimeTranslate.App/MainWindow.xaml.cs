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
            Content = new TextBlock
            {
                Text = "Desktop verification host\nRIGHT DRAG across: shelter   companion   discover\n to exercise OCR and lookup.",
                FontSize = 28,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(48),
                VerticalAlignment = System.Windows.VerticalAlignment.Center,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
            };
        }
    }
}

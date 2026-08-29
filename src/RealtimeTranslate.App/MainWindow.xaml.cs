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
using WpfBrushes = System.Windows.Media.Brushes;

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
            Width = 800;
            Height = 450;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            ShowActivated = true;
            Loaded += (_, _) => BuildDesktopTestFixture();
            return;
        }

        DesktopTestText.Visibility = Visibility.Collapsed;
    }

    private void BuildDesktopTestFixture()
    {
        // Install after the window has loaded so the test fixture cannot be
        // replaced by the initial XAML content during window creation.
        Content = new Border
        {
            Background = WpfBrushes.White,
            Child = new System.Windows.Controls.TextBox
            {
                Text = "shelter",
                FontSize = 48,
                FontWeight = FontWeights.Bold,
                Foreground = WpfBrushes.Black,
                Background = WpfBrushes.White,
                BorderThickness = new Thickness(0),
                IsReadOnly = true,
                Width = 520,
                Height = 96,
                TextAlignment = TextAlignment.Center,
                VerticalContentAlignment = System.Windows.VerticalAlignment.Center,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                VerticalAlignment = System.Windows.VerticalAlignment.Center,
            },
        };
        UpdateLayout();
    }
}

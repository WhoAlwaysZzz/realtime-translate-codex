using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using RealtimeTranslate.Core.Models;
using ControlsOrientation = System.Windows.Controls.Orientation;
using WpfBrushes = System.Windows.Media.Brushes;
using WpfButton = System.Windows.Controls.Button;
using WpfClipboard = System.Windows.Clipboard;
using WpfColor = System.Windows.Media.Color;
using WpfKeyEventArgs = System.Windows.Input.KeyEventArgs;
using WpfTextBlock = System.Windows.Controls.TextBlock;

namespace RealtimeTranslate.App.Presentation;

public sealed class TextExplanationWindow : Window
{
    private readonly TextExplanationPresentation _presentation;

    public TextExplanationWindow(TextExplanationPresentation presentation)
    {
        _presentation = presentation;
        Title = presentation.Title;
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.CanResizeWithGrip;
        ShowInTaskbar = false;
        Topmost = true;
        Width = 640;
        Height = 520;
        MinWidth = 420;
        MinHeight = 320;
        Background = WpfBrushes.Transparent;
        AllowsTransparency = true;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Content = BuildContent(presentation);
        PreviewKeyDown += OnPreviewKeyDown;
    }

    private UIElement BuildContent(TextExplanationPresentation presentation)
    {
        var root = new Border
        {
            CornerRadius = new CornerRadius(10),
            Background = new SolidColorBrush(WpfColor.FromArgb(246, 24, 29, 36)),
            BorderBrush = new SolidColorBrush(WpfColor.FromArgb(160, 255, 255, 255)),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(18),
        };

        var layout = new Grid();
        layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.Child = layout;

        var header = BuildHeader(presentation);
        Grid.SetRow(header, 0);
        layout.Children.Add(header);

        var content = BuildScrollableContent(presentation);
        Grid.SetRow(content, 1);
        layout.Children.Add(content);

        var footer = BuildFooter(presentation);
        Grid.SetRow(footer, 2);
        layout.Children.Add(footer);

        return root;
    }

    private UIElement BuildHeader(TextExplanationPresentation presentation)
    {
        var header = new Grid
        {
            Margin = new Thickness(0, 0, 0, 14),
        };
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var title = new WpfTextBlock
        {
            Text = presentation.Title,
            FontSize = 22,
            FontWeight = FontWeights.SemiBold,
            Foreground = presentation.IsError
                ? new SolidColorBrush(WpfColor.FromRgb(255, 204, 204))
                : WpfBrushes.White,
            VerticalAlignment = VerticalAlignment.Center,
        };
        Grid.SetColumn(title, 0);
        header.Children.Add(title);

        var copy = BuildButton("Copy", CopyAll);
        Grid.SetColumn(copy, 1);
        header.Children.Add(copy);

        var close = BuildButton("Close", Close);
        close.Margin = new Thickness(8, 0, 0, 0);
        Grid.SetColumn(close, 2);
        header.Children.Add(close);

        return header;
    }

    private static UIElement BuildScrollableContent(TextExplanationPresentation presentation)
    {
        var panel = new StackPanel
        {
            Orientation = ControlsOrientation.Vertical,
        };

        panel.Children.Add(BuildSection("Original", presentation.SourceText, 14, WpfColor.FromRgb(218, 224, 235)));
        panel.Children.Add(BuildSection("Translation", presentation.Translation, 20, presentation.IsError ? WpfColor.FromRgb(255, 204, 204) : WpfColor.FromRgb(255, 244, 198)));

        if (presentation.Notes.Count > 0)
        {
            var notesTitle = BuildLabel("Notes");
            notesTitle.Margin = new Thickness(0, 16, 0, 6);
            panel.Children.Add(notesTitle);

            foreach (var note in presentation.Notes)
            {
                panel.Children.Add(new WpfTextBlock
                {
                    Text = "- " + note,
                    Margin = new Thickness(0, 0, 0, 6),
                    FontSize = 14,
                    Foreground = new SolidColorBrush(WpfColor.FromRgb(225, 230, 239)),
                    TextWrapping = TextWrapping.Wrap,
                });
            }
        }

        return new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = panel,
        };
    }

    private static UIElement BuildFooter(TextExplanationPresentation presentation)
    {
        return new WpfTextBlock
        {
            Text = presentation.Footer,
            Margin = new Thickness(0, 12, 0, 0),
            FontSize = 12,
            Foreground = new SolidColorBrush(WpfColor.FromRgb(160, 169, 184)),
            TextWrapping = TextWrapping.Wrap,
        };
    }

    private static UIElement BuildSection(string label, string text, double fontSize, WpfColor textColor)
    {
        var panel = new StackPanel
        {
            Orientation = ControlsOrientation.Vertical,
            Margin = new Thickness(0, 0, 0, 12),
        };
        panel.Children.Add(BuildLabel(label));
        panel.Children.Add(new WpfTextBlock
        {
            Text = text,
            Margin = new Thickness(0, 5, 0, 0),
            FontSize = fontSize,
            FontWeight = label == "Translation" ? FontWeights.SemiBold : FontWeights.Normal,
            Foreground = new SolidColorBrush(textColor),
            TextWrapping = TextWrapping.Wrap,
        });
        return panel;
    }

    private static WpfTextBlock BuildLabel(string text)
    {
        return new WpfTextBlock
        {
            Text = text,
            FontSize = 12,
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(WpfColor.FromRgb(143, 153, 169)),
        };
    }

    private static WpfButton BuildButton(string text, Action action)
    {
        var button = new WpfButton
        {
            Content = text,
            Padding = new Thickness(12, 5, 12, 5),
            MinWidth = 64,
            BorderThickness = new Thickness(0),
            Foreground = WpfBrushes.White,
            Background = new SolidColorBrush(WpfColor.FromRgb(54, 62, 75)),
        };
        button.Click += (_, _) => action();
        return button;
    }

    private void CopyAll()
    {
        WpfClipboard.SetText($"""
        {_presentation.SourceText}

        {_presentation.Translation}

        {string.Join(Environment.NewLine, _presentation.Notes)}
        """);
    }

    private void OnPreviewKeyDown(object sender, WpfKeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Close();
            e.Handled = true;
        }
    }
}

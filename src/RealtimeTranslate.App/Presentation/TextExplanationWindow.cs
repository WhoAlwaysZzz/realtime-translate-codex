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
        UiTheme.StyleWindow(this);
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        UiTheme.PrepareEntrance(this);
        Content = BuildContent(presentation);
        PreviewKeyDown += OnPreviewKeyDown;
    }

    private UIElement BuildContent(TextExplanationPresentation presentation)
    {
        var layout = new Grid();
        layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var header = BuildHeader(presentation);
        Grid.SetRow(header, 0);
        layout.Children.Add(header);

        var content = BuildScrollableContent(presentation);
        Grid.SetRow(content, 1);
        layout.Children.Add(content);

        var footer = BuildFooter(presentation);
        Grid.SetRow(footer, 2);
        layout.Children.Add(footer);

        return UiTheme.CreatePanel(layout, new Thickness(20));
    }

    private UIElement BuildHeader(TextExplanationPresentation presentation)
    {
        var header = new Grid
        {
            Margin = new Thickness(0, 0, 0, 14),
        };
        header.MouseLeftButtonDown += (_, e) =>
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                DragMove();
            }
        };
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var title = new WpfTextBlock
        {
            Text = presentation.Title,
            FontSize = 18,
            FontWeight = FontWeights.SemiBold,
            Foreground = presentation.IsError
                ? UiTheme.Brush(UiTheme.Danger)
                : UiTheme.Brush(UiTheme.TextPrimary),
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

        panel.Children.Add(BuildSection("Original", presentation.SourceText, 14, UiTheme.TextSecondary));
        panel.Children.Add(BuildDivider());
        panel.Children.Add(BuildSection("Translation", presentation.Translation, 19, presentation.IsError ? UiTheme.Danger : UiTheme.AccentWarm));

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
                    Foreground = UiTheme.Brush(UiTheme.TextPrimary),
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
            Foreground = UiTheme.Brush(UiTheme.TextMuted),
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
            Foreground = UiTheme.Brush(textColor),
            TextWrapping = TextWrapping.Wrap,
        });
        return panel;
    }

    private static WpfTextBlock BuildLabel(string text)
    {
        return UiTheme.CreateLabel(text);
    }

    private static Border BuildDivider()
    {
        return new Border
        {
            Height = 1,
            Margin = new Thickness(0, 2, 0, 14),
            Background = UiTheme.Brush(WpfColor.FromArgb(45, 255, 255, 255)),
        };
    }

    private static WpfButton BuildButton(string text, Action action)
    {
        return UiTheme.CreateButton(text, action);
    }

    protected override void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);
        UiTheme.PlayEntrance(this, TimeSpan.FromMilliseconds(220));
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

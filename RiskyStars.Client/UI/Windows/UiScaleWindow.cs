using System;
using Myra.Graphics2D.UI;

namespace RiskyStars.Client;

public sealed class UiScaleWindow : DockableWindow
{
    private readonly Settings _settings;
    private readonly Action<int> _onPreviewScaleChanged;

    private HorizontalSlider? _scaleSlider;
    private Label? _scaleValueLabel;
    private Label? _previewLabel;

    public UiScaleWindow(Settings settings, WindowPreferences preferences, int screenWidth, int screenHeight, Action<int> onPreviewScaleChanged)
        : base("ui_scale", "UI Scale", preferences, screenWidth, screenHeight, 320, 220)
    {
        _settings = settings;
        _onPreviewScaleChanged = onPreviewScaleChanged;

        BuildContent();
        SyncFromSettings();

        if (_preferences.GetWindowState(_windowId) == null)
        {
            _window.Visible = false;
            DockTo(DockPosition.BottomRight);
            SaveState();
        }
    }

    public void SyncFromSettings()
    {
        if (_scaleSlider == null)
        {
            return;
        }

        _scaleSlider.Value = _settings.UiScalePercent;
        UpdateScaleDisplay(_settings.UiScalePercent);
    }

    private void BuildContent()
    {
        var layout = new VerticalStackPanel
        {
            Spacing = ThemeManager.Spacing.Medium,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        layout.Widgets.Add(BuildScalePanel());
        layout.Widgets.Add(BuildPresetPanel());
        layout.Widgets.Add(BuildButtonRow());

        _window.Content = layout;
    }

    private Widget BuildScalePanel()
    {
        var panel = ThemedUIFactory.CreateGameplayPanel(ThemeManager.Colors.BorderFocus);
        panel.HorizontalAlignment = HorizontalAlignment.Stretch;

        var layout = new VerticalStackPanel
        {
            Spacing = ThemeManager.Spacing.Small
        };

        layout.Widgets.Add(ThemedUIFactory.CreateLabel("Command Deck Scale", ThemeManager.LabelTheme.Heading));

        var description = ThemedUIFactory.CreateSecondaryLabel("Resize Myra panels, fonts, and controls.");
        description.Wrap = true;
        layout.Widgets.Add(description);

        var sliderRow = new Grid
        {
            ColumnSpacing = ThemeManager.Spacing.Small
        };
        sliderRow.ColumnsProportions.Add(new Proportion(ProportionType.Fill));
        sliderRow.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
        sliderRow.RowsProportions.Add(new Proportion(ProportionType.Auto));

        _scaleSlider = new HorizontalSlider
        {
            Minimum = 80,
            Maximum = 160,
            Value = _settings.UiScalePercent,
            Width = ThemeManager.ScalePixels(180),
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(_scaleSlider, 0);
        sliderRow.Widgets.Add(_scaleSlider);

        _scaleValueLabel = ThemedUIFactory.CreateLabel($"{_settings.UiScalePercent}%", ThemeManager.LabelTheme.Heading);
        _scaleValueLabel.Width = ThemeManager.ScalePixels(54);
        _scaleValueLabel.HorizontalAlignment = HorizontalAlignment.Right;
        _scaleValueLabel.VerticalAlignment = VerticalAlignment.Center;
        Grid.SetColumn(_scaleValueLabel, 1);
        sliderRow.Widgets.Add(_scaleValueLabel);

        layout.Widgets.Add(sliderRow);

        _previewLabel = ThemedUIFactory.CreateSmallLabel(string.Empty);
        _previewLabel.TextColor = ThemeManager.Colors.TextSecondary;
        layout.Widgets.Add(_previewLabel);

        _scaleSlider.ValueChanged += (s, e) =>
        {
            if (_scaleSlider != null)
            {
                PreviewScale((int)Math.Round(_scaleSlider.Value));
            }
        };

        panel.Widgets.Add(layout);
        return panel;
    }

    private Widget BuildPresetPanel()
    {
        var panel = ThemedUIFactory.CreateGameplayPanel();
        panel.HorizontalAlignment = HorizontalAlignment.Stretch;

        var layout = new VerticalStackPanel
        {
            Spacing = ThemeManager.Spacing.Small
        };

        layout.Widgets.Add(ThemedUIFactory.CreateSmallLabel("Quick presets"));

        var buttons = new HorizontalStackPanel
        {
            Spacing = ThemeManager.Spacing.Small,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        buttons.Widgets.Add(CreatePresetButton("90%", 90));
        buttons.Widgets.Add(CreatePresetButton("100%", 100));
        buttons.Widgets.Add(CreatePresetButton("110%", 110));
        buttons.Widgets.Add(CreatePresetButton("125%", 125));

        layout.Widgets.Add(buttons);
        panel.Widgets.Add(layout);
        return panel;
    }

    private Widget BuildButtonRow()
    {
        var buttons = new HorizontalStackPanel
        {
            Spacing = ThemeManager.Spacing.Small,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        var applyButton = ThemedUIFactory.CreateButton("Apply", 120, ThemeManager.Sizes.ButtonSmallHeight, ThemeManager.ButtonTheme.Primary);
        applyButton.Click += (s, e) => ApplyScale();
        buttons.Widgets.Add(applyButton);

        var resetButton = ThemedUIFactory.CreateButton("Reset", 120, ThemeManager.Sizes.ButtonSmallHeight);
        resetButton.Click += (s, e) => ResetScale();
        buttons.Widgets.Add(resetButton);

        return buttons;
    }

    private Myra.Graphics2D.UI.Button CreatePresetButton(string label, int scale)
    {
        var button = ThemedUIFactory.CreateButton(label, ThemeManager.ScalePixels(62), ThemeManager.Sizes.ButtonSmallHeight);
        button.Click += (s, e) => SetScale(scale);
        return button;
    }

    private void SetScale(int scale)
    {
        if (_scaleSlider == null)
        {
            return;
        }

        _scaleSlider.Value = scale;
    }

    private void ResetScale()
    {
        SetScale(100);
    }

    private void ApplyScale()
    {
        int scale = (int)Math.Round(_scaleSlider?.Value ?? _settings.UiScalePercent);
        scale = Math.Clamp(scale, 80, 160);

        _settings.UiScalePercent = scale;
        _settings.Normalize();
        _settings.Save();
        SaveState();
        GameFeedbackBus.PublishSuccess("UI scale updated", $"Command deck resized to {scale}%.");
    }

    private void PreviewScale(int scale)
    {
        scale = Math.Clamp(scale, 80, 160);
        UpdateScaleDisplay(scale);

        if (_settings.UiScalePercent == scale)
        {
            return;
        }

        _onPreviewScaleChanged(scale);
    }

    private void UpdateScaleDisplay(int scale)
    {
        if (_scaleValueLabel != null)
        {
            _scaleValueLabel.Text = $"{scale}%";
        }

        if (_previewLabel != null)
        {
            _previewLabel.Text = scale switch
            {
                <= 90 => "Compact bridge fit",
                <= 105 => "Standard command deck",
                <= 125 => "Large tactical console",
                _ => "Max bridge readability"
            };
        }
    }
}

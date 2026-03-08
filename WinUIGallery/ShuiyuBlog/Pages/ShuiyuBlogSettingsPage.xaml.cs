// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using WinUIGallery.Helpers;

namespace WinUIGallery.ShuiyuBlog.Pages;

public sealed partial class ShuiyuBlogSettingsPage : Page
{
    private readonly ToggleSwitch _compactSidebarToggle;
    private readonly Slider _sidebarWidthSlider;
    private readonly TextBlock _sidebarWidthValueTextBlock;
    private readonly ToggleSwitch _wrapTextToggle;
    private readonly ToggleSwitch _confirmExternalToggle;
    private readonly ToggleSwitch _startupTipToggle;
    private readonly ComboBox _defaultSectionComboBox;

    public ShuiyuBlogSettingsPage()
    {
        _compactSidebarToggle = new ToggleSwitch
        {
            Header = "紧凑侧边栏",
            OffContent = "关闭",
            OnContent = "开启",
            IsOn = SettingsHelper.Current.BlogCompactSidebar,
        };
        _compactSidebarToggle.Toggled += CompactSidebarToggle_Toggled;

        _sidebarWidthSlider = new Slider
        {
            Header = "侧边栏宽度",
            Minimum = 220,
            Maximum = 460,
            StepFrequency = 2,
            SmallChange = 2,
            LargeChange = 20,
            Value = SettingsHelper.Current.BlogSidebarWidth,
        };
        _sidebarWidthSlider.ValueChanged += (_, _) => SidebarWidthSlider_ValueChanged();

        _sidebarWidthValueTextBlock = new TextBlock
        {
            Opacity = 0.78,
            Text = $"当前宽度: {SettingsHelper.Current.BlogSidebarWidth}px",
        };

        _wrapTextToggle = new ToggleSwitch
        {
            Header = "文档自动换行",
            OffContent = "关闭",
            OnContent = "开启",
            IsOn = SettingsHelper.Current.BlogWrapDocumentText,
        };
        _wrapTextToggle.Toggled += WrapTextToggle_Toggled;

        _confirmExternalToggle = new ToggleSwitch
        {
            Header = "外链打开前确认",
            OffContent = "关闭",
            OnContent = "开启",
            IsOn = SettingsHelper.Current.BlogConfirmExternalNavigation,
        };
        _confirmExternalToggle.Toggled += ConfirmExternalToggle_Toggled;

        _startupTipToggle = new ToggleSwitch
        {
            Header = "启动时显示提示",
            OffContent = "关闭",
            OnContent = "开启",
            IsOn = SettingsHelper.Current.BlogShowStartupTips,
        };
        _startupTipToggle.Toggled += StartupTipToggle_Toggled;

        _defaultSectionComboBox = new ComboBox
        {
            Header = "默认打开分区",
            MinWidth = 220,
        };
        _defaultSectionComboBox.Items.Add("全部");
        _defaultSectionComboBox.Items.Add("专栏");
        _defaultSectionComboBox.Items.Add("项目");
        _defaultSectionComboBox.Items.Add("媒体");
        _defaultSectionComboBox.Items.Add("工具");
        _defaultSectionComboBox.Items.Add("用户");
        _defaultSectionComboBox.Items.Add("视频归档");
        _defaultSectionComboBox.SelectedItem = SettingsHelper.Current.BlogDefaultSection;
        _defaultSectionComboBox.SelectionChanged += DefaultSectionComboBox_SelectionChanged;

        BuildPageLayout();
    }

    private void BuildPageLayout()
    {
        Border hero = new()
        {
            Padding = new Thickness(16),
            CornerRadius = new CornerRadius(12),
            Background = Application.Current.Resources["CardBackgroundFillColorSecondaryBrush"] as Microsoft.UI.Xaml.Media.Brush,
            Child = new StackPanel
            {
                Spacing = 6,
                Children =
                {
                    new TextBlock
                    {
                        Text = "水鱼 Blog 设置",
                        FontSize = 30,
                        FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    },
                    new TextBlock
                    {
                        Text = "新增原网页风格相关配置项，控制侧边栏、文档阅读和外链行为。",
                        Opacity = 0.82,
                    },
                },
            },
        };

        StackPanel cards = new() { Spacing = 10 };
        cards.Children.Add(CreateSettingCard("布局", "控制导航和初始分区。", _compactSidebarToggle, _sidebarWidthSlider, _sidebarWidthValueTextBlock, _defaultSectionComboBox));
        cards.Children.Add(CreateSettingCard("阅读", "控制原生文档页面呈现。", _wrapTextToggle));
        cards.Children.Add(CreateSettingCard("交互", "控制外链安全和启动提示。", _confirmExternalToggle, _startupTipToggle));

        Grid root = new()
        {
            Padding = new Thickness(20),
            RowSpacing = 12,
        };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        root.Children.Add(hero);
        ScrollViewer scroll = new() { Content = cards };
        Grid.SetRow(scroll, 1);
        root.Children.Add(scroll);

        Content = root;
    }

    private static Border CreateSettingCard(string title, string description, params UIElement[] elements)
    {
        StackPanel panel = new() { Spacing = 8 };
        panel.Children.Add(new TextBlock
        {
            Text = title,
            FontSize = 20,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
        });
        panel.Children.Add(new TextBlock
        {
            Text = description,
            Opacity = 0.75,
            TextWrapping = TextWrapping.WrapWholeWords,
        });

        foreach (UIElement element in elements)
        {
            panel.Children.Add(element);
        }

        return new Border
        {
            Padding = new Thickness(14),
            CornerRadius = new CornerRadius(10),
            Background = Application.Current.Resources["CardBackgroundFillColorDefaultBrush"] as Microsoft.UI.Xaml.Media.Brush,
            Child = panel,
        };
    }

    private void CompactSidebarToggle_Toggled(object sender, RoutedEventArgs e)
    {
        SettingsHelper.Current.BlogCompactSidebar = _compactSidebarToggle.IsOn;
        App.MainWindow.RefreshBlogLayoutSettings();
    }

    private void SidebarWidthSlider_ValueChanged()
    {
        int width = (int)Math.Round(_sidebarWidthSlider.Value);
        SettingsHelper.Current.BlogSidebarWidth = width;
        _sidebarWidthValueTextBlock.Text = $"当前宽度: {SettingsHelper.Current.BlogSidebarWidth}px";
        App.MainWindow.RefreshBlogLayoutSettings();
    }

    private void WrapTextToggle_Toggled(object sender, RoutedEventArgs e)
    {
        SettingsHelper.Current.BlogWrapDocumentText = _wrapTextToggle.IsOn;
    }

    private void ConfirmExternalToggle_Toggled(object sender, RoutedEventArgs e)
    {
        SettingsHelper.Current.BlogConfirmExternalNavigation = _confirmExternalToggle.IsOn;
    }

    private void StartupTipToggle_Toggled(object sender, RoutedEventArgs e)
    {
        SettingsHelper.Current.BlogShowStartupTips = _startupTipToggle.IsOn;
    }

    private void DefaultSectionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_defaultSectionComboBox.SelectedItem is string section)
        {
            SettingsHelper.Current.BlogDefaultSection = section;
        }
    }
}

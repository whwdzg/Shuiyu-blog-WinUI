// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.UI;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.IO;
using WinUIGallery.Helpers;
using WinUIGallery.ShuiyuBlog.Models;
using WinUIGallery.ShuiyuBlog.Pages;

namespace WinUIGallery;

public sealed partial class MainWindow : Window
{
    private readonly Grid _rootLayout;
    private readonly TitleBar _titleBar;
    private readonly Button _titleBarBackButton;
    private readonly Button _titleBarPaneButton;
    private readonly NavigationView _rootNavigationView;
    private readonly Frame _rootFrame;

    public NavigationView NavigationView
    {
        get { return _rootNavigationView; }
    }

    public Action? NavigationViewLoaded { get; set; }

    public MainWindow()
    {
        _rootFrame = new Frame
        {
            Background = new SolidColorBrush(Colors.Transparent),
        };

        _titleBar = new TitleBar
        {
            Title = "水鱼 Blog (Shuiyu Blog)",
            HorizontalAlignment = HorizontalAlignment.Stretch,
            IsBackButtonVisible = false,
            IsPaneToggleButtonVisible = false,
        };
        _titleBar.IconSource = new ImageIconSource
        {
            ImageSource = new BitmapImage(new Uri("ms-appx:///Assets/Tiles/ShuiyuBlogIcon.ico")),
        };

        _titleBarBackButton = new Button
        {
            Width = 32,
            Height = 32,
            Padding = new Thickness(0),
            Content = new SymbolIcon(Symbol.Back),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        _titleBarBackButton.Click += TitleBarBackButton_Click;

        _titleBarPaneButton = new Button
        {
            Width = 32,
            Height = 32,
            Padding = new Thickness(0),
            Content = new SymbolIcon(Symbol.GlobalNavigationButton),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        _titleBarPaneButton.Click += TitleBarPaneButton_Click;

        StackPanel leftHeaderButtons = new()
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6,
            VerticalAlignment = VerticalAlignment.Center,
        };
        leftHeaderButtons.Children.Add(_titleBarBackButton);
        leftHeaderButtons.Children.Add(_titleBarPaneButton);
        _titleBar.LeftHeader = leftHeaderButtons;

        _rootNavigationView = new NavigationView
        {
            IsBackEnabled = false,
            IsSettingsVisible = false,
            IsBackButtonVisible = NavigationViewBackButtonVisible.Collapsed,
            IsPaneToggleButtonVisible = false,
            IsTitleBarAutoPaddingEnabled = false,
            PaneDisplayMode = NavigationViewPaneDisplayMode.Left,
            Content = _rootFrame,
            Background = new SolidColorBrush(Colors.Transparent),
        };
        _rootNavigationView.SelectionChanged += RootNavigationView_SelectionChanged;
        _rootFrame.Navigated += RootFrame_Navigated;
        BuildMenuItems();

        _rootLayout = new Grid();
        _rootLayout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        _rootLayout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        Grid.SetRow(_titleBar, 0);
        Grid.SetRow(_rootNavigationView, 1);
        _rootLayout.Children.Add(_titleBar);
        _rootLayout.Children.Add(_rootNavigationView);
        Content = _rootLayout;

        ConfigureWindowAppearance();

        Title = "水鱼 Blog (Shuiyu Blog)";
        AppWindow.SetIcon("Assets/Tiles/ShuiyuBlogIcon.ico");

        if (_rootNavigationView.MenuItems[0] is NavigationViewItem firstItem)
        {
            _rootNavigationView.SelectedItem = firstItem;
        }

        UpdateTitleBarBackButtonState();
        RefreshBlogLayoutSettings();
        NavigateByTag("home:index");

        _rootNavigationView.Loaded += async (_, _) =>
        {
            NavigationViewLoaded?.Invoke();
            if (SettingsHelper.Current.BlogShowStartupTips)
            {
                ContentDialog dialog = new()
                {
                    XamlRoot = _rootNavigationView.XamlRoot,
                    Title = "欢迎使用水鱼 Blog 原生版",
                    Content = "侧边栏已对齐网页功能入口（不含 Legacy-1.0），可在设置页调整默认分区与外链策略。",
                    PrimaryButtonText = "知道了",
                };

                await dialog.ShowAsync();
            }
        };
    }

    private void ConfigureWindowAppearance()
    {
        SystemBackdrop = new MicaBackdrop
        {
            Kind = MicaKind.BaseAlt,
        };

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(_titleBar);

        AppWindowTitleBar appWindowTitleBar = AppWindow.TitleBar;
        appWindowTitleBar.PreferredTheme = TitleBarTheme.UseDefaultAppMode;
        appWindowTitleBar.PreferredHeightOption = TitleBarHeightOption.Standard;
        appWindowTitleBar.ButtonBackgroundColor = Colors.Transparent;
        appWindowTitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
    }

    private void RootFrame_Navigated(object sender, Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        UpdateTitleBarBackButtonState();
    }

    private void TitleBarPaneButton_Click(object sender, RoutedEventArgs e)
    {
        _rootNavigationView.IsPaneOpen = !_rootNavigationView.IsPaneOpen;
    }

    private void TitleBarBackButton_Click(object sender, RoutedEventArgs e)
    {
        if (_rootFrame.CanGoBack)
        {
            _rootFrame.GoBack();
        }

        UpdateTitleBarBackButtonState();
    }

    private void UpdateTitleBarBackButtonState()
    {
        bool canGoBack = _rootFrame.CanGoBack;
        _titleBarBackButton.IsEnabled = canGoBack;
        _titleBarBackButton.Opacity = canGoBack ? 1.0 : 0.5;
    }

    private void BuildMenuItems()
    {
        _rootNavigationView.MenuItems.Add(CreateMenuItem("主页", Symbol.Home, "home:index"));

        NavigationViewItem projectsGroup = CreateParentItem("项目", Symbol.Library);
        projectsGroup.MenuItems.Add(CreateMenuItem("Modrinth", Symbol.World, "doc:projects/modrinth.html"));
        projectsGroup.MenuItems.Add(CreateMenuItem("更好的合成配方", Symbol.Paste, "doc:projects/better-crafting-recipes.html"));
        projectsGroup.MenuItems.Add(CreateMenuItem("更好附魔", Symbol.Edit, "doc:projects/better-enchantments.html"));
        projectsGroup.MenuItems.Add(CreateMenuItem("更好的生物掉落", Symbol.AllApps, "doc:projects/better-mob-drop.html"));
        projectsGroup.MenuItems.Add(CreateMenuItem("魔法染料", Symbol.Edit, "doc:projects/magical-dye.html"));
        projectsGroup.MenuItems.Add(CreateMenuItem("Github", Symbol.Library, "doc:projects/github.html"));
        _rootNavigationView.MenuItems.Add(projectsGroup);

        NavigationViewItem toolsGroup = CreateParentItem("工具", Symbol.Repair);
        toolsGroup.MenuItems.Add(CreateMenuItem("Base64 编解码", Symbol.Paste, "tool:base64"));
        toolsGroup.MenuItems.Add(CreateMenuItem("BaseX 编解码", Symbol.Switch, "tool:basex"));
        toolsGroup.MenuItems.Add(CreateMenuItem("Unicode 转换", Symbol.FontColor, "tool:unicode"));
        toolsGroup.MenuItems.Add(CreateMenuItem("进制转换", Symbol.Refresh, "tool:baseconvert"));
        toolsGroup.MenuItems.Add(CreateMenuItem("科学计算器", Symbol.Calculator, "tool:calculator"));
        toolsGroup.MenuItems.Add(CreateMenuItem("MC 投影查看器", Symbol.Map, "tool:mcprojection"));
        _rootNavigationView.MenuItems.Add(toolsGroup);

        NavigationViewItem columnGroup = CreateParentItem("专栏", Symbol.Document);
        columnGroup.MenuItems.Add(CreateMenuItem("留言板", Symbol.Comment, "doc:column/comments.html"));
        _rootNavigationView.MenuItems.Add(columnGroup);

        NavigationViewItem mediaGroup = CreateParentItem("媒体", Symbol.Video);
        mediaGroup.MenuItems.Add(CreateMenuItem("视频", Symbol.Video, "media:video"));
        mediaGroup.MenuItems.Add(CreateMenuItem("音乐", Symbol.Audio, "media:music"));
        mediaGroup.MenuItems.Add(CreateMenuItem("哔哩哔哩", Symbol.Globe, "doc:media/bilibili.html"));
        _rootNavigationView.MenuItems.Add(mediaGroup);

        _rootNavigationView.MenuItems.Add(CreateMenuItem("关于", Symbol.Help, "doc:about.html"));
    }

    private static NavigationViewItem CreateParentItem(string text, Symbol icon)
    {
        return new NavigationViewItem
        {
            Content = text,
            Icon = new SymbolIcon(icon),
            SelectsOnInvoked = false,
        };
    }

    private static NavigationViewItem CreateMenuItem(string text, Symbol icon, string tag)
    {
        return new NavigationViewItem
        {
            Content = text,
            Icon = new SymbolIcon(icon),
            Tag = tag,
        };
    }

    private void RootNavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItemContainer?.Tag is not string tag)
        {
            tag = "home:index";
        }

        NavigateByTag(tag);
    }

    public void NavigateByTag(string tag)
    {
        if (tag.StartsWith("home:", StringComparison.Ordinal))
        {
            _rootFrame.Navigate(typeof(ShuiyuHomePage));
            return;
        }

        if (tag.StartsWith("blog:", StringComparison.Ordinal))
        {
            string section = tag[5..];
            _rootFrame.Navigate(typeof(ShuiyuBlogPage), section);
            return;
        }

        if (tag.StartsWith("tool:", StringComparison.Ordinal))
        {
            string tool = tag[5..];
            Type targetPage = tool switch
            {
                "base64" => typeof(Base64ToolPage),
                "baseconvert" => typeof(BaseConvertToolPage),
                "basex" => typeof(BaseXToolPage),
                "unicode" => typeof(UnicodeToolPage),
                "calculator" => typeof(ScientificCalculatorPage),
                "mcprojection" => typeof(McProjectionToolPage),
                _ => typeof(Base64ToolPage),
            };

            _rootFrame.Navigate(targetPage);
            return;
        }

        if (tag.StartsWith("media:", StringComparison.Ordinal))
        {
            string media = tag[6..];
            Type targetPage = media switch
            {
                "music" => typeof(NativeMusicPage),
                "video" => typeof(NativeVideoPage),
                _ => typeof(NativeMusicPage),
            };

            _rootFrame.Navigate(targetPage);
            return;
        }

        if (tag.StartsWith("template:", StringComparison.Ordinal))
        {
            string section = tag[9..];
            _rootFrame.Navigate(typeof(CardTemplatePage), section);
            return;
        }

        if (tag.StartsWith("doc:", StringComparison.Ordinal))
        {
            string relativePath = tag[4..];
            _rootFrame.Navigate(typeof(ShuiyuReplicaPage), relativePath);
            return;
        }

        _rootFrame.Navigate(typeof(ShuiyuHomePage));
    }

    private static BlogEntry CreateEntryForPath(string relativePath)
    {
        string normalizedPath = relativePath.Replace('\\', '/').TrimStart('/');
        string section = normalizedPath.Split('/', StringSplitOptions.RemoveEmptyEntries) switch
        {
            string[] parts when parts.Length > 0 => parts[0].ToLowerInvariant() switch
            {
                "column" => "专栏",
                "projects" => "项目",
                "media" => "媒体",
                "tool" => "工具",
                "user" => "用户",
                "video-archive" => "视频归档",
                "includes" => "公共片段",
                "css" => "样式",
                "js" => "脚本",
                _ => "根目录",
            },
            _ => "根目录",
        };

        return new BlogEntry
        {
            RelativePath = normalizedPath,
            Title = Path.GetFileNameWithoutExtension(normalizedPath),
            Section = section,
            FileType = Path.GetExtension(normalizedPath).TrimStart('.').ToUpperInvariant(),
        };
    }

    public void RefreshBlogLayoutSettings()
    {
        bool compact = SettingsHelper.Current.BlogCompactSidebar;
        _rootNavigationView.OpenPaneLength = compact ? 230 : 320;
        _rootNavigationView.CompactPaneLength = compact ? 44 : 48;
    }

    public Frame GetRootFrame()
    {
        return _rootFrame;
    }

    public void Navigate(Type pageType, object? targetPageArguments = null, NavigationTransitionInfo? navigationTransitionInfo = null)
    {
        _rootFrame.Navigate(pageType, targetPageArguments, navigationTransitionInfo);
    }

    public void EnsureNavigationSelection(string id)
    {
        // Compatibility stub for legacy pages in this repository.
    }

    public void AddNavigationMenuItems()
    {
        // Compatibility stub for legacy startup flow in this repository.
    }
}

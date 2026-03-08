// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using System;
using WinUIGallery.ShuiyuBlog.Pages;

namespace WinUIGallery;

public sealed partial class MainWindow : Window
{
    private readonly NavigationView _rootNavigationView;
    private readonly Frame _rootFrame;

    public NavigationView NavigationView
    {
        get { return _rootNavigationView; }
    }

    public Action? NavigationViewLoaded { get; set; }

    public MainWindow()
    {
        _rootFrame = new Frame();
        _rootNavigationView = new NavigationView
        {
            IsBackEnabled = false,
            IsSettingsVisible = false,
            IsTitleBarAutoPaddingEnabled = false,
            PaneDisplayMode = NavigationViewPaneDisplayMode.Left,
            Content = _rootFrame,
        };
        _rootNavigationView.SelectionChanged += RootNavigationView_SelectionChanged;
        BuildMenuItems();
        Content = _rootNavigationView;

        Title = "水鱼 Blog (Shuiyu Blog)";
        AppWindow.SetIcon("Assets/Tiles/ShuiyuBlogIcon.ico");

        if (_rootNavigationView.MenuItems[0] is NavigationViewItem firstItem)
        {
            _rootNavigationView.SelectedItem = firstItem;
        }

        NavigateByTag("blog:全部");

        _rootNavigationView.Loaded += (_, _) => NavigationViewLoaded?.Invoke();
    }

    private void BuildMenuItems()
    {
        _rootNavigationView.MenuItems.Add(CreateMenuItem("内容总览", Symbol.Home, "blog:全部"));
        _rootNavigationView.MenuItems.Add(CreateMenuItem("专栏", Symbol.Document, "blog:专栏"));
        _rootNavigationView.MenuItems.Add(CreateMenuItem("旧版归档", Symbol.Library, "blog:旧版归档"));
        _rootNavigationView.MenuItems.Add(new NavigationViewItemSeparator());
        _rootNavigationView.MenuItems.Add(CreateMenuItem("Base64 工具", Symbol.Paste, "tool:base64"));
        _rootNavigationView.MenuItems.Add(CreateMenuItem("进制转换", Symbol.Refresh, "tool:baseconvert"));
        _rootNavigationView.MenuItems.Add(CreateMenuItem("科学计算器", Symbol.Calculator, "tool:calculator"));
        _rootNavigationView.MenuItems.Add(CreateMenuItem("原生音乐", Symbol.Audio, "media:music"));
        _rootNavigationView.MenuItems.Add(CreateMenuItem("原生视频", Symbol.Video, "media:video"));
        _rootNavigationView.MenuItems.Add(new NavigationViewItemSeparator());
        _rootNavigationView.MenuItems.Add(CreateMenuItem("项目详情", Symbol.Library, "template:projects"));
        _rootNavigationView.MenuItems.Add(CreateMenuItem("媒体详情", Symbol.Video, "template:media"));
        _rootNavigationView.MenuItems.Add(CreateMenuItem("用户详情", Symbol.Contact, "template:user"));
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
            tag = "blog:全部";
        }

        NavigateByTag(tag);
    }

    private void NavigateByTag(string tag)
    {
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
                "calculator" => typeof(ScientificCalculatorPage),
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

        _rootFrame.Navigate(typeof(ShuiyuBlogPage), "全部");
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

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace WinUIGallery.ShuiyuBlog.Pages;

public sealed partial class ShuiyuHomePage : Page
{
    public ShuiyuHomePage()
    {
        BuildPageLayout();
    }

    private void BuildPageLayout()
    {
        Border heroCard = new()
        {
            Padding = new Thickness(18),
            CornerRadius = new CornerRadius(12),
            Background = Application.Current.Resources["CardBackgroundFillColorSecondaryBrush"] as Microsoft.UI.Xaml.Media.Brush,
            Child = new StackPanel
            {
                Spacing = 8,
                Children =
                {
                    new TextBlock { Text = "主页", Opacity = 0.78 },
                    new TextBlock
                    {
                        Text = "水鱼 Blog",
                        FontSize = 34,
                        FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    },
                    new TextBlock
                    {
                        Text = "滄海水魚个人网站，陈列项目、工具、社交媒体。",
                        Opacity = 0.88,
                        TextWrapping = TextWrapping.WrapWholeWords,
                    },
                },
            },
        };

        StackPanel actionsGrid = new() { Spacing = 8 };
        StackPanel actionRow1 = new() { Orientation = Orientation.Horizontal, Spacing = 8 };
        actionRow1.Children.Add(CreateActionButton("Modrinth 项目", "doc:projects/modrinth.html"));
        actionRow1.Children.Add(CreateActionButton("留言板", "doc:column/comments.html"));
        actionRow1.Children.Add(CreateActionButton("哔哩哔哩视频", "doc:media/bilibili.html"));

        StackPanel actionRow2 = new() { Orientation = Orientation.Horizontal, Spacing = 8 };
        actionRow2.Children.Add(CreateActionButton("更好的合成配方", "doc:projects/better-crafting-recipes.html"));
        actionRow2.Children.Add(CreateActionButton("更好的生物掉落", "doc:projects/better-mob-drop.html"));
        actionRow2.Children.Add(CreateActionButton("更好附魔", "doc:projects/better-enchantments.html"));

        actionsGrid.Children.Add(actionRow1);
        actionsGrid.Children.Add(actionRow2);

        StackPanel summary = new() { Spacing = 8 };
        summary.Children.Add(new TextBlock
        {
            Text = "当前头条",
            FontSize = 24,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
        });
        summary.Children.Add(CreateFeature("单页壳层", "header、sidebar、footer 共享壳层，由 includes/shell.html 注入。"));
        summary.Children.Add(CreateFeature("交互模块", "滚动返回、搜索、lightbox 与设置面板按需加载。"));
        summary.Children.Add(CreateFeature("资源归档", "projects、column、video-archive 与 Legacy 1.0 记录多个迭代主题。"));
        summary.Children.Add(CreateFeature("技术迭代", "PWA 注册脚本、多语言 JSON 与主题系统构成 2.0 基础。"));

        StackPanel picks = new() { Spacing = 8 };
        picks.Children.Add(new TextBlock
        {
            Text = "精选页面",
            FontSize = 24,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
        });
        picks.Children.Add(CreatePick("更好的合成配方", "新增许多便捷的合成表，在不破坏难度下优化流程。", "doc:projects/better-crafting-recipes.html"));
        picks.Children.Add(CreatePick("更好附魔", "使更多物品可以被附魔，增强附魔使用对象。", "doc:projects/better-enchantments.html"));
        picks.Children.Add(CreatePick("更好的生物掉落", "优化生物掉落，使生物掉落更加合理。", "doc:projects/better-mob-drop.html"));
        picks.Children.Add(CreatePick("Modrinth", "收录在 modrinth 的插件、数据包、模组。", "doc:projects/modrinth.html"));
        picks.Children.Add(CreatePick("Github", "收录在 Github 的插件、数据包、模组。", "doc:projects/github.html"));
        picks.Children.Add(CreatePick("留言板", "留言板承载现场即兴感，结合 lightbox 提供翻阅体验。", "doc:column/comments.html"));
        picks.Children.Add(CreatePick("哔哩哔哩存档", "通过 media 集合哔哩哔哩视频与字幕，支持播放与筛选。", "doc:media/bilibili.html"));
        picks.Children.Add(CreatePick("站点说明", "关于页面与 manifest 提供 PWA 元数据。", "doc:about.html"));

        StackPanel rootPanel = new() { Spacing = 16 };
        rootPanel.Children.Add(heroCard);
        rootPanel.Children.Add(actionsGrid);
        rootPanel.Children.Add(summary);
        rootPanel.Children.Add(picks);

        Content = new ScrollViewer
        {
            Padding = new Thickness(20),
            Content = rootPanel,
        };
    }

    private static Border CreateFeature(string title, string desc)
    {
        StackPanel panel = new() { Spacing = 4 };
        panel.Children.Add(new TextBlock { Text = title, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, FontSize = 18 });
        panel.Children.Add(new TextBlock { Text = desc, Opacity = 0.82, TextWrapping = TextWrapping.WrapWholeWords });

        return new Border
        {
            Padding = new Thickness(12),
            CornerRadius = new CornerRadius(10),
            Background = Application.Current.Resources["CardBackgroundFillColorDefaultBrush"] as Microsoft.UI.Xaml.Media.Brush,
            Child = panel,
        };
    }

    private static Border CreatePick(string title, string desc, string tag)
    {
        Button button = CreateActionButton("前往", tag);

        StackPanel panel = new() { Spacing = 6 };
        panel.Children.Add(new TextBlock { Text = title, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, FontSize = 18 });
        panel.Children.Add(new TextBlock { Text = desc, Opacity = 0.82, TextWrapping = TextWrapping.WrapWholeWords });
        panel.Children.Add(button);

        return new Border
        {
            Padding = new Thickness(12),
            CornerRadius = new CornerRadius(10),
            Background = Application.Current.Resources["CardBackgroundFillColorDefaultBrush"] as Microsoft.UI.Xaml.Media.Brush,
            Child = panel,
        };
    }

    private static Button CreateActionButton(string title, string tag)
    {
        Button button = new()
        {
            Content = title,
            Tag = tag,
            HorizontalAlignment = HorizontalAlignment.Left,
        };
        button.Click += ActionButton_Click;
        return button;
    }

    private static void ActionButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string tag)
        {
            App.MainWindow.NavigateByTag(tag);
        }
    }
}

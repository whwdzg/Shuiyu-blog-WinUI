// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace WinUIGallery.ShuiyuBlog.Pages;

public sealed class VideoArchivePage : Page
{
    public VideoArchivePage()
    {
        StackPanel panel = new() { Spacing = 10 };
        panel.Children.Add(new TextBlock
        {
            Text = "页面已迁移",
            FontSize = 30,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
        });
        panel.Children.Add(new TextBlock
        {
            Text = "list-bilivideo 页面已迁移到 media/bilibili。",
            Opacity = 0.82,
        });

        Button goButton = new() { Content = "前往 哔哩哔哩 页面" };
        goButton.Click += (_, _) => App.MainWindow.NavigateByTag("doc:media/bilibili.html");
        panel.Children.Add(goButton);

        Content = new Grid
        {
            Padding = new Thickness(20),
            Children = { panel },
        };
    }
}

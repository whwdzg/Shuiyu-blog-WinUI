// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.ObjectModel;
using WinUIGallery.Helpers;
using WinUIGallery.ShuiyuBlog.Models;
using WinUIGallery.ShuiyuBlog.Services;
using Windows.System;

namespace WinUIGallery.ShuiyuBlog.Pages;

public sealed partial class CardTemplatePage : Page
{
    private readonly DetailCardService _detailCardService = new();
    private readonly ObservableCollection<DetailCardItem> _cards = [];
    private string _sectionName = "projects";

    public CardTemplatePage()
    {
        InitializeComponent();
        CardsRepeater.ItemsSource = _cards;
        Loaded += CardTemplatePage_Loaded;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is string section && !string.IsNullOrWhiteSpace(section))
        {
            _sectionName = section;
        }
    }

    private void CardTemplatePage_Loaded(object sender, RoutedEventArgs e)
    {
        HeaderTextBlock.Text = _sectionName switch
        {
            "projects" => "项目详情",
            "media" => "媒体详情",
            "user" => "用户详情",
            _ => "详情模板",
        };

        SubHeaderTextBlock.Text = "卡片化布局 + 图片预览 + 外链按钮";

        _cards.Clear();
        foreach (DetailCardItem item in _detailCardService.LoadCards(_sectionName))
        {
            _cards.Add(item);
        }
    }

    private async void OpenExternalButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not string url)
        {
            return;
        }

        if (!System.Uri.TryCreate(url, System.UriKind.Absolute, out System.Uri? uri))
        {
            return;
        }

        if (SettingsHelper.Current.BlogConfirmExternalNavigation)
        {
            ContentDialog dialog = new()
            {
                XamlRoot = XamlRoot,
                Title = "打开外部链接",
                Content = uri.AbsoluteUri,
                PrimaryButtonText = "继续",
                CloseButtonText = "取消",
                DefaultButton = ContentDialogButton.Close,
            };

            ContentDialogResult result = await dialog.ShowAsync();
            if (result != ContentDialogResult.Primary)
            {
                return;
            }
        }

        _ = Launcher.LaunchUriAsync(uri);
    }
}

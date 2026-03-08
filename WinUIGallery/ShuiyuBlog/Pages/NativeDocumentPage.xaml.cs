// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Navigation;
using System;
using Windows.System;
using WinUIGallery.ShuiyuBlog.Models;
using WinUIGallery.ShuiyuBlog.Services;

namespace WinUIGallery.ShuiyuBlog.Pages;

public sealed partial class NativeDocumentPage : Page
{
    private readonly NativeDocumentService _documentService = new();
    private readonly TextBlock _titleTextBlock;
    private readonly TextBlock _metaTextBlock;
    private readonly TextBox _contentTextBox;
    private readonly ItemsRepeater _imageRepeater;
    private readonly ItemsRepeater _linkRepeater;

    public NativeDocumentPage()
    {
        _titleTextBlock = new TextBlock
        {
            FontSize = 28,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Text = "原生页面",
            TextWrapping = TextWrapping.WrapWholeWords,
        };

        _metaTextBlock = new TextBlock
        {
            Opacity = 0.75,
            TextWrapping = TextWrapping.WrapWholeWords,
        };

        _contentTextBox = new TextBox
        {
            AcceptsReturn = true,
            IsReadOnly = true,
            MinHeight = 300,
            TextWrapping = TextWrapping.Wrap,
        };

        _imageRepeater = new ItemsRepeater
        {
            Layout = new StackLayout { Spacing = 10 },
            ItemTemplate = BuildImageTemplate(),
        };

        _linkRepeater = new ItemsRepeater
        {
            Layout = new StackLayout { Spacing = 8 },
            ItemTemplate = BuildLinkTemplate(),
        };

        BuildPageLayout();
    }

    private void BuildPageLayout()
    {
        Grid root = new()
        {
            Padding = new Thickness(20),
            RowSpacing = 10,
        };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        StackPanel header = new() { Spacing = 4 };
        header.Children.Add(_titleTextBlock);
        header.Children.Add(_metaTextBlock);

        StackPanel contentStack = new() { Spacing = 16 };
        contentStack.Children.Add(new Expander { Header = "正文（原生解析）", IsExpanded = true, Content = _contentTextBox });
        contentStack.Children.Add(new Expander { Header = "图片", IsExpanded = true, Content = _imageRepeater });
        contentStack.Children.Add(new Expander { Header = "外部链接", IsExpanded = true, Content = _linkRepeater });

        root.Children.Add(header);
        ScrollViewer contentScrollViewer = new() { Content = contentStack };
        root.Children.Add(contentScrollViewer);
        Grid.SetRow(contentScrollViewer, 1);

        Content = root;
    }

    private static DataTemplate BuildImageTemplate()
    {
        return (DataTemplate)XamlReader.Load(
            "<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' xmlns:model='using:WinUIGallery.ShuiyuBlog.Models' x:DataType='model:NativeDocumentImageItem'>" +
            "<Border Padding='10' CornerRadius='8'><StackPanel Spacing='8'><TextBlock FontWeight='SemiBold' Text='{x:Bind DisplayName}' /><Image Source='{x:Bind SourcePath}' MaxHeight='260' Stretch='Uniform' /><TextBlock Opacity='0.7' Text='{x:Bind SourcePath}' TextWrapping='WrapWholeWords' /></StackPanel></Border>" +
            "</DataTemplate>");
    }

    private static DataTemplate BuildLinkTemplate()
    {
        return (DataTemplate)XamlReader.Load(
            "<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' xmlns:model='using:WinUIGallery.ShuiyuBlog.Models' x:DataType='model:NativeDocumentLinkItem'>" +
            "<Border Padding='10' CornerRadius='8'><StackPanel Spacing='6'><TextBlock FontWeight='SemiBold' Text='{x:Bind DisplayName}' TextWrapping='WrapWholeWords' /><TextBlock Opacity='0.7' Text='{x:Bind Url}' TextWrapping='WrapWholeWords' /><HyperlinkButton Content='打开链接' NavigateUri='{x:Bind Url}' HorizontalAlignment='Left' /></StackPanel></Border>" +
            "</DataTemplate>");
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is not BlogEntry entry)
        {
            _titleTextBlock.Text = "原生页面";
            _metaTextBlock.Text = "未收到有效内容。";
            _contentTextBox.Text = string.Empty;
            return;
        }

        NativeDocumentDetail? detail = await _documentService.LoadAsync(entry);
        if (detail is null)
        {
            _titleTextBlock.Text = entry.Title;
            _metaTextBlock.Text = $"{entry.RelativePath} | {entry.Section}";
            _contentTextBox.Text = "未能读取对应文件。";
            return;
        }

        _titleTextBlock.Text = detail.Title;
        _metaTextBlock.Text = $"{detail.RelativePath} | 分区: {detail.Section} | 类型: {detail.FileType}";
        _contentTextBox.Text = detail.PlainText;
        _imageRepeater.ItemsSource = detail.Images;
        _linkRepeater.ItemsSource = detail.Links;
    }

    public async void OpenLinkButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string urlText } && Uri.TryCreate(urlText, UriKind.Absolute, out Uri? uri))
        {
            _ = await Launcher.LaunchUriAsync(uri);
        }
    }
}

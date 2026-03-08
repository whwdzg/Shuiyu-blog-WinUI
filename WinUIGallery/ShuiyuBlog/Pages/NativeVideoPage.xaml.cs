// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.ObjectModel;
using Windows.Media.Core;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace WinUIGallery.ShuiyuBlog.Pages;

public sealed partial class NativeVideoPage : Page
{
    private readonly ObservableCollection<PlaylistItem> _playlist = [];
    private readonly TextBlock _nowPlayingTextBlock;
    private readonly TextBox _urlTextBox;
    private readonly MediaPlayerElement _videoPlayer;

    public NativeVideoPage()
    {
        _nowPlayingTextBlock = new TextBlock { Opacity = 0.8, Text = "当前未播放" };
        _urlTextBox = new TextBox { PlaceholderText = "输入视频 URL，例如 https://.../video.mp4" };
        _videoPlayer = new MediaPlayerElement { AreTransportControlsEnabled = true, AutoPlay = false };

        ListView playlistListView = new()
        {
            IsItemClickEnabled = true,
            ItemsSource = _playlist,
        };
        playlistListView.ItemClick += PlaylistListView_ItemClick;

        Button playUrlButton = new() { Content = "播放 URL" };
        playUrlButton.Click += PlayUrlButton_Click;
        Button pickVideoButton = new() { Content = "添加本地视频" };
        pickVideoButton.Click += PickVideoButton_Click;

        Grid actionsGrid = new() { ColumnSpacing = 8 };
        actionsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        actionsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        actionsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        actionsGrid.Children.Add(_urlTextBox);
        Grid.SetColumn(playUrlButton, 1);
        actionsGrid.Children.Add(playUrlButton);
        Grid.SetColumn(pickVideoButton, 2);
        actionsGrid.Children.Add(pickVideoButton);

        Border playlistBorder = new()
        {
            Padding = new Thickness(10),
            CornerRadius = new CornerRadius(8),
            Child = new StackPanel
            {
                Spacing = 8,
                Children =
                {
                    new TextBlock { FontSize = 18, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Text = "播放列表" },
                    playlistListView,
                },
            },
        };

        Grid playerGrid = new() { ColumnSpacing = 12 };
        playerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(4, GridUnitType.Star) });
        playerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
        playerGrid.Children.Add(_videoPlayer);
        Grid.SetColumn(playlistBorder, 1);
        playerGrid.Children.Add(playlistBorder);

        Grid root = new()
        {
            Padding = new Thickness(20),
            RowSpacing = 10,
        };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        StackPanel header = new() { Spacing = 4 };
        header.Children.Add(new TextBlock { FontSize = 28, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Text = "原生视频播放器" });
        header.Children.Add(_nowPlayingTextBlock);

        root.Children.Add(header);
        Grid.SetRow(actionsGrid, 1);
        root.Children.Add(actionsGrid);
        Grid.SetRow(playerGrid, 2);
        root.Children.Add(playerGrid);
        Content = root;
    }

    private void PlayUrlButton_Click(object sender, RoutedEventArgs e)
    {
        string input = _urlTextBox.Text?.Trim() ?? string.Empty;
        if (!Uri.TryCreate(input, UriKind.Absolute, out Uri? uri))
        {
            _nowPlayingTextBlock.Text = "URL 无效。";
            return;
        }

        PlaylistItem item = new()
        {
            Name = uri.AbsoluteUri,
            SourceUri = uri,
        };

        _playlist.Add(item);
        PlayItem(item);
    }

    private async void PickVideoButton_Click(object sender, RoutedEventArgs e)
    {
        FileOpenPicker picker = new();
        IntPtr windowHandle = WindowNative.GetWindowHandle(App.MainWindow);
        InitializeWithWindow.Initialize(picker, windowHandle);
        picker.SuggestedStartLocation = PickerLocationId.VideosLibrary;
        picker.ViewMode = PickerViewMode.List;
        picker.FileTypeFilter.Add(".mp4");
        picker.FileTypeFilter.Add(".mkv");
        picker.FileTypeFilter.Add(".mov");
        picker.FileTypeFilter.Add(".avi");
        picker.FileTypeFilter.Add(".webm");

        StorageFile? file = await picker.PickSingleFileAsync();
        if (file is null)
        {
            return;
        }

        PlaylistItem item = new()
        {
            Name = file.Name,
            StorageFile = file,
        };

        _playlist.Add(item);
        PlayItem(item);
    }

    private void PlaylistListView_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is not PlaylistItem item)
        {
            return;
        }

        PlayItem(item);
    }

    private void PlayItem(PlaylistItem item)
    {
        MediaSource source = item.StorageFile is not null
            ? MediaSource.CreateFromStorageFile(item.StorageFile)
            : MediaSource.CreateFromUri(item.SourceUri!);

        _videoPlayer.Source = source;
        _nowPlayingTextBlock.Text = $"正在播放: {item.Name}";
    }

    private sealed class PlaylistItem
    {
        public string Name { get; init; } = string.Empty;

        public Uri? SourceUri { get; init; }

        public StorageFile? StorageFile { get; init; }

        public override string ToString()
        {
            return Name;
        }
    }
}

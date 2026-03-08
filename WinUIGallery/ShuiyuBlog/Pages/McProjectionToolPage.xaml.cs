// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace WinUIGallery.ShuiyuBlog.Pages;

public sealed partial class McProjectionToolPage : Page
{
    private readonly ObservableCollection<BlockStatItem> _stats = [];
    private readonly TextBlock _fileMetaTextBlock;
    private readonly TextBlock _summaryTextBlock;
    private readonly TextBox _previewTextBox;

    public McProjectionToolPage()
    {
        _fileMetaTextBlock = new TextBlock
        {
            Opacity = 0.82,
            TextWrapping = TextWrapping.WrapWholeWords,
            Text = "尚未选择投影文件。",
        };

        _summaryTextBlock = new TextBlock
        {
            TextWrapping = TextWrapping.WrapWholeWords,
            Text = "支持 .litematic / .schematic / .nbt。",
        };

        _previewTextBox = new TextBox
        {
            Header = "文本预览（用于诊断）",
            AcceptsReturn = true,
            IsReadOnly = true,
            TextWrapping = TextWrapping.Wrap,
            MinHeight = 140,
        };

        BuildPageLayout();
    }

    private void BuildPageLayout()
    {
        Button openButton = new() { Content = "打开投影文件" };
        openButton.Click += OpenButton_Click;

        ListView statsListView = new()
        {
            ItemsSource = _stats,
            MinHeight = 260,
        };

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
                        Text = "MC 投影查看器",
                        FontSize = 30,
                        FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    },
                    new TextBlock
                    {
                        Text = "原生版提供文件解析、压缩识别与方块 ID 统计，侧重信息检视与快速核对。",
                        Opacity = 0.82,
                    },
                },
            },
        };

        Grid root = new()
        {
            Padding = new Thickness(20),
            RowSpacing = 12,
        };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(2, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        root.Children.Add(hero);
        Grid.SetRow(openButton, 1);
        root.Children.Add(openButton);

        StackPanel metaPanel = new() { Spacing = 6 };
        metaPanel.Children.Add(_fileMetaTextBlock);
        metaPanel.Children.Add(_summaryTextBlock);
        Grid.SetRow(metaPanel, 2);
        root.Children.Add(metaPanel);

        Grid.SetRow(statsListView, 3);
        root.Children.Add(statsListView);
        Grid.SetRow(_previewTextBox, 4);
        root.Children.Add(_previewTextBox);

        Content = root;
    }

    private async void OpenButton_Click(object sender, RoutedEventArgs e)
    {
        FileOpenPicker picker = new();
        IntPtr windowHandle = WindowNative.GetWindowHandle(App.MainWindow);
        InitializeWithWindow.Initialize(picker, windowHandle);
        picker.ViewMode = PickerViewMode.List;
        picker.FileTypeFilter.Add(".litematic");
        picker.FileTypeFilter.Add(".schematic");
        picker.FileTypeFilter.Add(".nbt");

        StorageFile? file = await picker.PickSingleFileAsync();
        if (file is null)
        {
            return;
        }

        byte[] bytes = await File.ReadAllBytesAsync(file.Path);
        byte[] payload = TryDecompress(bytes, out string compressionType);
        string plainText = Encoding.UTF8.GetString(payload);
        IReadOnlyList<BlockStatItem> stats = ParseBlockStats(plainText);

        string sha256;
        using (SHA256 sha = SHA256.Create())
        {
            sha256 = Convert.ToHexString(sha.ComputeHash(bytes));
        }

        _fileMetaTextBlock.Text =
            "文件: " + file.Name + Environment.NewLine +
            "大小: " + FormatBytes((ulong)bytes.Length) + Environment.NewLine +
            "压缩: " + compressionType + Environment.NewLine +
            "SHA-256: " + sha256;

        _stats.Clear();
        foreach (BlockStatItem item in stats)
        {
            _stats.Add(item);
        }

        _summaryTextBlock.Text = "识别到方块 ID " + stats.Count.ToString(CultureInfo.InvariantCulture) + " 种。";

        string preview = plainText.Length > 1200 ? plainText[..1200] + "..." : plainText;
        _previewTextBox.Text = preview;
    }

    private static byte[] TryDecompress(byte[] bytes, out string compressionType)
    {
        try
        {
            if (bytes.Length > 2 && bytes[0] == 0x1F && bytes[1] == 0x8B)
            {
                compressionType = "GZip";
                using MemoryStream source = new(bytes);
                using GZipStream gzip = new(source, CompressionMode.Decompress);
                using MemoryStream target = new();
                gzip.CopyTo(target);
                return target.ToArray();
            }

            if (bytes.Length > 2 && bytes[0] == 0x78)
            {
                compressionType = "ZLib";
                using MemoryStream source = new(bytes);
                using ZLibStream zlib = new(source, CompressionMode.Decompress);
                using MemoryStream target = new();
                zlib.CopyTo(target);
                return target.ToArray();
            }
        }
        catch
        {
            compressionType = "原始数据（解压失败，已回退）";
            return bytes;
        }

        compressionType = "未压缩/未知";
        return bytes;
    }

    private static IReadOnlyList<BlockStatItem> ParseBlockStats(string content)
    {
        MatchCollection matches = Regex.Matches(content, "minecraft:[a-z0-9_\\-/]+", RegexOptions.IgnoreCase);
        Dictionary<string, int> map = new(StringComparer.OrdinalIgnoreCase);

        foreach (Match match in matches)
        {
            string key = match.Value.ToLowerInvariant();
            if (!map.TryAdd(key, 1))
            {
                map[key]++;
            }
        }

        List<BlockStatItem> items = map
            .OrderByDescending(pair => pair.Value)
            .ThenBy(pair => pair.Key, StringComparer.Ordinal)
            .Take(150)
            .Select(pair => new BlockStatItem
            {
                Name = pair.Key,
                Count = pair.Value,
            })
            .ToList();

        if (items.Count == 0)
        {
            items.Add(new BlockStatItem
            {
                Name = "未在文本中检测到 minecraft:* 标识",
                Count = 0,
            });
        }

        return items;
    }

    private static string FormatBytes(ulong value)
    {
        if (value < 1024)
        {
            return value.ToString(CultureInfo.InvariantCulture) + " B";
        }

        double kb = value / 1024d;
        if (kb < 1024)
        {
            return kb.ToString("F2", CultureInfo.InvariantCulture) + " KB";
        }

        double mb = kb / 1024d;
        if (mb < 1024)
        {
            return mb.ToString("F2", CultureInfo.InvariantCulture) + " MB";
        }

        double gb = mb / 1024d;
        return gb.ToString("F2", CultureInfo.InvariantCulture) + " GB";
    }

    private sealed class BlockStatItem
    {
        public string Name { get; init; } = string.Empty;

        public int Count { get; init; }

        public override string ToString()
        {
            return Name + "  ->  " + Count.ToString(CultureInfo.InvariantCulture);
        }
    }
}

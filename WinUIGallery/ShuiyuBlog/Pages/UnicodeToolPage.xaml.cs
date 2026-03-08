// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace WinUIGallery.ShuiyuBlog.Pages;

public sealed partial class UnicodeToolPage : Page
{
    private readonly TextBox _encodeInputTextBox;
    private readonly TextBox _decodeInputTextBox;
    private readonly ComboBox _modeComboBox;
    private readonly TextBlock _feedbackTextBlock;
    private readonly ObservableCollection<UnicodeRow> _rows = [];

    public UnicodeToolPage()
    {
        _encodeInputTextBox = new TextBox
        {
            Header = "字符输入",
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            PlaceholderText = "输入任意文本进行 Unicode 编码",
            MinHeight = 120,
        };

        _decodeInputTextBox = new TextBox
        {
            Header = "Unicode 输入",
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            PlaceholderText = "支持 \\uXXXX、U+XXXX、&#xXXXX;、&#1234;",
            MinHeight = 120,
        };

        _modeComboBox = new ComboBox
        {
            Header = "编码输出格式",
            MinWidth = 220,
        };
        _modeComboBox.Items.Add("\\uXXXX (转义)");
        _modeComboBox.Items.Add("U+XXXX (码点)");
        _modeComboBox.Items.Add("&#xXXXX; (HTML 实体)");
        _modeComboBox.SelectedIndex = 1;

        _feedbackTextBlock = new TextBlock
        {
            Opacity = 0.8,
            Text = "可在编码和解码模式之间切换。",
            TextWrapping = TextWrapping.WrapWholeWords,
        };

        BuildPageLayout();
    }

    private void BuildPageLayout()
    {
        ListView outputListView = new()
        {
            ItemsSource = _rows,
            MinHeight = 260,
        };

        Button encodeButton = new() { Content = "字符 -> Unicode" };
        encodeButton.Click += EncodeButton_Click;
        Button decodeButton = new() { Content = "Unicode -> 字符" };
        decodeButton.Click += DecodeButton_Click;
        Button clearButton = new() { Content = "清空" };
        clearButton.Click += ClearButton_Click;

        StackPanel actions = new()
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
        };
        actions.Children.Add(encodeButton);
        actions.Children.Add(decodeButton);
        actions.Children.Add(clearButton);

        Grid inputGrid = new() { ColumnSpacing = 12 };
        inputGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        inputGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        inputGrid.Children.Add(_encodeInputTextBox);
        Grid.SetColumn(_decodeInputTextBox, 1);
        inputGrid.Children.Add(_decodeInputTextBox);

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
                        Text = "Unicode 转换",
                        FontSize = 30,
                        FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    },
                    new TextBlock
                    {
                        Text = "支持网页版中的转义/码点/实体三种格式，并提供结构化结果表。",
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
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        root.Children.Add(hero);
        Grid.SetRow(_modeComboBox, 1);
        root.Children.Add(_modeComboBox);
        Grid.SetRow(inputGrid, 2);
        root.Children.Add(inputGrid);
        Grid.SetRow(actions, 3);
        root.Children.Add(actions);
        Grid.SetRow(outputListView, 4);
        root.Children.Add(outputListView);
        Grid.SetRow(_feedbackTextBlock, 5);
        root.Children.Add(_feedbackTextBlock);

        Content = root;
    }

    private void EncodeButton_Click(object sender, RoutedEventArgs e)
    {
        string input = _encodeInputTextBox.Text ?? string.Empty;
        if (string.IsNullOrWhiteSpace(input))
        {
            _feedbackTextBlock.Text = "请先输入字符内容。";
            return;
        }

        _rows.Clear();
        int index = 1;
        foreach (Rune rune in input.EnumerateRunes())
        {
            string display = rune.Value switch
            {
                0x20 => "[空格]",
                0x09 => "[Tab]",
                0x0A => "[换行]",
                _ => rune.ToString(),
            };

            string encoded = _modeComboBox.SelectedIndex switch
            {
                0 => ToEscape(rune.Value),
                1 => "U+" + rune.Value.ToString("X4", CultureInfo.InvariantCulture),
                2 => "&#x" + rune.Value.ToString("X", CultureInfo.InvariantCulture) + ";",
                _ => "U+" + rune.Value.ToString("X4", CultureInfo.InvariantCulture),
            };

            _rows.Add(new UnicodeRow
            {
                Index = index.ToString(CultureInfo.InvariantCulture),
                DisplayChar = display,
                Encoded = encoded,
            });
            index++;
        }

        _feedbackTextBlock.Text = "编码完成，共 " + _rows.Count.ToString(CultureInfo.InvariantCulture) + " 个字符。";
    }

    private void DecodeButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            string raw = _decodeInputTextBox.Text ?? string.Empty;
            if (string.IsNullOrWhiteSpace(raw))
            {
                _feedbackTextBlock.Text = "请先输入 Unicode 值。";
                return;
            }

            string decoded = DecodeUnicode(raw);
            _rows.Clear();
            int index = 1;
            foreach (Rune rune in decoded.EnumerateRunes())
            {
                _rows.Add(new UnicodeRow
                {
                    Index = index.ToString(CultureInfo.InvariantCulture),
                    DisplayChar = rune.ToString(),
                    Encoded = "U+" + rune.Value.ToString("X4", CultureInfo.InvariantCulture),
                });
                index++;
            }

            _encodeInputTextBox.Text = decoded;
            _feedbackTextBlock.Text = "解码完成。";
        }
        catch (Exception ex)
        {
            _feedbackTextBlock.Text = "解码失败: " + ex.Message;
        }
    }

    private void ClearButton_Click(object sender, RoutedEventArgs e)
    {
        _encodeInputTextBox.Text = string.Empty;
        _decodeInputTextBox.Text = string.Empty;
        _rows.Clear();
        _feedbackTextBlock.Text = "已清空。";
    }

    private static string ToEscape(int codePoint)
    {
        if (codePoint <= 0xFFFF)
        {
            return "\\u" + codePoint.ToString("X4", CultureInfo.InvariantCulture);
        }

        int adjusted = codePoint - 0x10000;
        int high = 0xD800 + (adjusted >> 10);
        int low = 0xDC00 + (adjusted & 0x3FF);
        return "\\u" + high.ToString("X4", CultureInfo.InvariantCulture) + "\\u" + low.ToString("X4", CultureInfo.InvariantCulture);
    }

    private static string DecodeUnicode(string input)
    {
        string output = input;
        output = Regex.Replace(output, "\\\\u\\{([0-9A-Fa-f]{1,6})\\}", m => RuneFromHex(m.Groups[1].Value));
        output = Regex.Replace(output, "\\\\u([0-9A-Fa-f]{4})", m => ((char)Convert.ToInt32(m.Groups[1].Value, 16)).ToString());
        output = Regex.Replace(output, "U\\+([0-9A-Fa-f]{4,6})", m => RuneFromHex(m.Groups[1].Value));
        output = Regex.Replace(output, "&#x([0-9A-Fa-f]{1,6});?", m => RuneFromHex(m.Groups[1].Value));
        output = Regex.Replace(output, "&#([0-9]{1,7});?", m => RuneFromDec(m.Groups[1].Value));
        return output;
    }

    private static string RuneFromHex(string hex)
    {
        int value = Convert.ToInt32(hex, 16);
        return RuneFromInt(value);
    }

    private static string RuneFromDec(string dec)
    {
        int value = int.Parse(dec, CultureInfo.InvariantCulture);
        return RuneFromInt(value);
    }

    private static string RuneFromInt(int value)
    {
        if (value < 0 || value > 0x10FFFF)
        {
            throw new InvalidOperationException("包含超出 Unicode 范围的码点。");
        }

        return Rune.GetRuneAt(char.ConvertFromUtf32(value), 0).ToString();
    }

    private sealed class UnicodeRow
    {
        public string Index { get; init; } = string.Empty;

        public string DisplayChar { get; init; } = string.Empty;

        public string Encoded { get; init; } = string.Empty;

        public override string ToString()
        {
            return Index + ". " + DisplayChar + " -> " + Encoded;
        }
    }
}

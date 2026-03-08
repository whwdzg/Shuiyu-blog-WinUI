// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text;

namespace WinUIGallery.ShuiyuBlog.Pages;

public sealed partial class BaseXToolPage : Page
{
    private const string Base32Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
    private const string Base58Alphabet = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";
    private const string Base91Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!#$%&()*+,./:;<=?@[]^_`{|}~\"";

    private readonly TextBox _inputTextBox;
    private readonly TextBox _outputTextBox;
    private readonly ComboBox _modeComboBox;
    private readonly TextBlock _feedbackTextBlock;

    public BaseXToolPage()
    {
        _inputTextBox = new TextBox
        {
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            Header = "输入",
            MinHeight = 180,
        };

        _outputTextBox = new TextBox
        {
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            Header = "输出",
            MinHeight = 180,
        };

        _modeComboBox = new ComboBox
        {
            Header = "编码模式",
            HorizontalAlignment = HorizontalAlignment.Left,
            MinWidth = 220,
        };
        _modeComboBox.Items.Add("Base16");
        _modeComboBox.Items.Add("Base32");
        _modeComboBox.Items.Add("Base58");
        _modeComboBox.Items.Add("Base64");
        _modeComboBox.Items.Add("Base85");
        _modeComboBox.Items.Add("Base91");
        _modeComboBox.SelectedIndex = 3;

        _feedbackTextBlock = new TextBlock
        {
            Opacity = 0.8,
            Text = "支持 Base16/32/58/64/85/91。",
            TextWrapping = TextWrapping.WrapWholeWords,
        };

        BuildPageLayout();
    }

    private void BuildPageLayout()
    {
        Button encodeButton = new() { Content = "编码" };
        encodeButton.Click += EncodeButton_Click;
        Button decodeButton = new() { Content = "解码" };
        decodeButton.Click += DecodeButton_Click;
        Button swapButton = new() { Content = "交换" };
        swapButton.Click += SwapButton_Click;
        Button clearButton = new() { Content = "清空" };
        clearButton.Click += ClearButton_Click;

        StackPanel actions = new()
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
        };
        actions.Children.Add(encodeButton);
        actions.Children.Add(decodeButton);
        actions.Children.Add(swapButton);
        actions.Children.Add(clearButton);

        Grid editorsGrid = new()
        {
            ColumnSpacing = 12,
        };
        editorsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        editorsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        editorsGrid.Children.Add(_inputTextBox);
        Grid.SetColumn(_outputTextBox, 1);
        editorsGrid.Children.Add(_outputTextBox);

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
                        Text = "BaseX 编解码",
                        FontSize = 30,
                        FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    },
                    new TextBlock
                    {
                        Text = "与网页工具保持一致的多进制文本编解码，采用 WinUI 原生交互。",
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
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        root.Children.Add(hero);
        Grid.SetRow(_modeComboBox, 1);
        root.Children.Add(_modeComboBox);
        Grid.SetRow(actions, 2);
        root.Children.Add(actions);
        Grid.SetRow(editorsGrid, 3);
        root.Children.Add(editorsGrid);
        Grid.SetRow(_feedbackTextBlock, 4);
        root.Children.Add(_feedbackTextBlock);

        Content = root;
    }

    private void EncodeButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            string mode = GetMode();
            byte[] bytes = Encoding.UTF8.GetBytes(_inputTextBox.Text ?? string.Empty);
            _outputTextBox.Text = mode switch
            {
                "Base16" => EncodeBase16(bytes),
                "Base32" => EncodeBase32(bytes),
                "Base58" => EncodeBase58(bytes),
                "Base64" => Convert.ToBase64String(bytes),
                "Base85" => EncodeBase85(bytes),
                "Base91" => EncodeBase91(bytes),
                _ => Convert.ToBase64String(bytes),
            };
            _feedbackTextBlock.Text = "编码成功。";
        }
        catch (Exception ex)
        {
            _feedbackTextBlock.Text = "编码失败: " + ex.Message;
        }
    }

    private void DecodeButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            string mode = GetMode();
            string input = _inputTextBox.Text ?? string.Empty;
            byte[] bytes = mode switch
            {
                "Base16" => DecodeBase16(input),
                "Base32" => DecodeBase32(input),
                "Base58" => DecodeBase58(input),
                "Base64" => Convert.FromBase64String(RemoveWhitespace(input)),
                "Base85" => DecodeBase85(input),
                "Base91" => DecodeBase91(input),
                _ => Convert.FromBase64String(RemoveWhitespace(input)),
            };
            _outputTextBox.Text = Encoding.UTF8.GetString(bytes);
            _feedbackTextBlock.Text = "解码成功。";
        }
        catch (Exception ex)
        {
            _feedbackTextBlock.Text = "解码失败: " + ex.Message;
        }
    }

    private void SwapButton_Click(object sender, RoutedEventArgs e)
    {
        string temp = _inputTextBox.Text;
        _inputTextBox.Text = _outputTextBox.Text;
        _outputTextBox.Text = temp;
        _feedbackTextBlock.Text = "已交换输入和输出。";
    }

    private void ClearButton_Click(object sender, RoutedEventArgs e)
    {
        _inputTextBox.Text = string.Empty;
        _outputTextBox.Text = string.Empty;
        _feedbackTextBlock.Text = "已清空。";
    }

    private string GetMode()
    {
        return _modeComboBox.SelectedItem as string ?? "Base64";
    }

    private static string RemoveWhitespace(string value)
    {
        StringBuilder builder = new(value.Length);
        foreach (char c in value)
        {
            if (!char.IsWhiteSpace(c))
            {
                builder.Append(c);
            }
        }

        return builder.ToString();
    }

    private static string EncodeBase16(byte[] bytes)
    {
        StringBuilder builder = new(bytes.Length * 2);
        foreach (byte value in bytes)
        {
            builder.Append(value.ToString("x2", CultureInfo.InvariantCulture));
        }

        return builder.ToString();
    }

    private static byte[] DecodeBase16(string value)
    {
        string normalized = RemoveWhitespace(value);
        if (normalized.Length % 2 != 0)
        {
            throw new InvalidOperationException("Base16 长度必须为偶数。");
        }

        byte[] output = new byte[normalized.Length / 2];
        for (int i = 0; i < output.Length; i++)
        {
            output[i] = byte.Parse(normalized.Substring(i * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        }

        return output;
    }

    private static string EncodeBase32(byte[] bytes)
    {
        int buffer = 0;
        int bits = 0;
        StringBuilder output = new();

        foreach (byte value in bytes)
        {
            buffer = (buffer << 8) | value;
            bits += 8;
            while (bits >= 5)
            {
                int index = (buffer >> (bits - 5)) & 31;
                bits -= 5;
                output.Append(Base32Alphabet[index]);
            }
        }

        if (bits > 0)
        {
            int index = (buffer << (5 - bits)) & 31;
            output.Append(Base32Alphabet[index]);
        }

        while (output.Length % 8 != 0)
        {
            output.Append('=');
        }

        return output.ToString();
    }

    private static byte[] DecodeBase32(string value)
    {
        string normalized = RemoveWhitespace(value).TrimEnd('=').ToUpperInvariant();
        int buffer = 0;
        int bits = 0;
        List<byte> output = [];

        foreach (char c in normalized)
        {
            int index = Base32Alphabet.IndexOf(c);
            if (index < 0)
            {
                throw new InvalidOperationException("包含非法的 Base32 字符。");
            }

            buffer = (buffer << 5) | index;
            bits += 5;
            while (bits >= 8)
            {
                bits -= 8;
                output.Add((byte)((buffer >> bits) & 0xff));
            }
        }

        return output.ToArray();
    }

    private static string EncodeBase58(byte[] bytes)
    {
        if (bytes.Length == 0)
        {
            return string.Empty;
        }

        int zeros = bytes.TakeWhile(value => value == 0).Count();
        BigInteger number = new BigInteger(bytes.Reverse().Concat(new byte[] { 0 }).ToArray());

        StringBuilder builder = new();
        while (number > 0)
        {
            BigInteger remainder;
            number = BigInteger.DivRem(number, 58, out remainder);
            builder.Insert(0, Base58Alphabet[(int)remainder]);
        }

        for (int i = 0; i < zeros; i++)
        {
            builder.Insert(0, '1');
        }

        return builder.ToString();
    }

    private static byte[] DecodeBase58(string value)
    {
        string normalized = RemoveWhitespace(value);
        if (string.IsNullOrEmpty(normalized))
        {
            return [];
        }

        BigInteger number = BigInteger.Zero;
        foreach (char c in normalized)
        {
            int index = Base58Alphabet.IndexOf(c);
            if (index < 0)
            {
                throw new InvalidOperationException("包含非法的 Base58 字符。");
            }

            number = number * 58 + index;
        }

        List<byte> bytes = [];
        while (number > 0)
        {
            BigInteger remainder;
            number = BigInteger.DivRem(number, 256, out remainder);
            bytes.Add((byte)remainder);
        }

        bytes.Reverse();

        int leadingOnes = normalized.TakeWhile(c => c == '1').Count();
        byte[] output = new byte[leadingOnes + bytes.Count];
        for (int i = 0; i < bytes.Count; i++)
        {
            output[leadingOnes + i] = bytes[i];
        }

        return output;
    }

    private static string EncodeBase85(byte[] bytes)
    {
        if (bytes.Length == 0)
        {
            return string.Empty;
        }

        StringBuilder output = new();
        for (int i = 0; i < bytes.Length; i += 4)
        {
            int chunkLength = Math.Min(4, bytes.Length - i);
            uint value = 0;
            for (int j = 0; j < chunkLength; j++)
            {
                value = (value << 8) | bytes[i + j];
            }

            for (int j = chunkLength; j < 4; j++)
            {
                value <<= 8;
            }

            char[] encoded = new char[5];
            for (int j = 4; j >= 0; j--)
            {
                encoded[j] = (char)((value % 85) + 33);
                value /= 85;
            }

            int keep = chunkLength < 4 ? chunkLength + 1 : 5;
            output.Append(encoded, 0, keep);
        }

        return output.ToString();
    }

    private static byte[] DecodeBase85(string value)
    {
        string normalized = RemoveWhitespace(value);
        if (string.IsNullOrEmpty(normalized))
        {
            return [];
        }

        List<byte> output = [];
        List<int> group = [];

        foreach (char c in normalized)
        {
            int code = c - 33;
            if (code < 0 || code > 84)
            {
                throw new InvalidOperationException("包含非法的 Base85 字符。");
            }

            group.Add(code);
            if (group.Count == 5)
            {
                AppendBase85Group(group, output, 4);
                group.Clear();
            }
        }

        if (group.Count > 0)
        {
            int keep = group.Count - 1;
            while (group.Count < 5)
            {
                group.Add(84);
            }

            AppendBase85Group(group, output, Math.Max(0, keep));
        }

        return output.ToArray();
    }

    private static void AppendBase85Group(IReadOnlyList<int> group, ICollection<byte> output, int keepBytes)
    {
        uint value = 0;
        for (int i = 0; i < 5; i++)
        {
            value = (value * 85) + (uint)group[i];
        }

        byte[] bytes =
        [
            (byte)((value >> 24) & 0xff),
            (byte)((value >> 16) & 0xff),
            (byte)((value >> 8) & 0xff),
            (byte)(value & 0xff),
        ];

        for (int i = 0; i < keepBytes; i++)
        {
            output.Add(bytes[i]);
        }
    }

    private static string EncodeBase91(byte[] data)
    {
        int b = 0;
        int n = 0;
        StringBuilder output = new();

        foreach (byte value in data)
        {
            b |= value << n;
            n += 8;
            if (n > 13)
            {
                int v = b & 8191;
                if (v > 88)
                {
                    b >>= 13;
                    n -= 13;
                }
                else
                {
                    v = b & 16383;
                    b >>= 14;
                    n -= 14;
                }

                output.Append(Base91Alphabet[v % 91]);
                output.Append(Base91Alphabet[v / 91]);
            }
        }

        if (n != 0)
        {
            output.Append(Base91Alphabet[b % 91]);
            if (n > 7 || b > 90)
            {
                output.Append(Base91Alphabet[b / 91]);
            }
        }

        return output.ToString();
    }

    private static byte[] DecodeBase91(string value)
    {
        string normalized = RemoveWhitespace(value);
        if (string.IsNullOrEmpty(normalized))
        {
            return [];
        }

        Dictionary<char, int> map = [];
        for (int i = 0; i < Base91Alphabet.Length; i++)
        {
            map[Base91Alphabet[i]] = i;
        }

        int v = -1;
        int b = 0;
        int n = 0;
        List<byte> output = [];

        foreach (char c in normalized)
        {
            if (!map.TryGetValue(c, out int current))
            {
                throw new InvalidOperationException("包含非法的 Base91 字符。");
            }

            if (v < 0)
            {
                v = current;
            }
            else
            {
                v += current * 91;
                b |= v << n;
                n += (v & 8191) > 88 ? 13 : 14;
                while (n >= 8)
                {
                    output.Add((byte)(b & 255));
                    b >>= 8;
                    n -= 8;
                }

                v = -1;
            }
        }

        if (v > -1)
        {
            output.Add((byte)((b | (v << n)) & 255));
        }

        return output.ToArray();
    }
}

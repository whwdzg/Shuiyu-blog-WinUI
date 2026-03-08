// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Text;

namespace WinUIGallery.ShuiyuBlog.Pages;

public sealed partial class Base64ToolPage : Page
{
    private readonly TextBox _inputTextBox;
    private readonly TextBox _outputTextBox;

    public Base64ToolPage()
    {
        _inputTextBox = new TextBox
        {
            AcceptsReturn = true,
            Header = "输入",
            TextWrapping = TextWrapping.Wrap,
        };

        _outputTextBox = new TextBox
        {
            AcceptsReturn = true,
            Header = "输出",
            IsReadOnly = true,
            TextWrapping = TextWrapping.Wrap,
        };

        Grid editorsGrid = new()
        {
            ColumnSpacing = 12,
        };
        editorsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        editorsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        editorsGrid.Children.Add(_inputTextBox);
        Grid.SetColumn(_outputTextBox, 1);
        editorsGrid.Children.Add(_outputTextBox);

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

        Grid root = new()
        {
            Padding = new Thickness(20),
            RowSpacing = 12,
        };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        TextBlock header = new()
        {
            FontSize = 26,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Text = "Base64 工具",
        };

        root.Children.Add(header);
        Grid.SetRow(editorsGrid, 1);
        root.Children.Add(editorsGrid);
        Grid.SetRow(actions, 2);
        root.Children.Add(actions);

        Content = root;
    }

    private void EncodeButton_Click(object sender, RoutedEventArgs e)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(_inputTextBox.Text ?? string.Empty);
        _outputTextBox.Text = Convert.ToBase64String(bytes);
    }

    private void DecodeButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            byte[] bytes = Convert.FromBase64String(_inputTextBox.Text ?? string.Empty);
            _outputTextBox.Text = Encoding.UTF8.GetString(bytes);
        }
        catch (FormatException)
        {
            _outputTextBox.Text = "Base64 格式无效。";
        }
    }

    private void SwapButton_Click(object sender, RoutedEventArgs e)
    {
        string left = _inputTextBox.Text;
        _inputTextBox.Text = _outputTextBox.Text;
        _outputTextBox.Text = left;
    }

    private void ClearButton_Click(object sender, RoutedEventArgs e)
    {
        _inputTextBox.Text = string.Empty;
        _outputTextBox.Text = string.Empty;
    }
}

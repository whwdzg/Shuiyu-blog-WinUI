// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;

namespace WinUIGallery.ShuiyuBlog.Pages;

public sealed partial class BaseConvertToolPage : Page
{
    private readonly TextBox _inputTextBox;
    private readonly ComboBox _fromBaseComboBox;
    private readonly ComboBox _toBaseComboBox;
    private readonly TextBlock _resultTextBlock;

    public BaseConvertToolPage()
    {
        _inputTextBox = new TextBox
        {
            Header = "输入数值",
            PlaceholderText = "例如 FF 或 1010",
        };

        _fromBaseComboBox = new ComboBox { Header = "源进制" };
        _toBaseComboBox = new ComboBox { Header = "目标进制" };
        _resultTextBlock = new TextBlock
        {
            FontSize = 18,
            Text = "结果：",
            TextWrapping = TextWrapping.Wrap,
        };

        Grid baseGrid = new() { ColumnSpacing = 12 };
        baseGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        baseGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        baseGrid.Children.Add(_fromBaseComboBox);
        Grid.SetColumn(_toBaseComboBox, 1);
        baseGrid.Children.Add(_toBaseComboBox);

        Button convertButton = new()
        {
            Content = "转换",
            HorizontalAlignment = HorizontalAlignment.Left,
        };
        convertButton.Click += ConvertButton_Click;

        StackPanel resultPanel = new() { Spacing = 12 };
        resultPanel.Children.Add(convertButton);
        resultPanel.Children.Add(_resultTextBlock);

        Grid root = new()
        {
            Padding = new Thickness(20),
            RowSpacing = 12,
        };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        TextBlock header = new()
        {
            FontSize = 26,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Text = "进制转换工具",
        };

        root.Children.Add(header);
        Grid.SetRow(_inputTextBox, 1);
        root.Children.Add(_inputTextBox);
        Grid.SetRow(baseGrid, 2);
        root.Children.Add(baseGrid);
        Grid.SetRow(resultPanel, 3);
        root.Children.Add(resultPanel);

        Content = root;

        int[] bases = [2, 8, 10, 16, 32, 36];
        foreach (int currentBase in bases)
        {
            _fromBaseComboBox.Items.Add(currentBase);
            _toBaseComboBox.Items.Add(currentBase);
        }

        _fromBaseComboBox.SelectedItem = 10;
        _toBaseComboBox.SelectedItem = 16;
    }

    private void ConvertButton_Click(object sender, RoutedEventArgs e)
    {
        string input = (_inputTextBox.Text ?? string.Empty).Trim();
        int fromBase = (int)(_fromBaseComboBox.SelectedItem ?? 10);
        int toBase = (int)(_toBaseComboBox.SelectedItem ?? 10);

        if (string.IsNullOrEmpty(input))
        {
            _resultTextBlock.Text = "结果：请输入数值。";
            return;
        }

        try
        {
            long value = Convert.ToInt64(input, fromBase);
            string output = Convert.ToString(value, toBase) ?? string.Empty;
            _resultTextBlock.Text = $"结果：{output.ToUpperInvariant()}";
        }
        catch (Exception)
        {
            _resultTextBlock.Text = "结果：输入与源进制不匹配，或超出范围。";
        }
    }
}

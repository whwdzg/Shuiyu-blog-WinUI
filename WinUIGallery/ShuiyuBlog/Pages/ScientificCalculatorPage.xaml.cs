// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace WinUIGallery.ShuiyuBlog.Pages;

public sealed partial class ScientificCalculatorPage : Page
{
    private static readonly Regex SinRegex = new(@"sin\(([^)]+)\)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex CosRegex = new(@"cos\(([^)]+)\)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex TanRegex = new(@"tan\(([^)]+)\)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex SqrtRegex = new(@"sqrt\(([^)]+)\)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private readonly TextBox _expressionTextBox;
    private readonly TextBlock _resultTextBlock;

    public ScientificCalculatorPage()
    {
        _expressionTextBox = new TextBox
        {
            Header = "表达式",
            PlaceholderText = "示例: sin(0.5) + 2*3",
        };

        _resultTextBlock = new TextBlock
        {
            FontSize = 20,
            Text = "结果：",
            TextWrapping = TextWrapping.Wrap,
        };

        Button evaluateButton = new() { Content = "计算" };
        evaluateButton.Click += EvaluateButton_Click;
        Button clearButton = new() { Content = "清空" };
        clearButton.Click += ClearButton_Click;

        StackPanel buttonPanel = new()
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
        };
        buttonPanel.Children.Add(evaluateButton);
        buttonPanel.Children.Add(clearButton);

        Grid root = new()
        {
            Padding = new Thickness(20),
            RowSpacing = 10,
        };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        TextBlock header = new()
        {
            FontSize = 26,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Text = "科学计算器",
        };

        root.Children.Add(header);
        Grid.SetRow(_expressionTextBox, 1);
        root.Children.Add(_expressionTextBox);
        Grid.SetRow(buttonPanel, 2);
        root.Children.Add(buttonPanel);
        Grid.SetRow(_resultTextBlock, 3);
        root.Children.Add(_resultTextBlock);

        Content = root;
    }

    private void EvaluateButton_Click(object sender, RoutedEventArgs e)
    {
        string expression = _expressionTextBox.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(expression))
        {
            _resultTextBlock.Text = "结果：请输入表达式。";
            return;
        }

        try
        {
            string normalized = NormalizeFunctions(expression);
            double value = EvaluateArithmeticExpression(normalized);
            _resultTextBlock.Text = "结果：" + value.ToString(CultureInfo.InvariantCulture);
        }
        catch (Exception)
        {
            _resultTextBlock.Text = "结果：表达式无效。支持 + - * / () 与 sin/cos/tan/sqrt。";
        }
    }

    private static string NormalizeFunctions(string expression)
    {
        string output = expression;
        output = ReplaceFunction(output, SinRegex, Math.Sin);
        output = ReplaceFunction(output, CosRegex, Math.Cos);
        output = ReplaceFunction(output, TanRegex, Math.Tan);
        output = ReplaceFunction(output, SqrtRegex, Math.Sqrt);
        return output;
    }

    private static string ReplaceFunction(string input, Regex regex, Func<double, double> function)
    {
        while (regex.IsMatch(input))
        {
            input = regex.Replace(input, match =>
            {
                string raw = match.Groups[1].Value;
                double number = double.Parse(raw, CultureInfo.InvariantCulture);
                double result = function(number);
                return result.ToString(CultureInfo.InvariantCulture);
            });
        }

        return input;
    }

    private static double EvaluateArithmeticExpression(string expression)
    {
        Queue<string> output = [];
        Stack<char> operators = [];
        int index = 0;
        bool expectUnary = true;

        while (index < expression.Length)
        {
            char current = expression[index];
            if (char.IsWhiteSpace(current))
            {
                index++;
                continue;
            }

            if (char.IsDigit(current) || current == '.' || (expectUnary && (current == '+' || current == '-')))
            {
                int start = index;
                if (expectUnary && (current == '+' || current == '-'))
                {
                    index++;
                }

                while (index < expression.Length && (char.IsDigit(expression[index]) || expression[index] == '.'))
                {
                    index++;
                }

                output.Enqueue(expression[start..index]);
                expectUnary = false;
                continue;
            }

            if (current == '(')
            {
                operators.Push(current);
                index++;
                expectUnary = true;
                continue;
            }

            if (current == ')')
            {
                while (operators.Count > 0 && operators.Peek() != '(')
                {
                    output.Enqueue(operators.Pop().ToString());
                }

                if (operators.Count == 0 || operators.Pop() != '(')
                {
                    throw new InvalidOperationException("Mismatched parentheses.");
                }

                index++;
                expectUnary = false;
                continue;
            }

            if (IsOperator(current))
            {
                while (operators.Count > 0 && IsOperator(operators.Peek()) && Precedence(operators.Peek()) >= Precedence(current))
                {
                    output.Enqueue(operators.Pop().ToString());
                }

                operators.Push(current);
                index++;
                expectUnary = true;
                continue;
            }

            throw new InvalidOperationException("Invalid character.");
        }

        while (operators.Count > 0)
        {
            char op = operators.Pop();
            if (op == '(' || op == ')')
            {
                throw new InvalidOperationException("Mismatched parentheses.");
            }

            output.Enqueue(op.ToString());
        }

        Stack<double> values = [];
        foreach (string token in output)
        {
            if (double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out double number))
            {
                values.Push(number);
                continue;
            }

            if (token.Length != 1 || !IsOperator(token[0]) || values.Count < 2)
            {
                throw new InvalidOperationException("Invalid expression.");
            }

            double right = values.Pop();
            double left = values.Pop();
            values.Push(token[0] switch
            {
                '+' => left + right,
                '-' => left - right,
                '*' => left * right,
                '/' => right == 0 ? throw new DivideByZeroException() : left / right,
                _ => throw new InvalidOperationException("Unknown operator."),
            });
        }

        if (values.Count != 1)
        {
            throw new InvalidOperationException("Invalid expression.");
        }

        return values.Pop();
    }

    private static bool IsOperator(char value)
    {
        return value == '+' || value == '-' || value == '*' || value == '/';
    }

    private static int Precedence(char value)
    {
        return value is '*' or '/' ? 2 : 1;
    }

    private void ClearButton_Click(object sender, RoutedEventArgs e)
    {
        _expressionTextBox.Text = string.Empty;
        _resultTextBlock.Text = "结果：";
    }
}

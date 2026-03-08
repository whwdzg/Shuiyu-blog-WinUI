// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using WinUIGallery.ShuiyuBlog.Models;
using WinUIGallery.ShuiyuBlog.Services;

namespace WinUIGallery.ShuiyuBlog.Pages;

public sealed partial class ShuiyuBlogPage : Page
{
    private readonly SiteContentService _contentService = new();
    private readonly List<BlogEntry> _allEntries = [];
    private readonly ObservableCollection<BlogEntry> _visibleEntries = [];
    private readonly ListView _entryListView;
    private readonly ComboBox _sectionComboBox;
    private readonly TextBox _searchTextBox;
    private readonly TextBlock _detailTitleTextBlock;
    private readonly TextBlock _detailPathTextBlock;
    private readonly TextBlock _detailMetaTextBlock;
    private readonly TextBox _detailTextBox;
    private string _initialSection = "全部";

    public ShuiyuBlogPage()
    {
        _entryListView = new ListView
        {
            IsItemClickEnabled = true,
            SelectionMode = ListViewSelectionMode.Single,
            DisplayMemberPath = nameof(BlogEntry.Title),
            ItemsSource = _visibleEntries,
        };
        _entryListView.ItemClick += EntryListView_ItemClick;

        _sectionComboBox = new ComboBox { Header = "分区" };
        _sectionComboBox.SelectionChanged += SectionComboBox_SelectionChanged;

        _searchTextBox = new TextBox
        {
            Header = "搜索",
            PlaceholderText = "按标题、路径、摘要搜索",
        };
        _searchTextBox.TextChanged += SearchTextBox_TextChanged;

        _detailTitleTextBlock = new TextBlock
        {
            FontSize = 22,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Text = "请选择左侧内容",
            TextWrapping = TextWrapping.WrapWholeWords,
        };

        _detailPathTextBlock = new TextBlock { Opacity = 0.75, TextWrapping = TextWrapping.WrapWholeWords };
        _detailMetaTextBlock = new TextBlock { Opacity = 0.75, TextWrapping = TextWrapping.WrapWholeWords };
        _detailTextBox = new TextBox
        {
            AcceptsReturn = true,
            IsReadOnly = true,
            MinHeight = 400,
            TextWrapping = TextWrapping.Wrap,
        };

        BuildPageLayout();
        Loaded += ShuiyuBlogPage_Loaded;
    }

    private void BuildPageLayout()
    {
        Grid root = new()
        {
            Padding = new Thickness(20),
            RowSpacing = 12,
        };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        StackPanel header = new() { Spacing = 4 };
        header.Children.Add(new TextBlock
        {
            FontSize = 28,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Text = "水鱼 Blog",
        });
        header.Children.Add(new TextBlock { Opacity = 0.8, Text = "Shuiyu Blog · WinUI 3 Native Archive" });

        Grid filterGrid = new() { ColumnSpacing = 12 };
        filterGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(220) });
        filterGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        filterGrid.Children.Add(_sectionComboBox);
        Grid.SetColumn(_searchTextBox, 1);
        filterGrid.Children.Add(_searchTextBox);

        Grid contentGrid = new() { ColumnSpacing = 12 };
        contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
        contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(3, GridUnitType.Star) });
        contentGrid.Children.Add(_entryListView);

        Border detailBorder = new()
        {
            Padding = new Thickness(12),
            CornerRadius = new CornerRadius(8),
        };

        StackPanel detailPanel = new() { Spacing = 8 };
        detailPanel.Children.Add(_detailTitleTextBlock);
        detailPanel.Children.Add(_detailPathTextBlock);
        detailPanel.Children.Add(_detailMetaTextBlock);
        detailPanel.Children.Add(_detailTextBox);
        detailBorder.Child = new ScrollViewer { Content = detailPanel };

        Grid.SetColumn(detailBorder, 1);
        contentGrid.Children.Add(detailBorder);

        root.Children.Add(header);
        Grid.SetRow(filterGrid, 1);
        root.Children.Add(filterGrid);
        Grid.SetRow(contentGrid, 2);
        root.Children.Add(contentGrid);
        Content = root;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is string initialSection && !string.IsNullOrWhiteSpace(initialSection))
        {
            _initialSection = initialSection;
        }
    }

    private async void ShuiyuBlogPage_Loaded(object sender, RoutedEventArgs e)
    {
        IReadOnlyList<BlogEntry> entries = await _contentService.LoadEntriesAsync();
        _allEntries.Clear();
        _allEntries.AddRange(entries);

        List<string> sections = _allEntries
            .Select(entry => entry.Section)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(section => section)
            .ToList();

        _sectionComboBox.Items.Clear();
        _sectionComboBox.Items.Add("全部");
        foreach (string section in sections)
        {
            _sectionComboBox.Items.Add(section);
        }

        int selectedIndex = 0;
        if (!string.IsNullOrWhiteSpace(_initialSection))
        {
            int existingIndex = _sectionComboBox.Items.IndexOf(_initialSection);
            if (existingIndex >= 0)
            {
                selectedIndex = existingIndex;
            }
        }

        _sectionComboBox.SelectedIndex = selectedIndex;
        ApplyFilter();
    }

    private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        ApplyFilter();
    }

    private void SectionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ApplyFilter();
    }

    private void EntryListView_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is not BlogEntry entry)
        {
            return;
        }

        Frame.Navigate(typeof(NativeDocumentPage), entry);
    }

    private void ApplyFilter()
    {
        string query = _searchTextBox.Text?.Trim() ?? string.Empty;
        string selectedSection = _sectionComboBox.SelectedItem as string ?? "全部";

        IEnumerable<BlogEntry> filtered = _allEntries;

        if (!string.Equals(selectedSection, "全部", StringComparison.Ordinal))
        {
            filtered = filtered.Where(entry => string.Equals(entry.Section, selectedSection, StringComparison.Ordinal));
        }

        if (!string.IsNullOrWhiteSpace(query))
        {
            filtered = filtered.Where(entry =>
                entry.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                entry.RelativePath.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                entry.Summary.Contains(query, StringComparison.OrdinalIgnoreCase));
        }

        _visibleEntries.Clear();
        foreach (BlogEntry entry in filtered)
        {
            _visibleEntries.Add(entry);
        }

        if (_visibleEntries.Count == 0)
        {
            _detailTitleTextBlock.Text = "没有匹配结果";
            _detailPathTextBlock.Text = string.Empty;
            _detailMetaTextBlock.Text = string.Empty;
            _detailTextBox.Text = "请调整搜索关键词或分区筛选条件。";
            return;
        }

        BlogEntry first = _visibleEntries[0];
        _detailTitleTextBlock.Text = first.Title;
        _detailPathTextBlock.Text = first.RelativePath;
        _detailMetaTextBlock.Text = $"分区: {first.Section}  |  类型: {first.FileType}";
        _detailTextBox.Text = first.PreviewText;
    }
}

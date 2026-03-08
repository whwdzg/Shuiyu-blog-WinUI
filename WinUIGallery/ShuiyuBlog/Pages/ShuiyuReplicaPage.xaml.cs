// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using WinUIGallery.ShuiyuBlog.Models;
using Windows.Media.Core;
using Windows.System;

namespace WinUIGallery.ShuiyuBlog.Pages;

public sealed partial class ShuiyuReplicaPage : Page
{
    private static readonly Regex TitleRegex = new("<title>(.*?)</title>", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
    private static readonly Regex SectionRegex = new("<section[^>]*>([\\s\\S]*?)</section>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex HeadingRegex = new("<(h1|h2|h3)[^>]*>([\\s\\S]*?)</\\1>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex ParagraphRegex = new("<p[^>]*>([\\s\\S]*?)</p>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex ListRegex = new("<li[^>]*>([\\s\\S]*?)</li>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex LinkRegex = new("<a[^>]*href=[\"']([^\"']+)[\"'][^>]*>([\\s\\S]*?)</a>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex ImageRegex = new("<img[^>]*src=[\"']([^\"']+)[\"'][^>]*>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex VideoRegex = new("<video[^>]*>([\\s\\S]*?)</video>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex SourceRegex = new("<source[^>]*src=[\"']([^\"']+)[\"'][^>]*>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex StripTagRegex = new("<[^>]+>", RegexOptions.Compiled);
    private static readonly Regex WhiteSpaceRegex = new("\\s+", RegexOptions.Compiled);

    private readonly StackPanel _contentHost;
    private string _relativePath = "index.html";

    public ShuiyuReplicaPage()
    {
        _contentHost = new StackPanel { Spacing = 14 };
        Content = new ScrollViewer
        {
            Padding = new Thickness(20),
            Content = _contentHost,
        };
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is BlogEntry entry && !string.IsNullOrWhiteSpace(entry.RelativePath))
        {
            _relativePath = entry.RelativePath;
        }
        else if (e.Parameter is string relativePath && !string.IsNullOrWhiteSpace(relativePath))
        {
            _relativePath = relativePath;
        }

        BuildFromPage(_relativePath.Replace('\\\\', '/').TrimStart('/'));
    }

    private void BuildFromPage(string relativePath)
    {
        _contentHost.Children.Clear();

        string fullPath = Path.Combine(AppContext.BaseDirectory, "SiteContent", relativePath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(fullPath))
        {
            _contentHost.Children.Add(CreateHero("页面不存在", relativePath));
            _contentHost.Children.Add(CreateTextCard("未找到对应页面文件。"));
            return;
        }

        string html = File.ReadAllText(fullPath);
        string title = ResolveTitle(relativePath, html);
        _contentHost.Children.Add(CreateHero(title, relativePath));

        MatchCollection sections = SectionRegex.Matches(html);
        if (sections.Count == 0)
        {
            _contentHost.Children.Add(CreateStructuredCard(html, fullPath));
            return;
        }

        foreach (Match section in sections)
        {
            _contentHost.Children.Add(CreateStructuredCard(section.Groups[1].Value, fullPath));
        }
    }

    private Border CreateHero(string title, string path)
    {
        StackPanel panel = new() { Spacing = 6 };
        panel.Children.Add(new TextBlock { Text = path, Opacity = 0.72 });
        panel.Children.Add(new TextBlock
        {
            Text = title,
            FontSize = 34,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            TextWrapping = TextWrapping.WrapWholeWords,
        });
        panel.Children.Add(new TextBlock
        {
            Text = "原生页面复刻视图",
            Opacity = 0.82,
        });

        return new Border
        {
            Padding = new Thickness(18),
            CornerRadius = new CornerRadius(12),
            Background = Application.Current.Resources["CardBackgroundFillColorSecondaryBrush"] as Brush,
            Child = panel,
        };
    }

    private Border CreateStructuredCard(string htmlSegment, string pageFullPath)
    {
        StackPanel panel = new() { Spacing = 8 };

        foreach (Match heading in HeadingRegex.Matches(htmlSegment))
        {
            string level = heading.Groups[1].Value.ToLowerInvariant();
            string text = HtmlToText(heading.Groups[2].Value);
            if (string.IsNullOrWhiteSpace(text))
            {
                continue;
            }

            panel.Children.Add(new TextBlock
            {
                Text = text,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                FontSize = level switch
                {
                    "h1" => 30,
                    "h2" => 24,
                    _ => 20,
                },
                TextWrapping = TextWrapping.WrapWholeWords,
            });
        }

        foreach (Match paragraph in ParagraphRegex.Matches(htmlSegment))
        {
            string text = HtmlToText(paragraph.Groups[1].Value);
            if (!string.IsNullOrWhiteSpace(text))
            {
                panel.Children.Add(new TextBlock
                {
                    Text = text,
                    Opacity = 0.9,
                    TextWrapping = TextWrapping.WrapWholeWords,
                });
            }
        }

        List<string> listItems = ListRegex.Matches(htmlSegment).Select(m => HtmlToText(m.Groups[1].Value)).Where(t => !string.IsNullOrWhiteSpace(t)).ToList();
        foreach (string item in listItems)
        {
            panel.Children.Add(new TextBlock
            {
                Text = "• " + item,
                TextWrapping = TextWrapping.WrapWholeWords,
            });
        }

        foreach (Match image in ImageRegex.Matches(htmlSegment))
        {
            string imagePath = ResolveAssetPath(image.Groups[1].Value, pageFullPath);
            if (string.IsNullOrWhiteSpace(imagePath))
            {
                continue;
            }

            panel.Children.Add(new Image
            {
                Source = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(new Uri(imagePath)),
                Stretch = Stretch.Uniform,
                MaxHeight = 420,
            });
        }

        foreach (Match video in VideoRegex.Matches(htmlSegment))
        {
            Match sourceMatch = SourceRegex.Match(video.Groups[1].Value);
            if (!sourceMatch.Success)
            {
                continue;
            }

            string source = ResolveAssetPath(sourceMatch.Groups[1].Value, pageFullPath);
            if (string.IsNullOrWhiteSpace(source))
            {
                continue;
            }

            if (Uri.TryCreate(source, UriKind.Absolute, out Uri? videoUri))
            {
                MediaPlayerElement player = new()
                {
                    AreTransportControlsEnabled = true,
                    Source = MediaSource.CreateFromUri(videoUri),
                    AutoPlay = false,
                    MinHeight = 240,
                    MaxHeight = 420,
                };
                panel.Children.Add(player);
            }
        }

        List<Button> links = [];
        foreach (Match link in LinkRegex.Matches(htmlSegment))
        {
            string href = link.Groups[1].Value.Trim();
            string caption = HtmlToText(link.Groups[2].Value);
            if (string.IsNullOrWhiteSpace(href))
            {
                continue;
            }

            Button btn = new()
            {
                Content = string.IsNullOrWhiteSpace(caption) ? href : caption,
                Tag = href,
                HorizontalAlignment = HorizontalAlignment.Left,
            };
            btn.Click += LinkButton_Click;
            links.Add(btn);
        }

        if (links.Count > 0)
        {
            StackPanel linkPanel = new() { Orientation = Orientation.Horizontal, Spacing = 8 };
            foreach (Button button in links.Take(8))
            {
                linkPanel.Children.Add(button);
            }
            panel.Children.Add(linkPanel);
        }

        if (panel.Children.Count == 0)
        {
            string fallback = HtmlToText(htmlSegment);
            if (!string.IsNullOrWhiteSpace(fallback))
            {
                panel.Children.Add(new TextBlock { Text = fallback, TextWrapping = TextWrapping.WrapWholeWords });
            }
        }

        return new Border
        {
            Padding = new Thickness(12),
            CornerRadius = new CornerRadius(10),
            Background = Application.Current.Resources["CardBackgroundFillColorDefaultBrush"] as Brush,
            Child = panel,
        };
    }

    private Border CreateTextCard(string text)
    {
        return new Border
        {
            Padding = new Thickness(12),
            CornerRadius = new CornerRadius(10),
            Background = Application.Current.Resources["CardBackgroundFillColorDefaultBrush"] as Brush,
            Child = new TextBlock
            {
                Text = text,
                TextWrapping = TextWrapping.WrapWholeWords,
            },
        };
    }

    private async void LinkButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string href })
        {
            return;
        }

        if (Uri.TryCreate(href, UriKind.Absolute, out Uri? absoluteUri))
        {
            _ = await Launcher.LaunchUriAsync(absoluteUri);
            return;
        }

        string tag = href switch
        {
            "/" => "home:index",
            "/tool/base64.html" => "tool:base64",
            "/tool/basex.html" => "tool:basex",
            "/tool/unicode.html" => "tool:unicode",
            "/tool/base-convert.html" => "tool:baseconvert",
            "/tool/scientific-calculator.html" => "tool:calculator",
            "/tool/mc-projection.html" => "tool:mcprojection",
            "/media/music.html" => "media:music",
            "/media/video.html" => "media:video",
            _ => "doc:" + href.TrimStart('/'),
        };

        App.MainWindow.NavigateByTag(tag);
    }

    private static string ResolveTitle(string relativePath, string html)
    {
        Match match = TitleRegex.Match(html);
        if (match.Success)
        {
            return HtmlToText(match.Groups[1].Value);
        }

        return Path.GetFileNameWithoutExtension(relativePath);
    }

    private static string HtmlToText(string value)
    {
        string decoded = WebUtility.HtmlDecode(StripTagRegex.Replace(value, " "));
        return WhiteSpaceRegex.Replace(decoded, " ").Trim();
    }

    private static string ResolveAssetPath(string source, string currentPagePath)
    {
        string value = source.Trim();
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        if (value.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || value.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return value;
        }

        if (value.StartsWith("/", StringComparison.Ordinal))
        {
            return "ms-appx:///SiteContent" + value;
        }

        string siteRoot = Path.Combine(AppContext.BaseDirectory, "SiteContent");
        string currentFolder = Path.GetDirectoryName(currentPagePath) ?? siteRoot;
        string fullPath = Path.GetFullPath(Path.Combine(currentFolder, value.Replace('/', Path.DirectorySeparatorChar)));
        string relative = Path.GetRelativePath(siteRoot, fullPath).Replace('\\', '/');
        return "ms-appx:///SiteContent/" + relative;
    }
}

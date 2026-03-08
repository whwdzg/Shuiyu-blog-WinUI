// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using Windows.System;

namespace WinUIGallery.ShuiyuBlog.Pages;

public abstract class NativeHtmlPageBase : Page
{
    private static readonly Regex TitleRegex = new("<title>(.*?)</title>", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
    private static readonly Regex SectionRegex = new("<section[^>]*>([\\s\\S]*?)</section>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex HeadingRegex = new("<(h1|h2|h3)[^>]*>([\\s\\S]*?)</\\1>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex ParagraphRegex = new("<p[^>]*>([\\s\\S]*?)</p>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex ListRegex = new("<li[^>]*>([\\s\\S]*?)</li>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex LinkRegex = new("<a[^>]*href=[\"']([^\"']+)[\"'][^>]*>([\\s\\S]*?)</a>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex ImageRegex = new("<img[^>]*src=[\"']([^\"']+)[\"'][^>]*>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex WhiteSpaceRegex = new("\\s+", RegexOptions.Compiled);
    private static readonly Regex StripTagRegex = new("<[^>]+>", RegexOptions.Compiled);

    protected enum RenderMode
    {
        Generic,
        Comments,
        Cards,
    }

    private readonly StackPanel contentHost = new() { Spacing = 14 };

    protected NativeHtmlPageBase()
    {
        Content = new ScrollViewer
        {
            Padding = new Thickness(20),
            Content = contentHost,
        };
    }

    protected void RenderPage(string relativePath, RenderMode mode)
    {
        contentHost.Children.Clear();

        string fullPath = Path.Combine(AppContext.BaseDirectory, "SiteContent", relativePath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(fullPath))
        {
            contentHost.Children.Add(CreateHero("页面不存在", relativePath));
            contentHost.Children.Add(CreateTextCard("未找到对应页面文件。"));
            return;
        }

        string html = File.ReadAllText(fullPath);
        contentHost.Children.Add(CreateHero(ResolveTitle(relativePath, html), relativePath));

        switch (mode)
        {
            case RenderMode.Comments:
                RenderComments(html, fullPath);
                break;
            case RenderMode.Cards:
                RenderCards(html, fullPath);
                break;
            default:
                RenderGeneric(html, fullPath);
                break;
        }
    }

    private void RenderGeneric(string html, string pagePath)
    {
        MatchCollection sections = SectionRegex.Matches(html);
        if (sections.Count == 0)
        {
            contentHost.Children.Add(CreateStructuredCard(html, pagePath));
            return;
        }

        foreach (Match section in sections)
        {
            contentHost.Children.Add(CreateStructuredCard(section.Groups[1].Value, pagePath));
        }
    }

    private void RenderComments(string html, string pagePath)
    {
        RenderGeneric(html, pagePath);

        MatchCollection commentCards = Regex.Matches(html, "<div class=\"comment-card\">([\\s\\S]*?)</div>", RegexOptions.IgnoreCase);
        if (commentCards.Count == 0)
        {
            return;
        }

        StackPanel list = new() { Spacing = 10 };
        list.Children.Add(new TextBlock
        {
            Text = "留言摘录",
            FontSize = 24,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
        });

        foreach (Match card in commentCards)
        {
            string author = HtmlToText(Regex.Match(card.Value, "<p class=\"comment-author\">([\\s\\S]*?)</p>", RegexOptions.IgnoreCase).Groups[1].Value);
            string date = HtmlToText(Regex.Match(card.Value, "<p class=\"comment-date\">([\\s\\S]*?)</p>", RegexOptions.IgnoreCase).Groups[1].Value);
            string text = HtmlToText(Regex.Match(card.Value, "<p class=\"comment-text\">([\\s\\S]*?)</p>", RegexOptions.IgnoreCase).Groups[1].Value);

            StackPanel panel = new() { Spacing = 4 };
            panel.Children.Add(new TextBlock { Text = author + "  " + date, Opacity = 0.76 });
            panel.Children.Add(new TextBlock { Text = text, TextWrapping = TextWrapping.WrapWholeWords });

            list.Children.Add(new Border
            {
                Padding = new Thickness(10),
                CornerRadius = new CornerRadius(8),
                Background = Application.Current.Resources["CardBackgroundFillColorDefaultBrush"] as Brush,
                Child = panel,
            });
        }

        contentHost.Children.Add(list);
    }

    private void RenderCards(string html, string pagePath)
    {
        RenderGeneric(html, pagePath);

        MatchCollection cards = Regex.Matches(html, "<(article|div) class=\"(video-card|project-card)\"[^>]*>([\\s\\S]*?)</\\1>", RegexOptions.IgnoreCase);
        if (cards.Count == 0)
        {
            return;
        }

        StackPanel cardsHost = new() { Spacing = 10 };
        cardsHost.Children.Add(new TextBlock
        {
            Text = "卡片列表",
            FontSize = 24,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
        });

        foreach (Match card in cards)
        {
            string cardHtml = card.Groups[3].Value;
            string title = HtmlToText(Regex.Match(cardHtml, "<h3[^>]*>([\\s\\S]*?)</h3>", RegexOptions.IgnoreCase).Groups[1].Value);
            string description = HtmlToText(Regex.Match(cardHtml, "<p class=\"(video-desc|project-desc)\"[^>]*>([\\s\\S]*?)</p>", RegexOptions.IgnoreCase).Groups[2].Value);

            StackPanel panel = new() { Spacing = 6 };
            panel.Children.Add(new TextBlock
            {
                Text = title,
                FontSize = 20,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                TextWrapping = TextWrapping.WrapWholeWords,
            });
            if (!string.IsNullOrWhiteSpace(description))
            {
                panel.Children.Add(new TextBlock { Text = description, Opacity = 0.85, TextWrapping = TextWrapping.WrapWholeWords });
            }

            Match imgMatch = ImageRegex.Match(cardHtml);
            if (imgMatch.Success)
            {
                string src = ResolveAssetPath(imgMatch.Groups[1].Value, pagePath);
                if (Uri.TryCreate(src, UriKind.Absolute, out Uri? uri))
                {
                    panel.Children.Add(new Image
                    {
                        Source = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(uri),
                        Stretch = Stretch.Uniform,
                        MaxHeight = 300,
                    });
                }
            }

            StackPanel actionLine = new() { Orientation = Orientation.Horizontal, Spacing = 8 };
            foreach (Match link in LinkRegex.Matches(cardHtml).Cast<Match>().Take(5))
            {
                string href = link.Groups[1].Value.Trim();
                string text = HtmlToText(link.Groups[2].Value);
                if (string.IsNullOrWhiteSpace(href))
                {
                    continue;
                }

                Button button = new()
                {
                    Content = string.IsNullOrWhiteSpace(text) ? href : text,
                    Tag = href,
                };
                button.Click += LinkButton_Click;
                actionLine.Children.Add(button);
            }
            panel.Children.Add(actionLine);

            cardsHost.Children.Add(new Border
            {
                Padding = new Thickness(12),
                CornerRadius = new CornerRadius(10),
                Background = Application.Current.Resources["CardBackgroundFillColorDefaultBrush"] as Brush,
                Child = panel,
            });
        }

        contentHost.Children.Add(cardsHost);
    }

    private Border CreateHero(string title, string path)
    {
        return new Border
        {
            Padding = new Thickness(18),
            CornerRadius = new CornerRadius(12),
            Background = Application.Current.Resources["CardBackgroundFillColorSecondaryBrush"] as Brush,
            Child = new StackPanel
            {
                Spacing = 6,
                Children =
                {
                    new TextBlock { Text = path, Opacity = 0.72 },
                    new TextBlock
                    {
                        Text = title,
                        FontSize = 34,
                        FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                        TextWrapping = TextWrapping.WrapWholeWords,
                    },
                },
            },
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
                panel.Children.Add(new TextBlock { Text = text, Opacity = 0.9, TextWrapping = TextWrapping.WrapWholeWords });
            }
        }

        foreach (string listItem in ListRegex.Matches(htmlSegment).Cast<Match>().Select(m => HtmlToText(m.Groups[1].Value)).Where(t => !string.IsNullOrWhiteSpace(t)))
        {
            panel.Children.Add(new TextBlock { Text = "• " + listItem, TextWrapping = TextWrapping.WrapWholeWords });
        }

        foreach (Match image in ImageRegex.Matches(htmlSegment))
        {
            string src = ResolveAssetPath(image.Groups[1].Value, pageFullPath);
            if (Uri.TryCreate(src, UriKind.Absolute, out Uri? imageUri))
            {
                panel.Children.Add(new Image
                {
                    Source = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(imageUri),
                    Stretch = Stretch.Uniform,
                    MaxHeight = 420,
                });
            }
        }

        MatchCollection linkMatches = LinkRegex.Matches(htmlSegment);
        if (linkMatches.Count > 0)
        {
            StackPanel links = new() { Orientation = Orientation.Horizontal, Spacing = 8 };
            foreach (Match link in linkMatches.Cast<Match>().Take(6))
            {
                string href = link.Groups[1].Value.Trim();
                string text = HtmlToText(link.Groups[2].Value);
                if (string.IsNullOrWhiteSpace(href))
                {
                    continue;
                }

                Button button = new()
                {
                    Content = string.IsNullOrWhiteSpace(text) ? href : text,
                    Tag = href,
                };
                button.Click += LinkButton_Click;
                links.Children.Add(button);
            }
            panel.Children.Add(links);
        }

        if (panel.Children.Count == 0)
        {
            panel.Children.Add(new TextBlock { Text = HtmlToText(htmlSegment), TextWrapping = TextWrapping.WrapWholeWords });
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
            Child = new TextBlock { Text = text, TextWrapping = TextWrapping.WrapWholeWords },
        };
    }

    private async void LinkButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string href })
        {
            return;
        }

        if (Uri.TryCreate(href, UriKind.Absolute, out Uri? uri))
        {
            _ = await Launcher.LaunchUriAsync(uri);
            return;
        }

        string normalized = href.TrimStart('/');
        string tag = normalized switch
        {
            "" => "home:index",
            "tool/base64.html" => "tool:base64",
            "tool/basex.html" => "tool:basex",
            "tool/unicode.html" => "tool:unicode",
            "tool/base-convert.html" => "tool:baseconvert",
            "tool/scientific-calculator.html" => "tool:calculator",
            "tool/mc-projection.html" => "tool:mcprojection",
            "media/music.html" => "media:music",
            "media/video.html" => "media:video",
            _ => "doc:" + normalized,
        };

        App.MainWindow.NavigateByTag(tag);
    }

    private static string ResolveTitle(string relativePath, string html)
    {
        Match m = TitleRegex.Match(html);
        if (m.Success)
        {
            return HtmlToText(m.Groups[1].Value);
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

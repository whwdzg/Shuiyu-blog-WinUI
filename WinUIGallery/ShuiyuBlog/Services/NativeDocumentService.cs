// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WinUIGallery.ShuiyuBlog.Models;

namespace WinUIGallery.ShuiyuBlog.Services;

public sealed class NativeDocumentService
{
    private const int MaxPlainTextLength = 120000;
    private const int MaxImageCount = 120;
    private const int MaxLinkCount = 200;

    private static readonly Regex TitleRegex = new("<title>(.*?)</title>", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
    private static readonly Regex StripScriptRegex = new("<script[\\s\\S]*?</script>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex StripStyleRegex = new("<style[\\s\\S]*?</style>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex StripTagRegex = new("<[^>]+>", RegexOptions.Compiled);
    private static readonly Regex ImageRegex = new("<img[^>]*src=[\"']([^\"']+)[\"'][^>]*>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex AltRegex = new("alt=[\"']([^\"']*)[\"']", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex LinkRegex = new("<a[^>]*href=[\"']([^\"']+)[\"'][^>]*>([\\s\\S]*?)</a>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex WhiteSpaceRegex = new("\\s+", RegexOptions.Compiled);

    public async Task<NativeDocumentDetail?> LoadAsync(BlogEntry entry)
    {
        string siteRoot = Path.Combine(AppContext.BaseDirectory, "SiteContent");
        string fullPath = Path.Combine(siteRoot, entry.RelativePath.Replace('/', Path.DirectorySeparatorChar));

        if (!File.Exists(fullPath))
        {
            return null;
        }

        string rawContent = await File.ReadAllTextAsync(fullPath).ConfigureAwait(false);

        return await Task.Run(() =>
        {
            string plainText = TrimForDisplay(ToPlainText(entry.FileType, rawContent));
            List<NativeDocumentImageItem> images = ExtractImages(rawContent, siteRoot, Path.GetDirectoryName(fullPath) ?? siteRoot);
            List<NativeDocumentLinkItem> links = ExtractLinks(rawContent);

            string title = entry.Title;
            Match titleMatch = TitleRegex.Match(rawContent);
            if (titleMatch.Success)
            {
                title = WhiteSpaceRegex.Replace(WebUtility.HtmlDecode(titleMatch.Groups[1].Value).Trim(), " ");
            }

            return new NativeDocumentDetail
            {
                Title = title,
                RelativePath = entry.RelativePath,
                Section = entry.Section,
                FileType = entry.FileType,
                PlainText = plainText,
                Images = images,
                Links = links,
            };
        }).ConfigureAwait(false);
    }

    private static string TrimForDisplay(string plainText)
    {
        if (plainText.Length <= MaxPlainTextLength)
        {
            return plainText;
        }

        return plainText[..MaxPlainTextLength] + "\n\n[内容较长，已截断显示以保证页面流畅。]";
    }

    private static string ToPlainText(string fileType, string raw)
    {
        if (!string.Equals(fileType, "HTML", StringComparison.OrdinalIgnoreCase))
        {
            return raw;
        }

        string removedScript = StripScriptRegex.Replace(raw, " ");
        string removedStyle = StripStyleRegex.Replace(removedScript, " ");
        string withoutTags = StripTagRegex.Replace(removedStyle, " ");
        return WhiteSpaceRegex.Replace(WebUtility.HtmlDecode(withoutTags).Trim(), " ");
    }

    private static List<NativeDocumentImageItem> ExtractImages(string rawContent, string siteRoot, string baseDirectory)
    {
        List<NativeDocumentImageItem> items = [];

        MatchCollection matches = ImageRegex.Matches(rawContent);
        foreach (Match match in matches)
        {
            string source = match.Groups[1].Value.Trim();
            string resolved = ResolveLocalPath(source, siteRoot, baseDirectory);
            if (string.IsNullOrWhiteSpace(resolved) || !File.Exists(resolved))
            {
                continue;
            }

            string displayName = Path.GetFileName(resolved);
            Match altMatch = AltRegex.Match(match.Value);
            if (altMatch.Success && !string.IsNullOrWhiteSpace(altMatch.Groups[1].Value))
            {
                displayName = WebUtility.HtmlDecode(altMatch.Groups[1].Value.Trim());
            }

            items.Add(new NativeDocumentImageItem
            {
                DisplayName = displayName,
                SourcePath = resolved,
            });

            if (items.Count >= MaxImageCount)
            {
                break;
            }
        }

        return items;
    }

    private static List<NativeDocumentLinkItem> ExtractLinks(string rawContent)
    {
        List<NativeDocumentLinkItem> items = [];

        MatchCollection matches = LinkRegex.Matches(rawContent);
        foreach (Match match in matches)
        {
            string url = match.Groups[1].Value.Trim();
            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri))
            {
                continue;
            }

            string display = StripTagRegex.Replace(match.Groups[2].Value, " ").Trim();
            display = WebUtility.HtmlDecode(WhiteSpaceRegex.Replace(display, " "));
            if (string.IsNullOrWhiteSpace(display))
            {
                display = uri.Host;
            }

            items.Add(new NativeDocumentLinkItem
            {
                DisplayName = display,
                Url = uri.AbsoluteUri,
            });

            if (items.Count >= MaxLinkCount)
            {
                break;
            }
        }

        return items;
    }

    private static string ResolveLocalPath(string source, string siteRoot, string baseDirectory)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            return string.Empty;
        }

        string trimmed = source.Trim();
        if (trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return string.Empty;
        }

        if (trimmed.StartsWith('/'))
        {
            string rootedRelative = trimmed.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            return Path.Combine(siteRoot, rootedRelative);
        }

        string relative = trimmed.Replace('/', Path.DirectorySeparatorChar);
        return Path.GetFullPath(Path.Combine(baseDirectory, relative));
    }
}

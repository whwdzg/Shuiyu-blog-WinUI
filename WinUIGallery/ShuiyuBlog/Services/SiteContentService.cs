// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WinUIGallery.ShuiyuBlog.Models;

namespace WinUIGallery.ShuiyuBlog.Services;

public sealed class SiteContentService
{
    private static readonly string[] SupportedExtensions = [".html", ".md", ".json", ".txt"];

    private static readonly Regex TitleRegex = new("<title>(.*?)</title>", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
    private static readonly Regex StripScriptRegex = new("<script[\\s\\S]*?</script>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex StripStyleRegex = new("<style[\\s\\S]*?</style>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex StripTagRegex = new("<[^>]+>", RegexOptions.Compiled);
    private static readonly Regex WhiteSpaceRegex = new("\\s+", RegexOptions.Compiled);

    public async Task<IReadOnlyList<BlogEntry>> LoadEntriesAsync()
    {
        string siteRoot = Path.Combine(AppContext.BaseDirectory, "SiteContent");
        if (!Directory.Exists(siteRoot))
        {
            return [];
        }

        List<string> files = Directory
            .EnumerateFiles(siteRoot, "*.*", SearchOption.AllDirectories)
            .Where(path => SupportedExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase))
            .ToList();

        List<BlogEntry> entries = new(files.Count);

        foreach (string file in files)
        {
            string relativePath = Path.GetRelativePath(siteRoot, file).Replace('\\', '/');
            if (relativePath.StartsWith("Legacy-1.0/", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string text = await File.ReadAllTextAsync(file);

            string title = GetTitle(relativePath, text);
            string normalizedText = NormalizeContent(Path.GetExtension(file), text);
            string summary = GetSummary(normalizedText, 180);
            string preview = GetSummary(normalizedText, 8000);

            entries.Add(new BlogEntry
            {
                RelativePath = relativePath,
                Title = title,
                Section = GetSection(relativePath),
                FileType = Path.GetExtension(file).TrimStart('.').ToUpperInvariant(),
                Summary = summary,
                PreviewText = preview,
            });
        }

        return entries
            .OrderBy(entry => entry.Section)
            .ThenBy(entry => entry.RelativePath)
            .ToList();
    }

    private static string GetSection(string relativePath)
    {
        string firstPart = relativePath.Split('/', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty;
        return firstPart.ToLowerInvariant() switch
        {
            "column" => "专栏",
            "projects" => "项目",
            "media" => "媒体",
            "tool" => "工具",
            "user" => "用户",
            "video-archive" => "视频归档",
            "legacy-1.0" => "旧版归档",
            "resource" => "资源",
            "css" => "样式",
            "js" => "脚本",
            "includes" => "公共片段",
            _ => "根目录",
        };
    }

    private static string GetTitle(string relativePath, string content)
    {
        Match titleMatch = TitleRegex.Match(content);
        if (titleMatch.Success)
        {
            return WhiteSpaceRegex.Replace(WebUtility.HtmlDecode(titleMatch.Groups[1].Value).Trim(), " ");
        }

        string fileName = Path.GetFileNameWithoutExtension(relativePath);
        return string.IsNullOrWhiteSpace(fileName) ? relativePath : fileName;
    }

    private static string NormalizeContent(string extension, string content)
    {
        if (extension.Equals(".html", StringComparison.OrdinalIgnoreCase))
        {
            string removedScript = StripScriptRegex.Replace(content, " ");
            string removedStyle = StripStyleRegex.Replace(removedScript, " ");
            string withoutTags = StripTagRegex.Replace(removedStyle, " ");
            return WhiteSpaceRegex.Replace(WebUtility.HtmlDecode(withoutTags).Trim(), " ");
        }

        return WhiteSpaceRegex.Replace(content.Trim(), " ");
    }

    private static string GetSummary(string value, int maxLength)
    {
        if (value.Length <= maxLength)
        {
            return value;
        }

        return value[..maxLength] + "...";
    }
}

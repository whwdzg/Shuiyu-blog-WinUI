// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using WinUIGallery.ShuiyuBlog.Models;

namespace WinUIGallery.ShuiyuBlog.Services;

public sealed class DetailCardService
{
    private static readonly Regex TitleRegex = new("<title>(.*?)</title>", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
    private static readonly Regex MetaDescRegex = new("<meta[^>]*name=\"description\"[^>]*content=\"(.*?)\"", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
    private static readonly Regex FirstImageRegex = new("<img[^>]*src=\"(.*?)\"", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
    private static readonly Regex StripTagsRegex = new("<[^>]+>", RegexOptions.Compiled);

    public IReadOnlyList<DetailCardItem> LoadCards(string sectionName)
    {
        string sectionFolder = Path.Combine(AppContext.BaseDirectory, "SiteContent", sectionName);
        if (!Directory.Exists(sectionFolder))
        {
            return [];
        }

        string[] htmlFiles = Directory.GetFiles(sectionFolder, "*.html", SearchOption.TopDirectoryOnly);
        List<DetailCardItem> cards = new(htmlFiles.Length);

        foreach (string file in htmlFiles.OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
        {
            string html = File.ReadAllText(file);
            string relativePath = Path.GetRelativePath(Path.Combine(AppContext.BaseDirectory, "SiteContent"), file).Replace('\\', '/');
            string title = ResolveTitle(file, html);
            string description = ResolveDescription(html);
            string? imagePath = ResolveImagePath(relativePath, html);

            cards.Add(new DetailCardItem
            {
                Title = title,
                Description = description,
                RelativePath = relativePath,
                ImagePath = imagePath,
                ExternalUrl = $"https://whwdzg.github.io/{relativePath}",
            });
        }

        return cards;
    }

    private static string ResolveTitle(string filePath, string html)
    {
        Match match = TitleRegex.Match(html);
        if (match.Success)
        {
            return match.Groups[1].Value.Trim();
        }

        return Path.GetFileNameWithoutExtension(filePath);
    }

    private static string ResolveDescription(string html)
    {
        Match metaDesc = MetaDescRegex.Match(html);
        if (metaDesc.Success)
        {
            return metaDesc.Groups[1].Value.Trim();
        }

        string plain = StripTagsRegex.Replace(html, " ");
        plain = Regex.Replace(plain, "\\s+", " ").Trim();
        if (plain.Length > 140)
        {
            return plain[..140] + "...";
        }

        return plain;
    }

    private static string? ResolveImagePath(string relativePath, string html)
    {
        Match firstImage = FirstImageRegex.Match(html);
        if (!firstImage.Success)
        {
            return null;
        }

        string imageRef = firstImage.Groups[1].Value.Trim();
        if (imageRef.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            return imageRef;
        }

        string parentFolder = Path.GetDirectoryName(relativePath)?.Replace('\\', '/') ?? string.Empty;
        if (imageRef.StartsWith("/", StringComparison.Ordinal))
        {
            return "ms-appx:///SiteContent" + imageRef;
        }

        string combined = string.IsNullOrEmpty(parentFolder) ? imageRef : parentFolder + "/" + imageRef;
        return "ms-appx:///SiteContent/" + combined;
    }
}

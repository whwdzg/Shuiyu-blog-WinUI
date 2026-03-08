// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace WinUIGallery.ShuiyuBlog.Models;

public sealed class BlogEntry
{
    public string RelativePath { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public string Section { get; init; } = string.Empty;

    public string FileType { get; init; } = string.Empty;

    public string Summary { get; init; } = string.Empty;

    public string PreviewText { get; init; } = string.Empty;
}

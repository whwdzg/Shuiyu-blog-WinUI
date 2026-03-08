// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace WinUIGallery.ShuiyuBlog.Models;

public sealed class DetailCardItem
{
    public string Title { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public string RelativePath { get; init; } = string.Empty;

    public string? ImagePath { get; init; }

    public string ExternalUrl { get; init; } = string.Empty;
}

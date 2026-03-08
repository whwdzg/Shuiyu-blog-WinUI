// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace WinUIGallery.ShuiyuBlog.Models;

public sealed class NativeDocumentDetail
{
    public string Title { get; init; } = string.Empty;

    public string RelativePath { get; init; } = string.Empty;

    public string Section { get; init; } = string.Empty;

    public string FileType { get; init; } = string.Empty;

    public string PlainText { get; init; } = string.Empty;

    public IReadOnlyList<NativeDocumentImageItem> Images { get; init; } = [];

    public IReadOnlyList<NativeDocumentLinkItem> Links { get; init; } = [];
}

public sealed class NativeDocumentImageItem
{
    public string DisplayName { get; init; } = string.Empty;

    public string SourcePath { get; init; } = string.Empty;
}

public sealed class NativeDocumentLinkItem
{
    public string DisplayName { get; init; } = string.Empty;

    public string Url { get; init; } = string.Empty;
}

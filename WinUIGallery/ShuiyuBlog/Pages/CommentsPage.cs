// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace WinUIGallery.ShuiyuBlog.Pages;

public sealed class CommentsPage : NativeHtmlPageBase
{
    public CommentsPage()
    {
        RenderPage("column/comments.html", RenderMode.Comments);
    }
}

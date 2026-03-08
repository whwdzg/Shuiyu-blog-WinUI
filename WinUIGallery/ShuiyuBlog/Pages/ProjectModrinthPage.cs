// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace WinUIGallery.ShuiyuBlog.Pages;

public sealed class ProjectModrinthPage : NativeHtmlPageBase
{
    public ProjectModrinthPage()
    {
        RenderPage("projects/modrinth.html", RenderMode.Cards);
    }
}

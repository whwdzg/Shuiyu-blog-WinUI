// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace WinUIGallery.ShuiyuBlog.Pages;

public sealed class ProjectGithubPage : NativeHtmlPageBase
{
    public ProjectGithubPage()
    {
        RenderPage("projects/github.html", RenderMode.Cards);
    }
}

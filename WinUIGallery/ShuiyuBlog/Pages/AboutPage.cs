// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace WinUIGallery.ShuiyuBlog.Pages;

public sealed class AboutPage : NativeHtmlPageBase
{
    public AboutPage()
    {
        RenderPage("about.html", RenderMode.Generic);
    }
}

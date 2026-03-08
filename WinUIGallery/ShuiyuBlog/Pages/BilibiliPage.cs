// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace WinUIGallery.ShuiyuBlog.Pages;

public sealed class BilibiliPage : NativeHtmlPageBase
{
    public BilibiliPage()
    {
        RenderPage("media/bilibili.html", RenderMode.Cards);
    }
}

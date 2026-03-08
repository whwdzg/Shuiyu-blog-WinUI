// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace WinUIGallery.Helpers;

internal static class NavigationPageMappings
{
    public static readonly Dictionary<string, Type> PageDictionary = new(StringComparer.Ordinal);
}

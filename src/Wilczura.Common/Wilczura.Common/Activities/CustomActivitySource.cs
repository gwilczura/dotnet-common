﻿using System.Diagnostics;
using Wilczura.Common.Consts;

namespace Wilczura.Common.Activities;

public static class CustomActivitySource
{
    public static readonly Lazy<ActivitySource> Source = new(() =>
                new ActivitySource(ObservabilityConsts.DefaultListenerName));
}

﻿// Copyright (c) Weihan Li.All rights reserved.
// Licensed under the MIT license.

using System.ComponentModel;

namespace HTTPie.Models;

[Flags]
public enum OutputFormat
{
    [Description("none")]
    None = 0,

    [Description("h")]
    ResponseHeaders = 1,

    [Description("b")]
    ResponseBody = 2,

    [Description("hb")]
    ResponseInfo = ResponseHeaders | ResponseBody,

    [Description("H")]
    RequestHeaders = 4,

    [Description("B")]
    RequestBody = 8,

    [Description("HB")]
    RequestInfo = RequestHeaders | RequestBody,

    [Description("t")]
    Timestamp = 16,

    [Description("hbt")]
    ResponseInfoWithTimestamp = ResponseHeaders | ResponseBody | Timestamp,

    [Description("HBhbt")]
    All = RequestInfo | ResponseInfo | Timestamp
}

using System;
using System.ComponentModel;

namespace HTTPie.Models
{
    [Flags]
    public enum OutputFormat
    {
        None = 0,
        [Description("h")] ResponseHeaders = 1,
        [Description("b")] ResponseBody = 2,
        ResponseInfo = ResponseHeaders | ResponseBody,
        [Description("H")] RequestHeaders = 4,
        [Description("B")] RequestBody = 8,
        RequestInfo = RequestHeaders | RequestBody,
        All = RequestInfo | ResponseInfo
    }
}
using System;

namespace HTTPie.Models
{
    [Flags]
    public enum OutputFormat
    {
        ResponseStatus = 1,
        ResponseHeaders = 2,
        ResponseBody = 4,
        RequestStatus = 8,
        RequestHeaders = 16,
        RequestBody = 32
    }
}
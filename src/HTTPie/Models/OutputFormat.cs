using System;

namespace HTTPie.Models
{
    [Flags]
    public enum OutputFormat
    {
        ResponseStatus = 1,
        ResponseHeaders = 2,
        ResponseBody = 4,
        ResponseInfo = ResponseStatus | ResponseHeaders | ResponseBody,
        RequestStatus = 8,
        RequestHeaders = 16,
        RequestBody = 32,
        RequestInfo = RequestStatus | RequestHeaders | RequestBody
    }
}
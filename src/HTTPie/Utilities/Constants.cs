// Copyright (c) Weihan Li.All rights reserved.
// Licensed under the MIT license.

namespace HTTPie.Utilities;

public static class Constants
{
    public const string ApplicationName = "dotnet-httpie";

    public const string ParamNameRegex = @"^[a-zA-Z_][\w_\-]*$";

    public const string ContentTypeHeaderName = "Content-Type";
    public const string ContentDispositionHeaderName = "Content-Disposition";

    public const string RequestMethodExistsPropertyName = "RequestMethodExists";
    public const string ResponseOutputFormatPropertyName = "OutputFormat";
    public const string ResponseListPropertyName = "ResponseList";

    public const string AuthorizationHeaderName = "Authorization";

    public const string RequestTimestampHeaderName = $"X-{ApplicationName}-RequestTimestamp";
    public const string ResponseTimestampHeaderName = $"X-{ApplicationName}-ResponseTimestamp";
    public const string RequestDurationHeaderName = $"X-{ApplicationName}-Duration";

    public const string ResponseCheckSumValueHeaderName = $"X-{ApplicationName}-CheckSum-Value";
    public const string ResponseCheckSumValidHeaderName = $"X-{ApplicationName}-CheckSum-Valid";

#pragma warning disable 8602
    private static readonly string AppVersion = typeof(Constants).Assembly.GetName().Version.ToString(3);
#pragma warning restore 8602
    public static readonly string DefaultUserAgent = $"{ApplicationName}/{AppVersion}";

    public static class FlagNames
    {
        public const string IsFormContentType = "IsFormContentType";
        public const string IsLoadTest = "IsLoadTest";
        public const string IsStreamingMode = "IsStreamingMode";
        public const string StreamingCompleted = "StreamingCompleted";
    }
}

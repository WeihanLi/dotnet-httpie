namespace HTTPie.Utilities
{
    public static class Constants
    {
        public const string ApplicationName = "dotnet-HTTPie";
        public const string PlainTextMediaType = "plain/text";
        public const string JsonMediaType = "application/json";

        public const string ContentTypeHeaderName = "Content-Type";
        public const string FormContentType = "application/x-www-form-urlencoded;charset=utf-8";
        public const string JsonContentType = "application/json;charset=utf-8";

#pragma warning disable 8602
        private static readonly string AppVersion = typeof(Constants).Assembly.GetName().Version.ToString(3);
#pragma warning restore 8602
        public static readonly string DefaultUserAgent = $"{ApplicationName}/{AppVersion}";
    }
}
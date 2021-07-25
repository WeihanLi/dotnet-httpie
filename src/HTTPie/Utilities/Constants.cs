namespace HTTPie.Utilities
{
    public static class Constants
    {
        public const string FormContentType = "application/x-www-form-urlencoded; charset=utf-8";
        public const string JsonContentType = "application/json";
#pragma warning disable 8602
        private static readonly string AppVersion = typeof(Constants).Assembly.GetName().Version.ToString(3);
#pragma warning restore 8602
        public static readonly string DefaultUserAgent = $"dotnet-HTTPie/{AppVersion}";
    }
}
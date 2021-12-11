using HTTPie.Abstractions;
using HTTPie.Models;
using HTTPie.Utilities;
using System.Text;

namespace HTTPie.Middleware;

public class AuthenticationMiddleware : IRequestMiddleware
{
    public static readonly Option<string> AuthenticationTypeOption = new(new[] { "--auth-type", "-A" }, () => "Basic", "Authentication type");
    public static readonly Option<string> AuthenticationValueOption = new(new[] { "--auth", "-a" }, "Authentication value");

    static AuthenticationMiddleware()
    {
        AuthenticationTypeOption.AddSuggestions(new[]
        {
                "Basic",
                "Bearer"
            });
    }

    public ICollection<Option> SupportedOptions() => new Option[] { AuthenticationTypeOption, AuthenticationValueOption };

    public Task Invoke(HttpRequestModel requestModel, Func<Task> next)
    {
        if (requestModel.ParseResult.HasOption(AuthenticationValueOption))
        {
            var authValue = requestModel.ParseResult.ValueForOption(AuthenticationValueOption);
            if (!requestModel.Headers.ContainsKey(Constants.AuthenticationHeaderName) && !string.IsNullOrEmpty(authValue))
            {
                var authType = requestModel.ParseResult.ValueForOption(AuthenticationTypeOption);
                var authHeaderValue = GetAuthHeader(authType, authValue);
                requestModel.Headers.TryAdd(Constants.AuthenticationHeaderName, authHeaderValue);
            }
        }
        return next();
    }

    private static string GetAuthHeader(string? authType, string authValue)
    {
        var authSchema = GetAuthSchema(authType);
        return authSchema switch
        {
            "Basic" => $"{authSchema} {Convert.ToBase64String(Encoding.UTF8.GetBytes(authValue))}",
            _ => $"{authSchema} {authValue}"

        };

        static string GetAuthSchema(string? authenticationType)
        {
            if (string.IsNullOrEmpty(authenticationType))
            {
                return "Basic";
            }
            if ("jwt".EqualsIgnoreCase(authenticationType))
            {
                return "Bearer";
            }
            return authenticationType;
        }
    }
}

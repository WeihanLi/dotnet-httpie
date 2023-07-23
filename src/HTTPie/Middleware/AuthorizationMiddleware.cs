// Copyright (c) Weihan Li.All rights reserved.
// Licensed under the MIT license.

using HTTPie.Abstractions;
using HTTPie.Models;
using HTTPie.Utilities;
using System.Text;

namespace HTTPie.Middleware;

public sealed class AuthorizationMiddleware : IRequestMiddleware
{
    private static readonly Option<string> AuthenticationTypeOption = new(new[] { "--auth-type", "-A" }, () => "Basic", "Authentication type");
    private static readonly Option<string> AuthenticationValueOption = new(new[] { "--auth", "-a" }, "Authentication value");

    static AuthorizationMiddleware()
    {
        AuthenticationTypeOption.AddCompletions(new[]
        {
            "Basic",
            "Bearer"
        });
    }

    public Option[] SupportedOptions() => new Option[] { AuthenticationTypeOption, AuthenticationValueOption };

    public Task InvokeAsync(HttpRequestModel requestModel, Func<HttpRequestModel, Task> next)
    {
        if (requestModel.ParseResult.HasOption(AuthenticationValueOption))
        {
            var authValue = requestModel.ParseResult.GetValueForOption(AuthenticationValueOption);
            if (!requestModel.Headers.ContainsKey(Constants.AuthorizationHeaderName) && !string.IsNullOrEmpty(authValue))
            {
                var authType = requestModel.ParseResult.GetValueForOption(AuthenticationTypeOption);
                var authHeaderValue = GetAuthHeader(authType, authValue);
                requestModel.Headers.TryAdd(Constants.AuthorizationHeaderName, authHeaderValue);
            }
        }
        return next(requestModel);
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
            if (string.IsNullOrEmpty(authenticationType) || "basic".EqualsIgnoreCase(authenticationType))
            {
                return "Basic";
            }
            if ("jwt".EqualsIgnoreCase(authenticationType) || "bearer".EqualsIgnoreCase(authenticationType))
            {
                return "Bearer";
            }
            return authenticationType;
        }
    }
}

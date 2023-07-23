// Copyright (c) Weihan Li.All rights reserved.
// Licensed under the MIT license.

using HTTPie.Abstractions;
using HTTPie.Models;
using Json.Schema;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using OutputFormat = Json.Schema.OutputFormat;

namespace HTTPie.Middleware;

public sealed class JsonSchemaValidationMiddleware : IResponseMiddleware
{
    private readonly ILogger<JsonSchemaValidationMiddleware> _logger;
    private const string JsonSchemaValidationResultHeader = "X-JsonSchema-ValidationResult";

    private const string JsonSchemaLoadFailed = "JsonSchema fail to load";
    private const string JsonSchemaValidateFailed = "JsonSchema fail to validate";


    private static readonly Option<string> JsonSchemaPathOption = new("--json-schema-path", "Json schema path");
    private static readonly Option<OutputFormat> JsonSchemaValidationOutputFormatOption = new("--json-schema-out-format", () => OutputFormat.Detailed, "Json schema validation result output format");

    public JsonSchemaValidationMiddleware(ILogger<JsonSchemaValidationMiddleware> logger)
    {
        _logger = logger;
    }

    public Option[] SupportedOptions()
    {
        return new Option[] { JsonSchemaPathOption, JsonSchemaValidationOutputFormatOption };
    }

    public async Task InvokeAsync(HttpContext context, Func<HttpContext, Task> next)
    {
        var schemaPath = context.Request.ParseResult.GetValueForOption(JsonSchemaPathOption)?.Trim();
        if (string.IsNullOrEmpty(schemaPath))
        {
            await next(context);
            return;
        }

        var validationResultMessage = string.Empty;
        JsonSchema? jsonSchema = null;
        try
        {
            if (schemaPath.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                || schemaPath.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                using var httpClient = new HttpClient();
                jsonSchema = await httpClient.GetFromJsonAsync<JsonSchema>(schemaPath);
            }
            else
            {
                jsonSchema = JsonSchema.FromFile(schemaPath);
            }
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, JsonSchemaLoadFailed);
            validationResultMessage = JsonSchemaLoadFailed;
        }
        if (jsonSchema is not null)
        {
            try
            {
                var options = new ValidationOptions
                {
                    OutputFormat = context.Request.ParseResult.GetValueForOption(JsonSchemaValidationOutputFormatOption)
                };
                var validateResult = jsonSchema.Validate(context.Response.Body, options);
                validationResultMessage = $"{validateResult.IsValid},{validateResult.Message}".Trim(',');
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, JsonSchemaValidateFailed);
                validationResultMessage = JsonSchemaValidateFailed;
            }
        }
        context.Response.Headers.TryAdd(JsonSchemaValidationResultHeader, validationResultMessage);

        await next(context);
    }
}

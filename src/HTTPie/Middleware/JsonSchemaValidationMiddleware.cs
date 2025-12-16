// Copyright (c) Weihan Li.All rights reserved.
// Licensed under the MIT license.

using HTTPie.Abstractions;
using HTTPie.Models;
using HTTPie.Utilities;
using Json.Schema;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json;
using OutputFormat = Json.Schema.OutputFormat;

namespace HTTPie.Middleware;

public sealed class JsonSchemaValidationMiddleware(ILogger<JsonSchemaValidationMiddleware> logger) : IResponseMiddleware
{
    private const string JsonSchemaValidationResultHeader = "X-JsonSchema-ValidationResult";
    private const string JsonSchemaLoadFailed = "JsonSchema fail to load";
    private const string JsonSchemaValidateFailed = "JsonSchema fail to validate";


    private static readonly Option<string> JsonSchemaPathOption = new("--json-schema-path")
    {
        Description = "Json schema path"
    };

    private static readonly Option<OutputFormat> JsonSchemaValidationOutputFormatOption =
        new("--json-schema-out-format")
        {
            Description = "Json schema validation result output format",
            DefaultValueFactory = _ => OutputFormat.List
        };

    public Option[] SupportedOptions() => [JsonSchemaPathOption, JsonSchemaValidationOutputFormatOption];

    public async Task InvokeAsync(HttpContext context, Func<HttpContext, Task> next)
    {
        var schemaPath = context.Request.ParseResult.GetValue(JsonSchemaPathOption)?.Trim();
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
                jsonSchema = await httpClient.GetFromJsonAsync(schemaPath, AppSerializationContext.Default.JsonSchema);
            }
            else
            {
                jsonSchema = JsonSchema.FromFile(schemaPath);
            }
        }
        catch (Exception e)
        {
            logger.LogWarning(e, JsonSchemaLoadFailed);
            validationResultMessage = JsonSchemaLoadFailed;
        }

        if (jsonSchema is not null)
        {
            try
            {
                var options = new EvaluationOptions()
                {
                    OutputFormat =
                        context.Request.ParseResult.GetValue(JsonSchemaValidationOutputFormatOption)
                };

                var resposenJsonElement = JsonElement.Parse(context.Response.Body);
                var validateResult = jsonSchema.Evaluate(resposenJsonElement, options);
                validationResultMessage = $"{validateResult.IsValid},{validateResult.Errors.ToJson()}".Trim(',');
            }
            catch (Exception e)
            {
                logger.LogWarning(e, JsonSchemaValidateFailed);
                validationResultMessage = JsonSchemaValidateFailed;
            }
        }

        context.Response.Headers.TryAdd(JsonSchemaValidationResultHeader, validationResultMessage);

        await next(context);
    }
}

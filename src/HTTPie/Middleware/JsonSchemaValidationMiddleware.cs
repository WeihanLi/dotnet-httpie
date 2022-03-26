// Copyright (c) Weihan Li. All rights reserved.
// Licensed under the MIT license.

using HTTPie.Abstractions;
using HTTPie.Models;
using Json.Schema;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using OutputFormat = Json.Schema.OutputFormat;

namespace HTTPie.Middleware;

public class JsonSchemaValidationMiddleware: IResponseMiddleware
{
    private readonly ILogger<JsonSchemaValidationMiddleware> _logger;
    private const string JsonSchemaValidationResultHeader = "X-JsonSchema-ValidationResult";
    
    private const string JsonSchemaLoadFailed = "JsonSchemaFailedToLoad";
    private const string JsonSchemaValidateFailed = "JsonSchemaFailedToValidate";
    
    
    private static readonly Option<string>  JsonSchemaFilePathOption = new("--json-schema-file-path", "Json schema file path");
    private static readonly Option<OutputFormat> JsonSchemaValidationOutputFormatOption = new("--json-schema-out-format",()=> OutputFormat.Detailed, "Json schema validation result output format");

    public JsonSchemaValidationMiddleware(ILogger<JsonSchemaValidationMiddleware> logger)
    {
        _logger = logger;
    }

    public ICollection<Option> SupportedOptions()
    {
        return new Option[] { JsonSchemaFilePathOption, JsonSchemaValidationOutputFormatOption };
    }

    public async Task Invoke(HttpContext context, Func<Task> next)
    {
        var schemaPath = context.Request.ParseResult.GetValueForOption(JsonSchemaFilePathOption)?.Trim();
        if (string.IsNullOrEmpty(schemaPath))
        {
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
            _logger.LogWarning(e, "Load JsonSchema failed");

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
                validationResultMessage = $"{validateResult.IsValid}:{validateResult.Message}".Trim(':');
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Validate Json schema failed");
                validationResultMessage = JsonSchemaValidateFailed;
            }
        }
        context.Response.Headers.TryAdd(JsonSchemaValidationResultHeader, validationResultMessage);
    }
}

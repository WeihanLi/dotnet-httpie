// Copyright (c) Weihan Li.All rights reserved.
// Licensed under the MIT license.

using HTTPie.Abstractions;
using HTTPie.Implement;
using HTTPie.Models;
using HTTPie.Utilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using WeihanLi.Common.Http;

namespace HTTPie.Commands;

/// <summary>
/// The <c>test</c> subcommand – executes an HTTP API test collection.
/// </summary>
public sealed partial class TestCommand : Command
{
    private static readonly Argument<string> CollectionPathArgument = new("collectionPath")
    {
        Description = "Path to the test collection file (.httptest.json)",
        Arity = ArgumentArity.ZeroOrOne
    };

    private static readonly Option<string> EnvironmentOption =
        new("--env")
        {
            Description = "The environment name to use (matches a variable block in the collection)"
        };

    private static readonly Option<string> EnvironmentFileOption =
        new("--env-file")
        {
            Description = "Path to an environment file (.httptest.env.json)"
        };

    private static readonly Option<bool> OfflineOption =
        new("--offline")
        {
            Description = "Print requests without sending them"
        };

    public TestCommand() : base("test", "execute an HTTP API test collection")
    {
        Arguments.Add(CollectionPathArgument);
        Options.Add(EnvironmentOption);
        Options.Add(EnvironmentFileOption);
        Options.Add(OfflineOption);
    }

    public async Task InvokeAsync(
        ParseResult parseResult, CancellationToken cancellationToken, IServiceProvider serviceProvider)
    {
        var logger = serviceProvider.GetRequiredService<ILogger>();
        var requestExecutor = serviceProvider.GetRequiredService<IRawHttpRequestExecutor>();

        var collectionPath = parseResult.GetValue(CollectionPathArgument);
        if (string.IsNullOrEmpty(collectionPath))
        {
            throw new InvalidOperationException("A collection path must be provided.");
        }
        if (!File.Exists(collectionPath))
        {
            throw new InvalidOperationException($"Collection file not found: {collectionPath}");
        }

        var environmentName = parseResult.GetValue(EnvironmentOption);
        var environmentFilePath = parseResult.GetValue(EnvironmentFileOption);
        var offline = parseResult.GetValue(OfflineOption);

        // Load collection
        await using var collectionStream = File.OpenRead(collectionPath);
        var collection = await JsonSerializer.DeserializeAsync(
            collectionStream,
            AppSerializationContext.Default.HttpTestCollection,
            cancellationToken);

        if (collection is null)
            throw new InvalidOperationException("Failed to parse the test collection file.");

        // Load environment variables
        var envVariables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        await LoadEnvironmentVariablesAsync(collection, environmentName, environmentFilePath, envVariables, cancellationToken);

        logger.LogDebug("Executing test collection: {CollectionName}", collection.Name);

        var results = new List<HttpTestResult>();
        await RunCollectionAsync(collection, envVariables, requestExecutor, offline, results, cancellationToken);

        // Report summary
        PrintSummary(collection.Name, results);

        // Exit with non-zero code if any test failed
        var failedCount = results.Count(r => !r.Passed);
        if (failedCount > 0)
        {
            throw new InvalidOperationException($"{failedCount} test(s) failed.");
        }
    }

    // -----------------------------------------------------------------------
    // Environment loading
    // -----------------------------------------------------------------------

    private static async Task LoadEnvironmentVariablesAsync(
        HttpTestCollection collection,
        string? environmentName,
        string? environmentFilePath,
        Dictionary<string, string> envVariables,
        CancellationToken cancellationToken)
    {
        // Apply collection-level variables first as defaults (lowest priority)
        foreach (var kv in collection.Variables)
            envVariables[kv.Key] = kv.Value;

        // Then apply env file variables on top (higher priority, overrides collection defaults).
        // Uses the explicitly specified --env name, or falls back to "default" when none is given.
        // If neither a matching named environment nor a "default" environment exists, env file is skipped.
        if (!string.IsNullOrEmpty(environmentFilePath) && File.Exists(environmentFilePath))
        {
            var effectiveEnvName = string.IsNullOrEmpty(environmentName) ? "default" : environmentName;
            await using var stream = File.OpenRead(environmentFilePath);
            var envs = await JsonSerializer.DeserializeAsync(
                stream,
                AppSerializationContext.Default.ListHttpTestEnvironment,
                cancellationToken);

            if (envs is not null)
            {
                foreach (var env in envs)
                {
                    if (env.Name.Equals(effectiveEnvName, StringComparison.OrdinalIgnoreCase))
                    {
                        foreach (var kv in env.Variables)
                            envVariables[kv.Key] = kv.Value;
                        break;
                    }
                }
            }
        }
    }

    // -----------------------------------------------------------------------
    // Collection execution
    // -----------------------------------------------------------------------

    private static async Task RunCollectionAsync(
        HttpTestCollection collection,
        Dictionary<string, string> collectionVariables,
        IRawHttpRequestExecutor requestExecutor,
        bool offline,
        List<HttpTestResult> results,
        CancellationToken cancellationToken)
    {
        Console.WriteLine($"=== Collection: {collection.Name} ===");

        foreach (var group in collection.Groups)
        {
            Console.WriteLine($"--- Group: {group.Name} ---");

            // Merge group variables on top of collection variables
            var groupVariables = new Dictionary<string, string>(collectionVariables, StringComparer.OrdinalIgnoreCase);
            foreach (var kv in group.Variables)
                groupVariables[kv.Key] = kv.Value;

            foreach (var request in group.Requests)
            {
                var result = await RunRequestAsync(
                    request,
                    groupVariables,
                    collection.PreScript,
                    collection.PostScript,
                    group.PreScript,
                    group.PostScript,
                    requestExecutor,
                    offline,
                    cancellationToken);
                results.Add(result);
            }
        }
    }

    private static async Task<HttpTestResult> RunRequestAsync(
        HttpTestRequest request,
        Dictionary<string, string> groupVariables,
        string? collectionPreScript,
        string? collectionPostScript,
        string? groupPreScript,
        string? groupPostScript,
        IRawHttpRequestExecutor requestExecutor,
        bool offline,
        CancellationToken cancellationToken)
    {
        var startTimestamp = Stopwatch.GetTimestamp();
        var result = new HttpTestResult { RequestName = request.Name };

        // Merge request variables on top of group variables
        var variables = new Dictionary<string, string>(groupVariables, StringComparer.OrdinalIgnoreCase);
        foreach (var kv in request.Variables)
            variables[kv.Key] = kv.Value;

        // Determine effective preScript and postScript (most specific wins)
        var effectivePreScript = request.PreScript ?? groupPreScript ?? collectionPreScript;
        var effectivePostScript = request.PostScript ?? groupPostScript ?? collectionPostScript;

        try
        {
            // Substitute variables in URL, body
            var url = SubstituteVariables(request.Url, variables);
            var body = string.IsNullOrEmpty(request.Body)
                ? null
                : SubstituteVariables(request.Body, variables);

            // Build HttpRequestMessage with initial headers (with variable substitution)
            var requestMessage = new HttpRequestMessage(GetHttpMethod(request.Method), url);
            requestMessage.TryAddHeaderIfNotExists(HttpHeaderNames.UserAgent, Constants.DefaultUserAgent);

            foreach (var (name, value) in request.Headers)
            {
                var resolvedValue = SubstituteVariables(value, variables);
                if (HttpHelper.IsWellKnownContentHeader(name))
                {
                    requestMessage.Content ??= new ByteArrayContent([]);
                    requestMessage.Content.Headers.TryAddWithoutValidation(name, resolvedValue);
                }
                else
                {
                    requestMessage.Headers.TryAddWithoutValidation(name, resolvedValue);
                }
            }

            if (!string.IsNullOrEmpty(body))
            {
                var contentType = request.Headers.TryGetValue(HttpHeaderNames.ContentType, out var ct)
                    ? ct
                    : HttpHelper.ApplicationJsonMediaType;
                requestMessage.Content = new StringContent(body, Encoding.UTF8, contentType);
            }

            // Execute preScript – may modify requestMessage.Headers or variables
            await HttpTestScriptRunner.ExecutePreScriptAsync(effectivePreScript, requestMessage, variables);

            // Print request
            Console.WriteLine($"  [{request.Name}]");
            Console.WriteLine("  Request:");
            Console.WriteLine(await requestMessage.ToRawMessageAsync(cancellationToken));

            if (offline)
            {
                result.Passed = true;
                return result;
            }

            // Execute request
            var response = await requestExecutor.ExecuteAsync(requestMessage, cancellationToken);
            var elapsed = ProfilerHelper.GetElapsedTime(startTimestamp);
            result.StatusCode = (int)response.StatusCode;
            result.Elapsed = elapsed;

            Console.WriteLine($"  Response ({elapsed.TotalMilliseconds:F0}ms):");
            Console.WriteLine(await response.ToRawMessageAsync(cancellationToken));

            // Execute postScript – may assert against response or update variables
            await HttpTestScriptRunner.ExecutePostScriptAsync(
                effectivePostScript,
                requestMessage,
                response,
                variables);

            result.Passed = true;
            Console.WriteLine($"  ✓ {request.Name} PASSED ({elapsed.TotalMilliseconds:F0}ms)");
        }
        catch (HttpTestAssertionException ex)
        {
            result.Passed = false;
            result.Error = ex.Message;
            Console.WriteLine($"  ✗ {request.Name} FAILED: {ex.Message}");
        }
        catch (Exception ex)
        {
            result.Passed = false;
            result.Error = ex.Message;
            Console.WriteLine($"  ✗ {request.Name} ERROR: {ex.Message}");
        }

        return result;
    }

    // -----------------------------------------------------------------------
    // HTTP method helper
    // -----------------------------------------------------------------------

    // Cache common HTTP method instances to avoid repeated allocations
    private static readonly Dictionary<string, HttpMethod> KnownHttpMethods =
        new(StringComparer.OrdinalIgnoreCase)
        {
            { "GET",     HttpMethod.Get     },
            { "POST",    HttpMethod.Post    },
            { "PUT",     HttpMethod.Put     },
            { "DELETE",  HttpMethod.Delete  },
            { "PATCH",   HttpMethod.Patch   },
            { "HEAD",    HttpMethod.Head    },
            { "OPTIONS", HttpMethod.Options },
        };

    private static HttpMethod GetHttpMethod(string method) =>
        KnownHttpMethods.TryGetValue(method, out var m) ? m : new HttpMethod(method.ToUpperInvariant());

    // -----------------------------------------------------------------------
    // Variable substitution
    // -----------------------------------------------------------------------

    [GeneratedRegex(@"\{\{(?<name>[a-zA-Z_][\w\.]*)\}\}")]
    private static partial Regex VariableRefRegex { get; }

    private static string SubstituteVariables(string text, Dictionary<string, string> variables)
    {
        if (string.IsNullOrEmpty(text)) return text;

        return VariableRefRegex.Replace(text, match =>
        {
            var name = match.Groups["name"].Value;
            return variables.TryGetValue(name, out var value) ? value : match.Value;
        });
    }

    // -----------------------------------------------------------------------
    // Summary reporting
    // -----------------------------------------------------------------------

    private static void PrintSummary(string collectionName, List<HttpTestResult> results)
    {
        var passed = results.Count(r => r.Passed);
        var failed = results.Count(r => !r.Passed);
        Console.WriteLine();
        Console.WriteLine($"=== Test Summary: {collectionName} ===");
        Console.WriteLine($"  Total:  {results.Count}");
        ConsoleHelper.WriteLineWithColor($"  Passed: {passed}", passed > 0 ? ConsoleColor.Green : ConsoleColor.White);
        if (failed > 0)
        {
            ConsoleHelper.WriteLineWithColor($"  Failed: {failed}", ConsoleColor.Red);
            foreach (var r in results.Where(r => !r.Passed))
                ConsoleHelper.WriteLineWithColor($"    - {r.RequestName}: {r.Error}", ConsoleColor.Red);
        }
        else
        {
            Console.WriteLine($"  Failed: {failed}");
        }
    }
}

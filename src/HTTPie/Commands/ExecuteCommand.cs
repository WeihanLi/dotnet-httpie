﻿// Copyright (c) Weihan Li.All rights reserved.
// Licensed under the MIT license.

using HTTPie.Abstractions;
using HTTPie.Utilities;
using Microsoft.Extensions.DependencyInjection;
using System.CommandLine.Invocation;

namespace HTTPie.Commands;

public sealed class ExecuteCommand : Command
{
    private static readonly Argument<string> FilePathArgument = new("path", "The script path to execute");

    private static readonly Option<ExecuteScriptType> ExecuteScriptTypeOption =
        new("--type", "The script type to execute");

    public ExecuteCommand() : base("execute", "execute http request related scripts")
    {
        AddArgument(FilePathArgument);
    }

    public async Task InvokeAsync(InvocationContext invocationContext, IServiceProvider serviceProvider)
    {
        var filePath = invocationContext.ParseResult.GetValueForArgument(FilePathArgument);
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
        {
            throw new InvalidOperationException("Invalid filePath");
        }

        var httpParser = serviceProvider.GetRequiredService<IHttpParser>();
        var httpExecutor = serviceProvider.GetRequiredService<IRawHttpRequestExecutor>();
        await foreach (var request in httpParser.ParseAsync(filePath))
        {
            Console.WriteLine(request.Name);
            Console.WriteLine("Request message:");
            Console.WriteLine(await request.RequestMessage.ToRawMessageAsync());
            using var response = await httpExecutor.Execute(request, invocationContext.GetCancellationToken());
            Console.WriteLine("Response message:");
            Console.WriteLine(await response.ToRawMessageAsync());
            Console.WriteLine();
        }
    }
}

public enum ExecuteScriptType
{
    Http,
    // Curl,
    // Har,
}

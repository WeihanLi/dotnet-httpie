// Copyright (c) Weihan Li.All rights reserved.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;

namespace HTTPie.Utilities;

public static partial class LogHelper
{
    [LoggerMessage(Level = LogLevel.Debug, Message = "Input parameters: {parameters}")]
    public static partial void PrintInputParameters(this ILogger logger, string parameters);

    [LoggerMessage(Level = LogLevel.Error, EventId = 1, Message = "Invoke Request Exception")]
    public static partial void InvokeRequestException(this ILogger logger, Exception exception);
}

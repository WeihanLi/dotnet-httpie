// Copyright (c) Weihan Li.All rights reserved.
// Licensed under the MIT license.

namespace HTTPie.Models;
public sealed class LoadTestReportModel
{
    public int TotalRequestCount { get; init; }
    public int SuccessRequestCount { get; init; }
    public double SuccessRequestRate => SuccessRequestCount * 100.0 / TotalRequestCount;
    public int FailRequestCount => TotalRequestCount - SuccessRequestCount;

    public double TotalElapsed { get; init; }
    public double RequestsPerSecond => TotalRequestCount * 1000 / TotalElapsed;
    public double Average { get; init; }
    public double Min { get; init; }
    public double Max { get; init; }
    public double Median { get; init; }
    public double P99 { get; init; }
    public double P95 { get; init; }
    public double P90 { get; init; }
    public double P75 { get; init; }
    public double P50 { get; init; }
}

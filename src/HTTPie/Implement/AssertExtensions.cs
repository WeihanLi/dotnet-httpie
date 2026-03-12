// Copyright (c) Weihan Li.All rights reserved.
// Licensed under the MIT license.

namespace HTTPie.Implement;

/// <summary>
/// Provides assertion helpers for HTTP test scripts via C# 14 <c>extension</c> members.
/// All assertions throw <see cref="HttpTestAssertionException"/> on failure, which
/// immediately stops further script execution.
/// </summary>
/// <remarks>
/// These helpers are available inside Roslyn-executed test scripts because the
/// <c>HTTPie.Implement</c> namespace is imported by default. Assertions can be
/// invoked as extension methods on the relevant value:
/// <code>
///   response.StatusCode.ShouldBe(200);
///   response.body.text.ShouldContain("token");
///   var id = (long)response.body.json.id;
///   id.ShouldBeGreaterThan(0);
///   token.ShouldNotBeNullOrEmpty("Token must be present");
/// </code>
/// A static <see cref="HttpAssert"/> class is also available for xunit / <c>Debug.Assert</c>-style
/// calls:
/// <code>
///   HttpAssert.Equal(200, response.StatusCode);
///   HttpAssert.Contains("token", response.body.text);
///   HttpAssert.True(response.StatusCode &lt; 500, "Server error");
/// </code>
/// </remarks>
public static class AssertExtensions
{
    // -----------------------------------------------------------------------
    // Extensions on object? — null checks
    // -----------------------------------------------------------------------

    extension(object? value)
    {
        /// <summary>Asserts that the value is <c>null</c>.</summary>
        public void ShouldBeNull(string? message = null)
        {
            if (value is not null)
                throw new HttpTestAssertionException(
                    message ?? $"ShouldBeNull failed: value was '{value}'.");
        }

        /// <summary>Asserts that the value is not <c>null</c>.</summary>
        public void ShouldNotBeNull(string? message = null)
        {
            if (value is null)
                throw new HttpTestAssertionException(
                    message ?? "ShouldNotBeNull failed: value was null.");
        }
    }

    // -----------------------------------------------------------------------
    // Extensions on bool
    // -----------------------------------------------------------------------

    extension(bool condition)
    {
        /// <summary>Asserts that the boolean value is <c>true</c>.</summary>
        public void ShouldBeTrue(string? message = null)
        {
            if (!condition)
                throw new HttpTestAssertionException(
                    message ?? "ShouldBeTrue failed: condition was false.");
        }

        /// <summary>Asserts that the boolean value is <c>false</c>.</summary>
        public void ShouldBeFalse(string? message = null)
        {
            if (condition)
                throw new HttpTestAssertionException(
                    message ?? "ShouldBeFalse failed: condition was true.");
        }
    }

    // -----------------------------------------------------------------------
    // Extensions on int — status-code and numeric comparisons
    // -----------------------------------------------------------------------

    extension(int actual)
    {
        /// <summary>Asserts that the integer equals <paramref name="expected"/>.</summary>
        public void ShouldBe(int expected, string? message = null)
        {
            if (actual != expected)
                throw new HttpTestAssertionException(
                    message ?? $"ShouldBe failed: expected {expected}, actual {actual}.");
        }

        /// <summary>Asserts that the integer does not equal <paramref name="unexpected"/>.</summary>
        public void ShouldNotBe(int unexpected, string? message = null)
        {
            if (actual == unexpected)
                throw new HttpTestAssertionException(
                    message ?? $"ShouldNotBe failed: value was {actual}.");
        }

        /// <summary>Asserts that the integer is greater than <paramref name="threshold"/>.</summary>
        public void ShouldBeGreaterThan(int threshold, string? message = null)
        {
            if (actual <= threshold)
                throw new HttpTestAssertionException(
                    message ?? $"ShouldBeGreaterThan failed: {actual} is not greater than {threshold}.");
        }

        /// <summary>Asserts that the integer is greater than or equal to <paramref name="threshold"/>.</summary>
        public void ShouldBeGreaterThanOrEqualTo(int threshold, string? message = null)
        {
            if (actual < threshold)
                throw new HttpTestAssertionException(
                    message ?? $"ShouldBeGreaterThanOrEqualTo failed: {actual} is less than {threshold}.");
        }

        /// <summary>Asserts that the integer is less than <paramref name="threshold"/>.</summary>
        public void ShouldBeLessThan(int threshold, string? message = null)
        {
            if (actual >= threshold)
                throw new HttpTestAssertionException(
                    message ?? $"ShouldBeLessThan failed: {actual} is not less than {threshold}.");
        }
    }

    // -----------------------------------------------------------------------
    // Extensions on long — numeric comparisons
    // -----------------------------------------------------------------------

    extension(long actual)
    {
        /// <summary>Asserts that the value equals <paramref name="expected"/>.</summary>
        public void ShouldBe(long expected, string? message = null)
        {
            if (actual != expected)
                throw new HttpTestAssertionException(
                    message ?? $"ShouldBe failed: expected {expected}, actual {actual}.");
        }

        /// <summary>Asserts that the value is greater than <paramref name="threshold"/>.</summary>
        public void ShouldBeGreaterThan(long threshold, string? message = null)
        {
            if (actual <= threshold)
                throw new HttpTestAssertionException(
                    message ?? $"ShouldBeGreaterThan failed: {actual} is not greater than {threshold}.");
        }
    }

    // -----------------------------------------------------------------------
    // Extensions on string? — string-specific assertions
    // -----------------------------------------------------------------------

    extension(string? actual)
    {
        /// <summary>Asserts that the string equals <paramref name="expected"/>.</summary>
        public void ShouldBe(string? expected, string? message = null)
        {
            if (!string.Equals(actual, expected, StringComparison.Ordinal))
                throw new HttpTestAssertionException(
                    message ?? $"ShouldBe failed: expected \"{expected}\", actual \"{actual}\".");
        }

        /// <summary>Asserts that the string contains <paramref name="substring"/>.</summary>
        public void ShouldContain(string substring, string? message = null)
        {
            if (actual is null || !actual.Contains(substring, StringComparison.Ordinal))
                throw new HttpTestAssertionException(
                    message ?? $"ShouldContain failed: \"{actual}\" does not contain \"{substring}\".");
        }

        /// <summary>Asserts that the string does not contain <paramref name="substring"/>.</summary>
        public void ShouldNotContain(string substring, string? message = null)
        {
            if (actual is not null && actual.Contains(substring, StringComparison.Ordinal))
                throw new HttpTestAssertionException(
                    message ?? $"ShouldNotContain failed: \"{actual}\" contains \"{substring}\".");
        }

        /// <summary>Asserts that the string starts with <paramref name="prefix"/>.</summary>
        public void ShouldStartWith(string prefix, string? message = null)
        {
            if (actual is null || !actual.StartsWith(prefix, StringComparison.Ordinal))
                throw new HttpTestAssertionException(
                    message ?? $"ShouldStartWith failed: \"{actual}\" does not start with \"{prefix}\".");
        }

        /// <summary>Asserts that the string ends with <paramref name="suffix"/>.</summary>
        public void ShouldEndWith(string suffix, string? message = null)
        {
            if (actual is null || !actual.EndsWith(suffix, StringComparison.Ordinal))
                throw new HttpTestAssertionException(
                    message ?? $"ShouldEndWith failed: \"{actual}\" does not end with \"{suffix}\".");
        }

        /// <summary>Asserts that the string is not <c>null</c> or empty.</summary>
        public void ShouldNotBeNullOrEmpty(string? message = null)
        {
            if (string.IsNullOrEmpty(actual))
                throw new HttpTestAssertionException(
                    message ?? "ShouldNotBeNullOrEmpty failed: value was null or empty.");
        }

        /// <summary>Asserts that the string is not <c>null</c>, empty, or whitespace-only.</summary>
        public void ShouldNotBeNullOrWhiteSpace(string? message = null)
        {
            if (string.IsNullOrWhiteSpace(actual))
                throw new HttpTestAssertionException(
                    message ?? "ShouldNotBeNullOrWhiteSpace failed: value was null, empty, or whitespace.");
        }
    }
}

/// <summary>
/// Static assertion class providing xunit / <c>Debug.Assert</c>-style helpers for HTTP test scripts.
/// All methods throw <see cref="HttpTestAssertionException"/> on failure, immediately stopping
/// further script execution.
/// </summary>
/// <remarks>
/// Available directly inside Roslyn-executed scripts:
/// <code>
///   HttpAssert.Equal(200, response.StatusCode);
///   HttpAssert.True(response.StatusCode &lt; 500, "Server error");
///   HttpAssert.Contains("token", response.body.text);
///   HttpAssert.NotNull(response.body.json.id);
/// </code>
/// </remarks>
public static class HttpAssert
{
    /// <summary>Fails unconditionally with the given <paramref name="message"/>.</summary>
    public static void Fail(string message) =>
        throw new HttpTestAssertionException(message);

    /// <summary>Asserts that <paramref name="condition"/> is <c>true</c>.</summary>
    public static void True(bool condition, string? message = null)
    {
        if (!condition)
            throw new HttpTestAssertionException(message ?? "HttpAssert.True failed.");
    }

    /// <summary>Asserts that <paramref name="condition"/> is <c>false</c>.</summary>
    public static void False(bool condition, string? message = null)
    {
        if (condition)
            throw new HttpTestAssertionException(message ?? "HttpAssert.False failed.");
    }

    /// <summary>Asserts that <paramref name="value"/> is <c>null</c>.</summary>
    public static void Null(object? value, string? message = null)
    {
        if (value is not null)
            throw new HttpTestAssertionException(
                message ?? $"HttpAssert.Null failed: value was '{value}'.");
    }

    /// <summary>Asserts that <paramref name="value"/> is not <c>null</c>.</summary>
    public static void NotNull(object? value, string? message = null)
    {
        if (value is null)
            throw new HttpTestAssertionException(
                message ?? "HttpAssert.NotNull failed: value was null.");
    }

    /// <summary>Asserts that <paramref name="expected"/> and <paramref name="actual"/> are equal.</summary>
    public static void Equal<T>(T expected, T actual, string? message = null)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new HttpTestAssertionException(
                message ?? $"HttpAssert.Equal failed: expected '{expected}', actual '{actual}'.");
    }

    /// <summary>Asserts that <paramref name="unexpected"/> and <paramref name="actual"/> are not equal.</summary>
    public static void NotEqual<T>(T unexpected, T actual, string? message = null)
    {
        if (EqualityComparer<T>.Default.Equals(unexpected, actual))
            throw new HttpTestAssertionException(
                message ?? $"HttpAssert.NotEqual failed: value was '{actual}'.");
    }

    /// <summary>Asserts that the string <paramref name="haystack"/> contains <paramref name="needle"/>.</summary>
    public static void Contains(string needle, string haystack, string? message = null)
    {
        if (!haystack.Contains(needle, StringComparison.Ordinal))
            throw new HttpTestAssertionException(
                message ?? $"HttpAssert.Contains failed: \"{haystack}\" does not contain \"{needle}\".");
    }

    /// <summary>Asserts that the string <paramref name="haystack"/> does not contain <paramref name="needle"/>.</summary>
    public static void DoesNotContain(string needle, string haystack, string? message = null)
    {
        if (haystack.Contains(needle, StringComparison.Ordinal))
            throw new HttpTestAssertionException(
                message ?? $"HttpAssert.DoesNotContain failed: \"{haystack}\" contains \"{needle}\".");
    }

    /// <summary>Asserts that the string is not <c>null</c> or empty.</summary>
    public static void NotNullOrEmpty(string? value, string? message = null)
    {
        if (string.IsNullOrEmpty(value))
            throw new HttpTestAssertionException(
                message ?? "HttpAssert.NotNullOrEmpty failed: value was null or empty.");
    }

    /// <summary>Asserts that <paramref name="actual"/> is greater than <paramref name="threshold"/>.</summary>
    public static void GreaterThan<T>(T threshold, T actual, string? message = null)
        where T : IComparable<T>
    {
        if (actual.CompareTo(threshold) <= 0)
            throw new HttpTestAssertionException(
                message ?? $"HttpAssert.GreaterThan failed: {actual} is not greater than {threshold}.");
    }

    /// <summary>Asserts that <paramref name="actual"/> is less than <paramref name="threshold"/>.</summary>
    public static void LessThan<T>(T threshold, T actual, string? message = null)
        where T : IComparable<T>
    {
        if (actual.CompareTo(threshold) >= 0)
            throw new HttpTestAssertionException(
                message ?? $"HttpAssert.LessThan failed: {actual} is not less than {threshold}.");
    }
}

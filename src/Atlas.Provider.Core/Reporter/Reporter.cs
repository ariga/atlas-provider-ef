/*
Title: Reporter
Type: Source Code
Availability: https://github.com/dotnet/efcore/blob/0d1d602d72fefe14f12a86410ec70394ec8151e0/src/ef/Reporter.cs
*/

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using static Atlas.Provider.Core.AnsiConstants;
using System.CodeDom.Compiler;
using System.Text;

namespace Atlas.Provider.Core;

internal static class Reporter
{
    public static bool IsVerbose { get; set; }
    public static bool NoColor { get; set; }
    public static bool PrefixOutput { get; set; }

    [return: NotNullIfNotNull("value")]
    public static string? Colorize(string? value, Func<string?, string> colorizeFunc)
        => NoColor ? value : colorizeFunc(value);

    public static void WriteError(string? message)
        => WriteLine(Prefix("error:   ", Colorize(message, x => Bold + Red + x + Reset)));

    public static void WriteWarning(string? message)
        => WriteLine(Prefix("warn:    ", Colorize(message, x => Bold + Yellow + x + Reset)));

    public static void WriteInformation(string? message)
        => WriteLine(Prefix("info:    ", message));

    public static void WriteData(string? message)
        => WriteLine(Prefix("data:    ", Colorize(message, x => Bold + Gray + x + Reset)));

    public static void WriteVerbose(string? message)
    {
        if (IsVerbose)
        {
            WriteLine(Prefix("verbose: ", Colorize(message, x => Bold + Black + x + Reset)));
        }
    }

    public static void Write(CompilerError error)
    {
        var builder = new StringBuilder();

        if (!string.IsNullOrEmpty(error.FileName))
        {
            builder.Append(error.FileName);

            if (error.Line > 0)
            {
                builder
                    .Append("(")
                    .Append(error.Line);

                if (error.Column > 0)
                {
                    builder
                        .Append(",")
                        .Append(error.Column);
                }

                builder.Append(")");
            }

            builder.Append(" : ");
        }

        builder
            .Append(error.IsWarning ? "warning" : "error")
            .Append(" ")
            .Append(error.ErrorNumber)
            .Append(": ")
            .AppendLine(error.ErrorText);

        if (error.IsWarning)
        {
            WriteWarning(builder.ToString());
        }
        else
        {
            WriteError(builder.ToString());
        }
    }

    private static string? Prefix(string prefix, string? value)
        => PrefixOutput
            ? value == null
                ? prefix
                : string.Join(
                    Environment.NewLine,
                    value.Split(new[] { Environment.NewLine }, StringSplitOptions.None).Select(l => prefix + l))
            : value;

    private static void WriteLine(string? value)
    {
        if (NoColor)
        {
            Console.WriteLine(value);
        }
        else
        {
            AnsiConsole.WriteLine(value);
        }
    }
}

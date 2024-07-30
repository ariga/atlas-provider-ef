using System;
using System.Linq;
using System.CodeDom.Compiler;
using System.Text;

namespace Atlas.Provider.Core.Executor;

internal static class Reporter
{
    public static bool IsVerbose { get; set; }
    public static bool PrefixOutput { get; set; }

    public static void WriteError(string? message)
        => WriteLine(Prefix("error:   ", message));

    public static void WriteWarning(string? message)
        => WriteLine(Prefix("warn:    ", message));

    public static void WriteInformation(string? message)
        => WriteLine(Prefix("info:    ", message));

    public static void WriteData(string? message)
        => WriteLine(Prefix("data:    ", message));

    public static void WriteVerbose(string? message)
    {
        if (IsVerbose)
        {
            WriteLine(Prefix("verbose: ", message));
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
        Console.WriteLine(value);
    }
}

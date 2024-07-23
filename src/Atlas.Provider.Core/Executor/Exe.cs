/*
Title: Exe
Type: Source Code
Availability: https://github.com/dotnet/efcore/blob/9262a34db65ce52085ded548fc480522051cec2b/src/dotnet-ef/Exe.cs
*/

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Atlas.Provider.Tools;

internal static class Exe
{
    public static int Run(
        string executable,
        IReadOnlyList<string> args,
        string? workingDirectory = null,
        bool interceptOutput = false)
    {
        var arguments = ToArguments(args);

        var startInfo = new ProcessStartInfo
        {
            FileName = executable,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = interceptOutput
        };
        if (workingDirectory != null)
        {
            startInfo.WorkingDirectory = workingDirectory;
        }

        var process = Process.Start(startInfo)!;

        process.WaitForExit();

        return process.ExitCode;
    }

    private static string ToArguments(IReadOnlyList<string> args)
    {
        var builder = new StringBuilder();
        for (var i = 0; i < args.Count; i++)
        {
            if (i != 0)
            {
                builder.Append(' ');
            }

            if (args[i].Length == 0)
            {
                builder.Append("\"\"");

                continue;
            }

            if (args[i].IndexOf(' ') == -1)
            {
                builder.Append(args[i]);

                continue;
            }

            builder.Append('"');

            var pendingBackslashes = 0;
            for (var j = 0; j < args[i].Length; j++)
            {
                switch (args[i][j])
                {
                    case '\"':
                        if (pendingBackslashes != 0)
                        {
                            builder.Append('\\', pendingBackslashes * 2);
                            pendingBackslashes = 0;
                        }

                        builder.Append("\\\"");
                        break;

                    case '\\':
                        pendingBackslashes++;
                        break;

                    default:
                        if (pendingBackslashes != 0)
                        {
                            if (pendingBackslashes == 1)
                            {
                                builder.Append('\\');
                            }
                            else
                            {
                                builder.Append('\\', pendingBackslashes * 2);
                            }

                            pendingBackslashes = 0;
                        }

                        builder.Append(args[i][j]);
                        break;
                }
            }

            if (pendingBackslashes != 0)
            {
                builder.Append('\\', pendingBackslashes * 2);
            }

            builder.Append('"');
        }

        return builder.ToString();
    }
}

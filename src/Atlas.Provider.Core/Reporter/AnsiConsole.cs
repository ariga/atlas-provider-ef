/*
Title: AnsiConsole
Type: Source Code
Availability: https://github.com/dotnet/efcore/blob/70cd92974e09e938a92384e91b5c3179f4773725/src/ef/AnsiConsole.cs
*/

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Atlas.Provider.Core;

internal static class AnsiConsole
{
    public static readonly AnsiTextWriter Out = new(Console.Out);

    public static void WriteLine(string? text)
        => Out.WriteLine(text);
}

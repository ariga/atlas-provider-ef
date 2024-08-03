using System;
using System.CommandLine;
using System.Linq;

namespace Atlas.Provider.Core.Executor;

// The Reporter logs to stderr to separate error messages from SQL output, which goes to stdout.
internal static class Reporter
{
  public static bool IsVerbose { get; set; }
  public static bool PrefixOutput { get; set; }

  public static void WriteError(string? message)
      => Console.Error.WriteLine(Prefix("error:   ", message));

  public static void WriteWarning(string? message)
      => Console.Error.WriteLine(Prefix("warn:    ", message));

  public static void WriteInformation(string? message)
      => Console.Error.WriteLine(Prefix("info:    ", message));

  public static void WriteData(string? message)
      => Console.Error.WriteLine(Prefix("data:    ", message));

  public static void WriteVerbose(string? message)
  {
    if (IsVerbose)
    {
      Console.Error.WriteLine(Prefix("verbose: ", message));
    }
  }

  private static string? Prefix(string prefix, string? value)
      => 
          value == null
              ? prefix
              : string.Join(
                  Environment.NewLine,
                  value.Split(new[] { Environment.NewLine }, StringSplitOptions.None).Select(l => prefix + l));
}

using HuLoopBOT.Services;
using HuLoopBOT.Models;

namespace HuLoopBOT.Utilities;

public static class CommandLineHandler
{
    public static int Execute(string[] args)
    {
        try
        {
            Logger.LogInformation($"Console mode started with {args.Length} arguments");
            Logger.LogVerbose($"Arguments: {string.Join(" ", args)}");

            if (args.Length == 0 || args.Contains("--help") || args.Contains("-h") || args.Contains("/?"))
            {
                ShowHelp();
                return 0;
            }

            var silent = args.Contains("--silent") || args.Contains("-s");
            var exitCode = 0;

            foreach (var arg in args)
            {
                var result = ProcessArgument(arg);

                if (result != null)
                {
                    if (!result.Success)
                    {
                        exitCode = 1;
                        Logger.LogError($"Operation failed: {result.Message}", null);

                        if (!silent)
                        {
                            Console.WriteLine($"ERROR: {result.Message}");
                        }
                    }
                    else
                    {
                        Logger.LogInformation($"Operation succeeded: {result.Message}");

                        if (!silent)
                        {
                            Console.WriteLine($"SUCCESS: {result.Message}");
                        }
                    }
                }
            }

            Logger.LogInformation($"Console mode completed with exit code: {exitCode}");
            return exitCode;
        }
        catch (Exception ex)
        {
            Logger.LogError("Unhandled exception in console mode", ex);
            Console.WriteLine($"CRITICAL ERROR: {ex.Message}");
            return -1;
        }
    }

    private static OperationResult? ProcessArgument(string arg)
    {
        var argLower = arg.ToLowerInvariant();

        Logger.LogVerbose($"Processing argument: {arg}");

        return argLower switch
        {
            "--disable-sleep" or "-ds" => new PowerService().DisableSleep(),
            "--disable-lock" or "-dl" => new LockService().DisableLockScreen(),
            "--disable-rdp-timeout" or "-dr" => new RdpService().PreventTimeout(),
            "--transfer-session" or "-ts" => new SessionTransferService().Transfer(1),
            "--all" or "-a" => ExecuteAll(),
            "--silent" or "-s" => null, // Handled separately
            _ when argLower.StartsWith("--transfer-session=") => ExecuteSessionTransfer(arg),
            _ when argLower.StartsWith("-ts=") => ExecuteSessionTransfer(arg),
            _ => HandleUnknownArgument(arg)
        };
    }

    private static OperationResult ExecuteAll()
    {
        Logger.LogInformation("Executing all operations");

        var results = new List<OperationResult>
        {
            new PowerService().DisableSleep(),
            new LockService().DisableLockScreen(),
            new RdpService().PreventTimeout()
        };

        var allSuccessful = results.All(r => r.Success);
        var message = allSuccessful
    ? "All operations completed successfully"
       : "Some operations failed";

        return allSuccessful
             ? OperationResult.Ok(message)
                 : OperationResult.Fail(message);
    }

    private static OperationResult ExecuteSessionTransfer(string arg)
    {
        var parts = arg.Split('=');
        if (parts.Length == 2 && int.TryParse(parts[1], out int sessionId))
        {
            Logger.LogVerbose($"Transferring to session ID: {sessionId}");
            return new SessionTransferService().Transfer(sessionId);
        }

        return OperationResult.Fail($"Invalid session ID format: {arg}");
    }

    private static OperationResult? HandleUnknownArgument(string arg)
    {
        if (arg.StartsWith("-") || arg.StartsWith("--"))
        {
            var message = $"Unknown argument: {arg}";
            Logger.LogWarning(message);
            Console.WriteLine($"WARNING: {message}");
            Console.WriteLine("Use --help for usage information");
        }

        return null;
    }

    private static void ShowHelp()
    {
        var help = @"
HuLoopBOT - System Configuration Tool
======================================

USAGE:
    HuLoopBOT.exe [OPTIONS]

MODES:
    No arguments    Launch GUI mode (Windows Forms interface)
    With arguments      Run in console/silent mode

OPTIONS:
    --disable-sleep, -ds
  Disable system sleep/standby timeout
    
    --disable-lock, -dl
  Disable Windows lock screen
    
    --disable-rdp-timeout, -dr
        Disable RDP session timeout
    
    --transfer-session[=ID], -ts[=ID]
        Transfer session to console (default ID=1)
  Example: --transfer-session=2
    
    --all, -a
        Execute all operations (sleep, lock, RDP timeout)
    
    --silent, -s
 Run silently without console output (except errors)
    
    --help, -h, /?
        Show this help message

EXAMPLES:
    HuLoopBOT.exe --all
        Execute all operations with console output
    
    HuLoopBOT.exe --disable-sleep --disable-lock --silent
     Disable sleep and lock screen silently
    
    HuLoopBOT.exe -a -s
        Execute all operations silently (for Group Policy)

EXIT CODES:
    0   Success
    1   One or more operations failed
    -1  Critical error

NOTES:
    - Administrator privileges are required
    - Logs are written to Windows Event Viewer (Application Log)
    - File log: C:\ProgramData\HuLoopBOT\log.txt
    - Ideal for Group Policy startup/shutdown scripts
";

        Console.WriteLine(help);
        Logger.LogInformation("Help displayed to user");
    }
}

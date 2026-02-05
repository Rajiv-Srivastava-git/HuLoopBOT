using HuLoopBOT.Services;
using HuLoopBOT.Utilities;

namespace HuLoopBOT;

/// <summary>
/// Command-line interface for service management
/// Run with admin privileges for installation/start/stop operations
/// </summary>
public static class ServiceCLI
{
    public static int Run(string[] args)
    {
        Console.WriteLine();
        Console.WriteLine("========================================");
        Console.WriteLine("  HuLoopBOT Service Manager");
        Console.WriteLine("========================================");
        Console.WriteLine();

        // Check if running with arguments (command-line mode)
        if (args.Length > 0)
        {
            return ExecuteCommand(args);
        }

        // Interactive mode
        return InteractiveMode();
    }

    private static int ExecuteCommand(string[] args)
    {
        var command = args[0].ToLower();

        switch (command)
        {
            case "install":
                {
                    var path = args.Length > 1 ? args[1] : "";
                    var result = ServiceManager.InstallService(path);
                    PrintResult(result);
                    return result.Success ? 0 : 1;
                }

            case "uninstall":
                {
                    var result = ServiceManager.UninstallService();
                    PrintResult(result);
                    return result.Success ? 0 : 1;
                }

            case "start":
                {
                    var result = ServiceManager.StartService();
                    PrintResult(result);
                    return result.Success ? 0 : 1;
                }

            case "stop":
                {
                    var result = ServiceManager.StopService();
                    PrintResult(result);
                    return result.Success ? 0 : 1;
                }

            case "restart":
                {
                    var result = ServiceManager.RestartService();
                    PrintResult(result);
                    return result.Success ? 0 : 1;
                }

            case "status":
                {
                    var status = ServiceManager.GetServiceStatus();
                    PrintStatus(status);
                    return 0;
                }

            case "enable":
                {
                    var result = ServiceManager.EnableService();
                    PrintResult(result);
                    return result.Success ? 0 : 1;
                }

            case "disable":
                {
                    var result = ServiceManager.DisableService();
                    PrintResult(result);
                    return result.Success ? 0 : 1;
                }

            case "build-selfcontained":
                {
                    var outputDir = args.Length > 1 ? args[1] : "publish-self-contained";
                    var result = ServiceManager.BuildSelfContained(outputDir);
                    PrintResult(result);
                    return result.Success ? 0 : 1;
                }

            case "help":
            case "-h":
            case "--help":
                PrintHelp();
                return 0;

            default:
                Console.WriteLine($"Unknown command: {command}");
                Console.WriteLine("Run with 'help' for usage information");
                return 1;
        }
    }

    private static int InteractiveMode()
    {
        while (true)
        {
            // Check admin status
            var isAdmin = ServiceManager.IsRunningAsAdministrator();
            Console.ForegroundColor = isAdmin ? ConsoleColor.Green : ConsoleColor.Yellow;
            Console.WriteLine($"Running as: {(isAdmin ? "Administrator" : "Standard User")}");
            Console.ResetColor();

            if (!isAdmin)
            {
                Console.WriteLine("Note: Install/Start/Stop operations require administrator privileges");
            }
            Console.WriteLine();

            // Show current status
            var status = ServiceManager.GetServiceStatus();
            PrintStatus(status);
            Console.WriteLine();

            // Show menu
            Console.WriteLine("Available Commands:");
            Console.WriteLine("  1. Install Service");
            Console.WriteLine("  2. Uninstall Service");
            Console.WriteLine("  3. Start Service");
            Console.WriteLine("  4. Stop Service");
            Console.WriteLine("  5. Restart Service");
            Console.WriteLine("  6. Enable Monitoring");
            Console.WriteLine("  7. Disable Monitoring");
            Console.WriteLine("  8. Build Self-Contained Package");
            Console.WriteLine("  9. Restart as Administrator");
            Console.WriteLine("  0. Exit");
            Console.WriteLine();
            Console.Write("Select option: ");

            var input = Console.ReadLine();
            Console.WriteLine();

            switch (input)
            {
                case "1":
                    {
                        Console.Write("Enter service executable path (or press Enter for default): ");
                        var path = Console.ReadLine();
                        var result = ServiceManager.InstallService(string.IsNullOrWhiteSpace(path) ? "" : path);
                        PrintResult(result);
                        break;
                    }

                case "2":
                    {
                        var result = ServiceManager.UninstallService();
                        PrintResult(result);
                        break;
                    }

                case "3":
                    {
                        var result = ServiceManager.StartService();
                        PrintResult(result);
                        break;
                    }

                case "4":
                    {
                        var result = ServiceManager.StopService();
                        PrintResult(result);
                        break;
                    }

                case "5":
                    {
                        var result = ServiceManager.RestartService();
                        PrintResult(result);
                        break;
                    }

                case "6":
                    {
                        var result = ServiceManager.EnableService();
                        PrintResult(result);
                        break;
                    }

                case "7":
                    {
                        var result = ServiceManager.DisableService();
                        PrintResult(result);
                        break;
                    }

                case "8":
                    {
                        Console.Write("Enter output directory (or press Enter for default): ");
                        var outputDir = Console.ReadLine();
                        var result = ServiceManager.BuildSelfContained(
                        string.IsNullOrWhiteSpace(outputDir) ? "publish-self-contained" : outputDir);
                        PrintResult(result);
                        break;
                    }

                case "9":
                    {
                        Console.WriteLine("Restarting with administrator privileges...");
                        if (ServiceManager.RestartAsAdministrator())
                        {
                            return 0; // Exit current instance
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("Failed to restart as administrator");
                            Console.ResetColor();
                        }
                        break;
                    }

                case "0":
                    Console.WriteLine("Exiting...");
                    return 0;

                default:
                    Console.WriteLine("Invalid option");
                    break;
            }

            Console.WriteLine();
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
            Console.Clear();
        }
    }

    private static void PrintResult(ServiceOperationResult result)
    {
        Console.ForegroundColor = result.Success ? ConsoleColor.Green : ConsoleColor.Red;
        Console.WriteLine(result.Success ? "✓ Success" : "✗ Failed");
        Console.ResetColor();
        Console.WriteLine(result.Message);

        if (!string.IsNullOrEmpty(result.ErrorDetails))
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"Details: {result.ErrorDetails}");
            Console.ResetColor();
        }
    }

    private static void PrintStatus(ServiceStatus status)
    {
        Console.WriteLine("Service Status:");
        Console.WriteLine($"  Installed: {(status.IsInstalled ? "Yes" : "No")}");

        if (status.IsInstalled)
        {
            Console.ForegroundColor = status.StatusCode == System.ServiceProcess.ServiceControllerStatus.Running
            ? ConsoleColor.Green
                       : ConsoleColor.Yellow;
            Console.WriteLine($"  Status: {status.Status}");
            Console.ResetColor();

            if (!string.IsNullOrEmpty(status.DisplayName))
            {
                Console.WriteLine($"  Display Name: {status.DisplayName}");
            }
        }

        if (!string.IsNullOrEmpty(status.ErrorMessage))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"  Error: {status.ErrorMessage}");
            Console.ResetColor();
        }
    }

    private static void PrintHelp()
    {
        Console.WriteLine("HuLoopBOT Service Manager - Command Line Usage");
        Console.WriteLine();
        Console.WriteLine("Usage: HuLoopBOT.exe [command] [options]");
        Console.WriteLine();
        Console.WriteLine("Commands:");
        Console.WriteLine("  install [path]   Install the service (optional: specify executable path)");
        Console.WriteLine("  uninstall              Uninstall the service");
        Console.WriteLine("  start     Start the service");
        Console.WriteLine("  stop       Stop the service");
        Console.WriteLine("  restart  Restart the service");
        Console.WriteLine("  status    Show service status");
        Console.WriteLine("  enable             Enable service monitoring");
        Console.WriteLine("  disable      Disable service monitoring");
        Console.WriteLine("  build-selfcontained [dir]  Build self-contained package");
        Console.WriteLine("  help      Show this help message");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  HuLoopBOT.exe install");
        Console.WriteLine("  HuLoopBOT.exe start");
        Console.WriteLine("  HuLoopBOT.exe status");
        Console.WriteLine("  HuLoopBOT.exe build-selfcontained");
        Console.WriteLine();
        Console.WriteLine("Note: Install/Start/Stop commands require administrator privileges");
        Console.WriteLine("      Run without arguments for interactive mode");
    }
}

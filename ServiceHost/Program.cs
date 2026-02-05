using HuLoopBOT.Services;
using System.ServiceProcess;

namespace HuLoopBOT.ServiceHost;

/// <summary>
/// Service host entry point for HuLoopBOT RDP Monitoring Service
/// This executable runs as a Windows Service and continues running independently
/// </summary>
internal static class Program
{
    /// <summary>
    /// The main entry point for the Windows Service application.
    /// </summary>
    static void Main(string[] args)
    {
        // If running with /install or /uninstall arguments from command line
        if (args.Length > 0)
        {
            HandleCommandLineArguments(args);
            return;
        }

        // Normal service execution
        ServiceBase[] ServicesToRun;
        ServicesToRun = new ServiceBase[]
        {
            new RdpMonitoringService()
        };
        ServiceBase.Run(ServicesToRun);
    }

    private static void HandleCommandLineArguments(string[] args)
    {
        string command = args[0].ToLower();

        switch (command)
        {
            case "/install":
            case "-install":
                if (RdpMonitoringService.InstallService())
                {
                    Console.WriteLine("Service installed successfully.");
                }
                else
                {
                    Console.WriteLine("Failed to install service.");
                }
                break;

            case "/uninstall":
            case "-uninstall":
                if (RdpMonitoringService.UninstallService())
                {
                    Console.WriteLine("Service uninstalled successfully.");
                }
                else
                {
                    Console.WriteLine("Failed to uninstall service.");
                }
                break;

            default:
                Console.WriteLine("Unknown command. Valid commands: /install, /uninstall");
                break;
        }
    }
}

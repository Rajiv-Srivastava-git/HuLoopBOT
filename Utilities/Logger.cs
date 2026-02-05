using System.Diagnostics;
using System.Text;

namespace HuLoopBOT.Utilities;

/// <summary>
/// Enhanced logging utility with separate event sources and rotating log files
/// </summary>
public static class Logger
{
    // Base paths for different log types
    private static readonly string LogDirectory = @"C:\ProgramData\HuLoopBOT\Logs";
    private static string _currentLogFile = string.Empty;
    private static readonly object _logFileLock = new object();

    // Event sources for different components
    private const string MainEventSource = "HuLoopBOT";
    private const string RdpMonitorEventSource = "HuLoopBOT_RDP_Monitor";
    private const string EventLogName = "Application";

    private static bool _mainEventSourceChecked = false;
    private static bool _mainEventSourceAvailable = false;
    private static bool _rdpEventSourceChecked = false;
    private static bool _rdpEventSourceAvailable = false;

    // Configuration
    public static bool VerboseLoggingEnabled { get; set; } = true;
    public static int MaxLogFileSizeMB { get; set; } = 10;
    public static int MaxLogFileAgeDays { get; set; } = 30;

    static Logger()
    {
        try
        {
            InitializeLogging();
        }
        catch (Exception ex)
        {
            // Fallback logging if initialization fails
            try
            {
                File.AppendAllText(Path.Combine(LogDirectory, "logger-error.txt"),
        $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}: Logger initialization failed: {ex.Message}\n");
            }
            catch
            {
                // Ultimate fallback - do nothing
            }
        }
    }

    /// <summary>
    /// Initialize logging system - create directories and log files
    /// </summary>
    private static void InitializeLogging()
    {
        try
        {
            // Create log directory if it doesn't exist
            if (!Directory.Exists(LogDirectory))
            {
                Directory.CreateDirectory(LogDirectory);
            }

            // Create a new log file for this run
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string processName = Process.GetCurrentProcess().ProcessName;
            _currentLogFile = Path.Combine(LogDirectory, $"{processName}_{timestamp}.log");

            // Write initial header
            lock (_logFileLock)
            {
                var header = new StringBuilder();
                header.AppendLine("========================================");
                header.AppendLine($"Log File Created: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                header.AppendLine($"Application: {processName}");
                header.AppendLine($"Process ID: {Process.GetCurrentProcess().Id}");
                header.AppendLine($"Machine Name: {Environment.MachineName}");
                header.AppendLine($"OS Version: {Environment.OSVersion}");
                header.AppendLine($".NET Version: {Environment.Version}");
                header.AppendLine($"User: {Environment.UserName}");
                header.AppendLine($"Log File: {_currentLogFile}");
                header.AppendLine("========================================");
                header.AppendLine();

                File.WriteAllText(_currentLogFile, header.ToString());
            }

            // Clean up old log files
            CleanupOldLogFiles();

            // Initialize event sources asynchronously
            Task.Run(() => EnsureMainEventSource());
            Task.Run(() => EnsureRdpEventSource());

            LogInformation("Logger initialized successfully");
        }
        catch (Exception ex)
        {
            // Log to fallback location
            try
            {
                string fallbackLog = Path.Combine(Path.GetTempPath(), "HuLoopBOT_fallback.log");
                File.AppendAllText(fallbackLog,
                 $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}: Logger initialization failed: {ex.Message}\n");
            }
            catch
            {
                // Nothing we can do
            }
        }
    }

    /// <summary>
    /// Ensure main event source exists
    /// </summary>
    private static void EnsureMainEventSource()
    {
  if (_mainEventSourceChecked)
            return;

 _mainEventSourceChecked = true;

        try
        {
      // Check if running as administrator - EventLog.CreateEventSource requires admin rights
      bool isAdmin = false;
try
        {
  using var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
    var principal = new System.Security.Principal.WindowsPrincipal(identity);
                isAdmin = principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
            }
    catch
    {
         isAdmin = false;
     }

         if (!isAdmin)
     {
         LogToFile($"[SYSTEM] Skipping EventLog source creation (not running as admin)");
     return;
        }

          if (!EventLog.SourceExists(MainEventSource))
   {
          EventLog.CreateEventSource(MainEventSource, EventLogName);
                LogToFile($"[SYSTEM] Created Event Source: {MainEventSource}");
         }
  _mainEventSourceAvailable = true;
        }
        catch (Exception ex)
        {
     LogToFile($"[SYSTEM] Failed to create main event source: {ex.Message}");
        }
    }

    /// <summary>
    /// Ensure RDP Monitor event source exists
    /// </summary>
    private static void EnsureRdpEventSource()
    {
        if (_rdpEventSourceChecked)
      return;

        _rdpEventSourceChecked = true;

        try
        {
    // Check if running as administrator
            bool isAdmin = false;
    try
     {
       using var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
     var principal = new System.Security.Principal.WindowsPrincipal(identity);
          isAdmin = principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
       }
            catch
            {
          isAdmin = false;
      }

    if (!isAdmin)
            {
            LogToFile($"[SYSTEM] Skipping EventLog source creation (not running as admin)");
         return;
      }

      if (!EventLog.SourceExists(RdpMonitorEventSource))
            {
  EventLog.CreateEventSource(RdpMonitorEventSource, EventLogName);
       LogToFile($"[SYSTEM] Created Event Source: {RdpMonitorEventSource}");
            }
 _rdpEventSourceAvailable = true;
        }
        catch (Exception ex)
        {
     LogToFile($"[SYSTEM] Failed to create RDP Monitor event source: {ex.Message}");
        }
    }

    /// <summary>
    /// Clean up old log files based on age and size
    /// </summary>
    private static void CleanupOldLogFiles()
    {
        try
        {
            if (!Directory.Exists(LogDirectory))
                return;

            var cutoffDate = DateTime.Now.AddDays(-MaxLogFileAgeDays);
            var logFiles = Directory.GetFiles(LogDirectory, "*.log");

            int deletedCount = 0;
            long freedSpace = 0;

            foreach (var file in logFiles)
            {
                try
                {
                    var fileInfo = new FileInfo(file);

                    // Delete if too old or too large
                    if (fileInfo.LastWriteTime < cutoffDate ||
                           fileInfo.Length > MaxLogFileSizeMB * 1024 * 1024)
                    {
                        freedSpace += fileInfo.Length;
                        File.Delete(file);
                        deletedCount++;
                    }
                }
                catch
                {
                    // Continue with other files
                }
            }

            if (deletedCount > 0)
            {
                LogToFile($"[SYSTEM] Cleaned up {deletedCount} old log files, freed {freedSpace / 1024.0:F2} KB");
            }
        }
        catch (Exception ex)
        {
            LogToFile($"[SYSTEM] Error during log cleanup: {ex.Message}");
        }
    }

    #region Main Logging Methods

    /// <summary>
    /// Log informational message
    /// </summary>
    public static void Log(string message)
    {
        LogInformation(message);
    }

    /// <summary>
    /// Log informational message with context
    /// </summary>
    public static void LogInformation(string message, string context = "")
    {
        try
        {
            string formattedMessage = string.IsNullOrEmpty(context)
   ? message
      : $"[{context}] {message}";

            LogToFile($"[INFO] {formattedMessage}");
            LogToEventViewer(MainEventSource, formattedMessage, EventLogEntryType.Information, 1000);
        }
        catch (Exception ex)
        {
            FallbackLog($"LogInformation failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Log warning message with context
    /// </summary>
    public static void LogWarning(string message, string context = "")
    {
        try
        {
            string formattedMessage = string.IsNullOrEmpty(context)
               ? message
            : $"[{context}] {message}";

            LogToFile($"[WARNING] {formattedMessage}");
            LogToEventViewer(MainEventSource, formattedMessage, EventLogEntryType.Warning, 2000);
        }
        catch (Exception ex)
        {
            FallbackLog($"LogWarning failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Log error message with optional exception
    /// </summary>
    public static void LogError(string message, Exception? exception = null, string context = "")
    {
        try
        {
            var sb = new StringBuilder();

            if (!string.IsNullOrEmpty(context))
            {
                sb.Append($"[{context}] ");
            }

            sb.Append(message);

            if (exception != null)
            {
                sb.AppendLine();
                sb.AppendLine($"Exception Type: {exception.GetType().FullName}");
                sb.AppendLine($"Message: {exception.Message}");
                sb.AppendLine($"Stack Trace:");
                sb.AppendLine(exception.StackTrace);

                // Log inner exceptions
                var innerEx = exception.InnerException;
                int level = 1;
                while (innerEx != null && level <= 3)
                {
                    sb.AppendLine();
                    sb.AppendLine($"Inner Exception (Level {level}):");
                    sb.AppendLine($"Type: {innerEx.GetType().FullName}");
                    sb.AppendLine($"Message: {innerEx.Message}");
                    sb.AppendLine($"Stack Trace:");
                    sb.AppendLine(innerEx.StackTrace);

                    innerEx = innerEx.InnerException;
                    level++;
                }
            }

            string fullMessage = sb.ToString();
            LogToFile($"[ERROR] {fullMessage}");
            LogToEventViewer(MainEventSource, fullMessage, EventLogEntryType.Error, 3000);
        }
        catch (Exception ex)
        {
            FallbackLog($"LogError failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Log verbose/debug message
    /// </summary>
    public static void LogVerbose(string message, string context = "")
    {
        if (!VerboseLoggingEnabled)
            return;

        try
        {
            string formattedMessage = string.IsNullOrEmpty(context)
                          ? message
                          : $"[{context}] {message}";

            LogToFile($"[VERBOSE] {formattedMessage}");
            LogToEventViewer(MainEventSource, formattedMessage, EventLogEntryType.Information, 1001);
        }
        catch (Exception ex)
        {
            FallbackLog($"LogVerbose failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Log debug message
    /// </summary>
    public static void LogDebug(string message, string context = "")
    {
        if (!VerboseLoggingEnabled)
            return;

        try
        {
            string formattedMessage = string.IsNullOrEmpty(context)
                        ? message
             : $"[{context}] {message}";

            LogToFile($"[DEBUG] {formattedMessage}");
        }
        catch (Exception ex)
        {
            FallbackLog($"LogDebug failed: {ex.Message}");
        }
    }

    #endregion

    #region RDP Monitor Specific Logging

    /// <summary>
    /// Log RDP Monitor informational message
    /// </summary>
    public static void LogRdpInfo(string message, int? sessionId = null)
    {
        try
        {
            string formattedMessage = sessionId.HasValue
           ? $"[Session {sessionId}] {message}"
             : message;

            LogToFile($"[RDP-INFO] {formattedMessage}");
            LogToEventViewer(RdpMonitorEventSource, formattedMessage, EventLogEntryType.Information, 5000);
        }
        catch (Exception ex)
        {
            FallbackLog($"LogRdpInfo failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Log RDP Monitor warning
    /// </summary>
    public static void LogRdpWarning(string message, int? sessionId = null)
    {
        try
        {
            string formattedMessage = sessionId.HasValue
            ? $"[Session {sessionId}] {message}"
                 : message;

            LogToFile($"[RDP-WARNING] {formattedMessage}");
            LogToEventViewer(RdpMonitorEventSource, formattedMessage, EventLogEntryType.Warning, 5001);
        }
        catch (Exception ex)
        {
            FallbackLog($"LogRdpWarning failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Log RDP Monitor error
    /// </summary>
    public static void LogRdpError(string message, Exception? exception = null, int? sessionId = null)
    {
        try
        {
            var sb = new StringBuilder();

            if (sessionId.HasValue)
            {
                sb.Append($"[Session {sessionId}] ");
            }

            sb.Append(message);

            if (exception != null)
            {
                sb.AppendLine();
                sb.AppendLine($"Exception: {exception.GetType().FullName}");
                sb.AppendLine($"Message: {exception.Message}");
                sb.AppendLine($"Stack Trace:");
                sb.AppendLine(exception.StackTrace);
            }

            string fullMessage = sb.ToString();
            LogToFile($"[RDP-ERROR] {fullMessage}");
            LogToEventViewer(RdpMonitorEventSource, fullMessage, EventLogEntryType.Error, 5002);
        }
        catch (Exception ex)
        {
            FallbackLog($"LogRdpError failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Log RDP session event with details
    /// </summary>
    public static void LogRdpSessionEvent(string eventType, int sessionId, string details = "")
    {
        try
        {
            var sb = new StringBuilder();
            sb.AppendLine($"RDP Session Event: {eventType}");
            sb.AppendLine($"Session ID: {sessionId}");
            sb.AppendLine($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");

            if (!string.IsNullOrEmpty(details))
            {
                sb.AppendLine($"Details: {details}");
            }

            string message = sb.ToString();
            LogToFile($"[RDP-EVENT] {message}");
            LogToEventViewer(RdpMonitorEventSource, message, EventLogEntryType.Information, 5003);
        }
        catch (Exception ex)
        {
            FallbackLog($"LogRdpSessionEvent failed: {ex.Message}");
        }
    }

    #endregion

    #region Private Helper Methods

    /// <summary>
    /// Write message to log file
    /// </summary>
    private static void LogToFile(string message)
    {
        try
        {
            lock (_logFileLock)
            {
                if (string.IsNullOrEmpty(_currentLogFile))
                {
                    InitializeLogging();
                }

                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                string logEntry = $"{timestamp} [{Thread.CurrentThread.ManagedThreadId:D4}] {message}\n";

                File.AppendAllText(_currentLogFile, logEntry);

                // Check file size and rotate if needed
                var fileInfo = new FileInfo(_currentLogFile);
                if (fileInfo.Length > MaxLogFileSizeMB * 1024 * 1024)
                {
                    RotateLogFile();
                }
            }
        }
        catch
        {
            // Silently fail - we're the logger
        }
    }

    /// <summary>
    /// Rotate log file when it gets too large
    /// </summary>
    private static void RotateLogFile()
    {
        try
        {
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string processName = Process.GetCurrentProcess().ProcessName;
            _currentLogFile = Path.Combine(LogDirectory, $"{processName}_{timestamp}.log");

            LogToFile("[SYSTEM] Log file rotated due to size limit");
        }
        catch
        {
            // Continue with current file
        }
    }

    /// <summary>
    /// Write message to Event Viewer
    /// </summary>
    private static void LogToEventViewer(string source, string message, EventLogEntryType entryType, int eventId)
    {
        // Don't block if event source isn't available
        Task.Run(() =>
   {
       try
       {
           bool sourceAvailable = source == RdpMonitorEventSource
               ? _rdpEventSourceAvailable
                  : _mainEventSourceAvailable;

           if (!sourceAvailable)
           {
               // Try one more time to create it
               if (source == RdpMonitorEventSource)
                   EnsureRdpEventSource();
               else
                   EnsureMainEventSource();
           }

           if (EventLog.SourceExists(source))
           {
               // Truncate message if too long (Event Viewer has limits)
               string truncatedMessage = message.Length > 31000
                    ? message.Substring(0, 31000) + "... (truncated)"
              : message;

               EventLog.WriteEntry(source, truncatedMessage, entryType, eventId);
           }
       }
       catch
       {
           // Silently fail - Event Viewer logging is optional
       }
   });
    }

    /// <summary>
    /// Fallback logging when main logging fails
    /// </summary>
    private static void FallbackLog(string message)
    {
        try
        {
            string fallbackLog = Path.Combine(Path.GetTempPath(), "HuLoopBOT_fallback.log");
            File.AppendAllText(fallbackLog,
                  $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}: {message}\n");
        }
        catch
        {
            // Nothing we can do
        }
    }

    #endregion

    #region Public Utility Methods

    /// <summary>
    /// Get current log file path
    /// </summary>
    public static string GetCurrentLogFile()
    {
        return _currentLogFile;
    }

    /// <summary>
    /// Get all log files in directory
    /// </summary>
    public static string[] GetAllLogFiles()
    {
        try
        {
            if (!Directory.Exists(LogDirectory))
                return Array.Empty<string>();

            return Directory.GetFiles(LogDirectory, "*.log")
          .OrderByDescending(f => new FileInfo(f).LastWriteTime)
                  .ToArray();
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    /// <summary>
    /// Force cleanup of old log files
    /// </summary>
    public static void ForceCleanup()
    {
        CleanupOldLogFiles();
    }

    #endregion
}

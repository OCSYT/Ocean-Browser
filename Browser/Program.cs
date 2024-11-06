using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;

namespace Browser
{
    public class Args
    {
        public string UserDataPath { get; set; }
        public string StartupUrl { get; set; }
    }

    internal static class Program
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern int AllocConsole();

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AttachConsole(uint dwProcessId);

        private const uint ATTACH_PARENT_PROCESS = 0xFFFFFFFF;

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [STAThread]
        static void Main(string[] args)
        {
            //AllocConsole();

            // Attach to the parent console if it exists
            AttachConsole(ATTACH_PARENT_PROCESS);

            // Redirect standard output and error to the console
            StreamWriter consoleOutput = new StreamWriter(Console.OpenStandardOutput())
            {
                AutoFlush = true,
            };
            Console.SetOut(consoleOutput);
            Console.SetError(consoleOutput);

            // Application specific code
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Create a temporary directory for user data
            string tempUserDataFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempUserDataFolder);
            //Console.WriteLine($"TEMP user data folder: {tempUserDataFolder}");

            var browser = new Browser();
            browser.Run(args, tempUserDataFolder);

            Application.ThreadException += (sender, e) =>
            {
                Console.WriteLine($"Unhandled exception: {e.Exception.Message}");
            };

            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                Console.WriteLine($"Unhandled exception: {e.ExceptionObject}");
            };

            // Clean up the temporary directory after the PageManager is closed
            Application.ApplicationExit += (sender, e) =>
            {
                Console.WriteLine("Application is exiting. Starting cleanup...");
                CleanupTempDirectoryAsync(tempUserDataFolder).Wait(); // Synchronously wait for cleanup
            };
        }

        private static async Task CleanupTempDirectoryAsync(string tempUserDataFolder)
        {
            try
            {
                if (Directory.Exists(tempUserDataFolder))
                {
                    Console.WriteLine("Deleting temporary directory...");
                    try
                    {
                        Directory.Delete(tempUserDataFolder, true); // Delete the temp directory and its contents
                        Console.WriteLine("Temporary directory deleted.");

                        // Wait for 2 seconds after deleting
                        await Task.Delay(2000);
                    }
                    catch (IOException ioEx)
                    {
                        Console.WriteLine($"Error during deletion: {ioEx.Message}");
                        Console.WriteLine("Retrying deletion after 2 seconds...");
                        await Task.Delay(2000);
                        try
                        {
                            Directory.Delete(tempUserDataFolder, true); // Retry deletion
                            Console.WriteLine("Temporary directory deleted after retry.");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Unexpected error during deletion: {ex.Message}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Unexpected error during deletion: {ex.Message}");
                    }
                }
                else
                {
                    Console.WriteLine("Temporary directory does not exist.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during cleanup: {ex.Message}");
            }
        }
    }

    public class Browser
    {
        private PageManager PageManager;
        private string BrowserName = "Ocean";
        private string BrowserVersion = "1.0.0";

        public void Run(string[] args, string tempUserDataFolder)
        {
            var parsedArgs = new Args();

            // Parse command-line arguments
            foreach (string arg in args)
            {
                if (arg.StartsWith("/userDataPath=", StringComparison.OrdinalIgnoreCase))
                {
                    parsedArgs.UserDataPath = arg.Substring("/userDataPath=".Length);
                }
                else if (Uri.IsWellFormedUriString(arg, UriKind.Absolute))
                {
                    parsedArgs.StartupUrl = arg; // Treat it as a URL if valid
                }
            }

            // Pass Args instance to PageManager
            PageManager = new PageManager(
                BrowserName,
                BrowserVersion,
                parsedArgs,
                tempUserDataFolder
            );
            Application.Run(PageManager);
        }
    }
}

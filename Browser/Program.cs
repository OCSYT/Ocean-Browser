using Microsoft.Win32;
using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Browser
{
    internal static class Program
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern int AllocConsole();
        [STAThread]
        static void Main()
        {
            //AllocConsole();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            var browser = new Browser();
            browser.Run();
        }
    }

    public class Browser
    {
        private PageManager PageManager;
        private string BrowserName = "Ocean";

        public void Run()
        {
            PageManager = new PageManager(BrowserName);
            Application.Run(PageManager);
        }
    }
}

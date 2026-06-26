using System;
using System.Diagnostics;
using System.Security.Principal;
using System.Threading;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using WinRT;

namespace FeatureTool
{
    public static class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            if (!IsRunningAsAdmin())
            {
                RestartAsAdmin();
                return;
            }

            WinRT.ComWrappersSupport.InitializeComWrappers();
            Application.Start((p) =>
            {
                var context = new DispatcherQueueSynchronizationContext(
                    DispatcherQueue.GetForCurrentThread());
                SynchronizationContext.SetSynchronizationContext(context);
                _ = new App();
            });
        }

        private static bool IsRunningAsAdmin()
        {
            try
            {
                using var identity = WindowsIdentity.GetCurrent();
                return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }

        private static void RestartAsAdmin()
        {
            var exePath = Environment.ProcessPath;
            if (string.IsNullOrEmpty(exePath)) return;

            var psi = new ProcessStartInfo
            {
                FileName = exePath,
                UseShellExecute = true,
                Verb = "runas",
            };

            try
            {
                Process.Start(psi);
            }
            catch
            {
            }
        }
    }
}

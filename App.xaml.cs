using System;
using System.Windows;
using System.Security.Principal;
using System.Diagnostics;
using System.Reflection;
using System.IO;

namespace unity_theme_switcher
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            AdminRelauncher();
            ExtractAllResources();

            Properties.Add("CatPath", string.Concat(Path.GetTempPath(), "cat.exe"));
            Properties.Add("DDPath", string.Concat(Path.GetTempPath(), "dd.exe"));
            Properties.Add("PrintfPath", string.Concat(Path.GetTempPath(), "printf.exe"));
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            RemoveAllResources();
        }

        private void AdminRelauncher()
        {
            if (!IsRunAsAdmin())
            {
                ProcessStartInfo proc = new ProcessStartInfo();
                proc.UseShellExecute = true;
                proc.WorkingDirectory = Environment.CurrentDirectory;
                proc.FileName = Assembly.GetEntryAssembly().CodeBase;

                proc.Verb = "runas";

                try
                {
                    Process.Start(proc);
                    Application.Current.Shutdown();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("This program must be run as an administrator! \n\n" + ex.ToString());
                }
            }
        }

        private bool IsRunAsAdmin()
        {
            WindowsIdentity id = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(id);

            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        private void ExtractResource(string embeddedFileName, string destinationPath)
        {
            Assembly currentAssembly = Assembly.GetExecutingAssembly();
            string[] arrResources = currentAssembly.GetManifestResourceNames();
            foreach (string resourceName in arrResources)
            {
                if (resourceName.ToUpper().EndsWith(embeddedFileName.ToUpper()))
                {
                    Stream resourceToSave = currentAssembly.GetManifestResourceStream(resourceName);
                    string file = string.Concat(destinationPath, embeddedFileName);
                    File.Create(string.Concat(destinationPath, embeddedFileName)).Close();
                    var output = File.OpenWrite(file);
                    resourceToSave.CopyTo(output);
                    resourceToSave.Close();
                }
            }
        }

        private void ExtractAllResources()
        {
            string path = Path.GetTempPath();

            ExtractResource("cat.exe", path);
            ExtractResource("dd.exe", path);
            ExtractResource("printf.exe", path);
        }

        private void RemoveAllResources()
        {
            string path = Path.GetTempPath();

            File.Delete(string.Concat(path, "cat.exe"));
            File.Delete(string.Concat(path, "dd.exe"));
            File.Delete(string.Concat(path, "printf.exe"));
        }
    }
}

using Microsoft.WindowsAPICodePack.Dialogs;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace unity_theme_switcher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private enum Theme
        {
            NotSupported,
            Dark,
            Light
        }

        string exePath;
        string unityVersion;
        Theme currentTheme;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Select_Executable(object sender, RoutedEventArgs e)
        {
            using (var dialog = new CommonOpenFileDialog())
            {
                dialog.IsFolderPicker = false;

                CommonFileDialogResult result = dialog.ShowDialog(this);

                if (result == CommonFileDialogResult.Ok)
                {
                    // Minimal protection
                    if(!dialog.FileName.ToLower().Contains("unity.exe"))
                    {
                        MessageBox.Show("This does not seem to be a Unity executable.", "Unity Theme Switcher", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    else if(IsUnityRunning())
                    {
                        MessageBox.Show("Please shutdown your Unity Editor before proceeding.", "Unity Theme Switcher", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }

                    exePath = dialog.FileName;

                    if (!string.IsNullOrEmpty(exePath))
                    {                        
                        unityVersion = FileVersionInfo.GetVersionInfo(exePath).ProductVersion;

                        if (!string.IsNullOrEmpty(unityVersion))
                        {
                            DetectedVersionText.IsEnabled = true;
                            UnityVersion.Text = unityVersion;

                            currentTheme = GetCurrentTheme();

                            SelectedFileText.IsEnabled = true;
                            FolderLabel.Text = exePath;

                            SwitchBox.IsEnabled = true;
                            RefreshThemeUI();
                            Toggle.IsEnabled = true;
                            Toggle.FontSize = 16;
                        }
                    }
                }
            }
        }

        private void Switch_Theme(object sender, RoutedEventArgs e)
        {
            if (unityVersion.StartsWith("2019.2") || unityVersion.StartsWith("2019.3") || unityVersion.StartsWith("2020.1"))
                Switch_Theme_Internal("11366617", "u", "t");
            if (unityVersion.StartsWith("2019.1"))
                Switch_Theme_Internal("16519510", "t", "u");
            else if (unityVersion.StartsWith("2018"))
                Switch_Theme_Internal("19340416", "u", "t");
        }

        private void Switch_Theme_Internal(string offset, string lightVal, string darkVal)
        {
            if (IsUnityRunning())
            {
                MessageBox.Show("Please shutdown your Unity Editor before proceeding.", "Unity Theme Switcher", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            BackupExecutable();

            string newByte = currentTheme == Theme.Dark ? lightVal : darkVal;
            string cmd = App.Current.Properties["PrintfPath"] + " " + newByte + " | " + App.Current.Properties["DDPath"] + " of=\"" + exePath + "\" bs=1 seek=" + offset + " count=1 conv=notrunc";
            Process p = CreateConsoleProcess(cmd);
            p.Start();
            p.WaitForExit();

            currentTheme = (Theme)InverseTheme;

            RefreshThemeUI();
            MessageBox.Show("Theme switched!\nA backup Unity.exe.bak file was created in the same directory.\nYou can restore your editor by renaming the backup to Unity.exe if needed.", "Unity Theme Switcher", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private bool IsUnityRunning()
        {
            Process[] pname = Process.GetProcessesByName("Unity.exe");
            if (pname.Length == 0)
                return false;
            return true;
        }

        private void BackupExecutable()
        {
            string backupPath = exePath + ".BAK";
            bool shouldOverwrite = File.Exists(backupPath);
            File.Copy(exePath, exePath + ".BAK", shouldOverwrite);
        }

        // True will be light theme, false will be dark theme
        // 1 light, 0 absence of light
        private Theme GetCurrentTheme()
        {
            if (unityVersion.StartsWith("2019.2") || unityVersion.StartsWith("2019.3") || unityVersion.StartsWith("2020.1"))
                return GetCurrentTheme_Internal("11366617", "t");
            else if (unityVersion.StartsWith("2019.1"))
                return GetCurrentTheme_Internal("16519510", "u");
            else if (unityVersion.StartsWith("2018"))
                return GetCurrentTheme_Internal("19340416", "t");
            else
                return Theme.NotSupported;
        }

        private Theme GetCurrentTheme_Internal(string offset, string darkVal)
        {
            string fileName = GenerateUniqueLocalFilename();

            string cmd = App.Current.Properties["DDPath"] + " skip=" + offset + " count=1 if=\"" + exePath + "\" of=\"" + fileName + "\" bs=1";
            Process p = CreateConsoleProcess(cmd);
            p.Start();
            p.WaitForExit();

            cmd = App.Current.Properties["CatPath"] + " \"" + fileName + "\"";
            p = CreateConsoleProcess(cmd, true);
            p.Start();
            p.WaitForExit();

            File.Delete(fileName);

            string stringVal = string.Empty;

            // There will only be one line with one character
            // 1 byte contains 1 character
            while (!p.StandardOutput.EndOfStream)
            {
                stringVal = p.StandardOutput.ReadLine();
            }

            if (stringVal == darkVal)
                return Theme.Dark;
            else
                return Theme.Light;
        }

        private void RefreshThemeUI()
        {
            switch (currentTheme)
            {
                case Theme.NotSupported:
                    MessageBox.Show("Your version of Unity currently isn't supported by this tool.", "Unity Theme Switcher", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                case Theme.Dark:
                    ThemeBackground.Background = System.Windows.Media.Brushes.Black;
                    break;
                case Theme.Light:
                    ThemeBackground.Background = System.Windows.Media.Brushes.White;
                    break;
                default:
                    break;
            }

            Toggle.Content = string.Concat("Switch to ", System.Enum.GetName(typeof(Theme), InverseTheme));
        }

        private Theme InverseTheme
        {
            get
            {
                if (currentTheme == Theme.NotSupported)
                    return Theme.NotSupported;
                else if (currentTheme == Theme.Light)
                    return Theme.Dark;
                else
                    return Theme.Light;
            }
        }

        private string GenerateUniqueLocalFilename()
        {
            System.Random random = new System.Random();
            int fileName = random.Next();
            string path = Path.GetTempPath();

            while (File.Exists(string.Concat(path, fileName)))
            {
                fileName = random.Next();
            }

            return string.Concat(path, fileName);
        }

        private Process CreateConsoleProcess(string command, bool redirectStandardOutput = false, bool enableRaisingEvents = false, System.EventHandler callback = null)
        {
            string baseCommand = "/C ";
            string fullCommand = baseCommand + command;

            Process proc = new Process();
            proc.EnableRaisingEvents = enableRaisingEvents;

            proc.StartInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = fullCommand,
                UseShellExecute = false,
                RedirectStandardOutput = redirectStandardOutput,
                CreateNoWindow = true
            };

            if (enableRaisingEvents && callback != null)
            {
                proc.Exited += callback;
            }

            return proc;
        }
    }
}

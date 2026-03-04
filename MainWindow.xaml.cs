using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.Diagnostics;
using Path = System.IO.Path;

namespace UE57AndroidManager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
        private async void DownloadAndroidStudio_Click(object sender, RoutedEventArgs e)
        {
            string url = "https://redirector.gvt1.com/edgedl/android/studio/install/2024.1.2.0/android-studio-2024.1.2.0-windows.exe";
            string filePath = @"C:\Temp\android-studio-koala.exe";

            using (var client = new HttpClient())
            {
                    var data = await client.GetByteArrayAsync(url);
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                File.WriteAllBytes(filePath, data);
            }

            OutputBox.AppendText("Android Studio downloaded.\n");
        }

        private async void VerifyJDK_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = await RunProcessAsync("java", "-version", null,
                    s => Dispatcher.Invoke(() => OutputBox.AppendText(s + "\n")),
                    s => Dispatcher.Invoke(() => OutputBox.AppendText(s + "\n")));
                // java writes version to stderr; after streaming, still report detection
                var output = string.IsNullOrWhiteSpace(result.StandardError) ? result.StandardOutput : result.StandardError;
                if (!string.IsNullOrWhiteSpace(output))
                {
                    OutputBox.AppendText("Java detected: " + output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)[0] + "\n");
                }
                else
                {
                    OutputBox.AppendText("Java not found. Please install OpenJDK 21 or set JAVA_HOME.\n");
                }
            }
            catch (Exception ex)
            {
                OutputBox.AppendText("Error verifying JDK: " + ex.Message + "\n");
            }
        }

        private async void InstallSDK_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // locate Android SDK root
                string sdkRoot = Environment.GetEnvironmentVariable("ANDROID_SDK_ROOT") ?? Environment.GetEnvironmentVariable("ANDROID_HOME");
                if (string.IsNullOrWhiteSpace(sdkRoot))
                {
                    var user = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                    var defaultPath = Path.Combine(user, "AppData", "Local", "Android", "Sdk");
                    if (Directory.Exists(defaultPath)) sdkRoot = defaultPath;
                }

                if (string.IsNullOrWhiteSpace(sdkRoot) || !Directory.Exists(sdkRoot))
                {
                    OutputBox.AppendText("Android SDK root not found. Please install Android SDK or set ANDROID_SDK_ROOT.\n");
                    return;
                }

                // find sdkmanager
                string[] possibleSdkManager = new[] {
                    Path.Combine(sdkRoot, "cmdline-tools", "latest", "bin", "sdkmanager.bat"),
                    Path.Combine(sdkRoot, "cmdline-tools", "latest", "bin", "sdkmanager"),
                    Path.Combine(sdkRoot, "tools", "bin", "sdkmanager.bat"),
                    Path.Combine(sdkRoot, "tools", "bin", "sdkmanager")
                };

                string sdkmanager = possibleSdkManager.FirstOrDefault(File.Exists);
                if (sdkmanager == null)
                {
                    OutputBox.AppendText("sdkmanager not found in SDK root. Please install Android SDK command-line tools.\n");
                    return;
                }

                OutputBox.AppendText("Using SDK root: " + sdkRoot + "\n");

                // uninstall higher platforms than 35
                var platformsDir = Path.Combine(sdkRoot, "platforms");
                if (Directory.Exists(platformsDir))
                {
                    var dirs = Directory.GetDirectories(platformsDir)
                        .Select(Path.GetFileName)
                        .Where(n => n != null && n.StartsWith("android-"));
                    foreach (var d in dirs)
                    {
                        int api;
                        if (int.TryParse(d.Substring("android-".Length), out api) && api > 35)
                        {
                            OutputBox.AppendText($"Uninstalling higher platform {d}...\n");
                            await RunProcessAsync(sdkmanager, $"--uninstall \"platforms;{d}\"", null,
                                s => Dispatcher.Invoke(() => OutputBox.AppendText(s + "\n")),
                                s => Dispatcher.Invoke(() => OutputBox.AppendText(s + "\n")));
                        }
                    }
                }

                // packages to ensure installed (subset)
                var packages = new[] {
                    "cmdline-tools;latest",
                    "platforms;android-35",
                    "build-tools;35.0.1",
                    "ndk;27.3.13750724"
                };

                var args = string.Join(" ", packages.Select(p => "\"" + p + "\""));
                OutputBox.AppendText("Installing required packages: " + string.Join(", ", packages) + "\n");

                // Ensure sdkmanager runs with proper env
                var result = await RunProcessAsync(sdkmanager, "--sdk_root=\"" + sdkRoot + "\" --install " + args, null,
                    s => Dispatcher.Invoke(() => OutputBox.AppendText(s + "\n")),
                    s => Dispatcher.Invoke(() => OutputBox.AppendText(s + "\n")));
                OutputBox.AppendText("sdkmanager finished.\n");
            }
            catch (Exception ex)
            {
                OutputBox.AppendText("Error installing SDK/NDK: " + ex.Message + "\n");
            }
        }

        private async void VerifyUE_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string uePath = @"C:\Program Files\Epic Games\UE_5.7\Engine\Extras\Android";
                if (!Directory.Exists(uePath))
                {
                    OutputBox.AppendText("Unreal Engine Android extras folder not found at: " + uePath + "\n");
                    return;
                }

                var expected = new[] { "SetupAndroid.bat", "SetupAndroid.command", "SetupAndroid.sh" };
                foreach (var f in expected)
                {
                    var p = Path.Combine(uePath, f);
                    OutputBox.AppendText(f + ": " + (File.Exists(p) ? "Found" : "Missing") + "\n");
                }
            }
            catch (Exception ex)
            {
                OutputBox.AppendText("Error verifying UE files: " + ex.Message + "\n");
            }
        }

        private async void RunSetup_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string uePath = @"C:\Program Files\Epic Games\UE_5.7\Engine\Extras\Android";
                string script = Path.Combine(uePath, "SetupAndroid.bat");
                if (!File.Exists(script))
                {
                    OutputBox.AppendText("SetupAndroid.bat not found at: " + script + "\n");
                    return;
                }

                OutputBox.AppendText("Running SetupAndroid.bat...\n");
                var result = await RunProcessAsync(script, "", uePath,
                    s => Dispatcher.Invoke(() => OutputBox.AppendText(s + "\n")),
                    s => Dispatcher.Invoke(() => OutputBox.AppendText(s + "\n")));
                OutputBox.AppendText("Setup script finished.\n");
            }
            catch (Exception ex)
            {
                OutputBox.AppendText("Error running setup script: " + ex.Message + "\n");
            }
        }

        private async void GenerateKeystore_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // find keytool
                string javaHome = Environment.GetEnvironmentVariable("JAVA_HOME");
                string keytoolPath = null;
                if (!string.IsNullOrWhiteSpace(javaHome))
                {
                    var candidate = Path.Combine(javaHome, "bin", "keytool.exe");
                    if (File.Exists(candidate)) keytoolPath = candidate;
                }
                if (keytoolPath == null)
                {
                    // try in PATH
                    keytoolPath = "keytool.exe";
                }

                string keystorePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "ExampleKey.keystore");
                string storepass = "changeit";
                string dname = "CN=Example, OU=Dev, O=Company, L=City, S=State, C=US";

                var args = $"-genkeypair -alias MyKey -keyalg RSA -keysize 2048 -validity 10000 -keystore \"{keystorePath}\" -storepass {storepass} -keypass {storepass} -dname \"{dname}\"";
                OutputBox.AppendText("Generating keystore at: " + keystorePath + "\n");
                var result = await RunProcessAsync(keytoolPath, args, null,
                    s => Dispatcher.Invoke(() => OutputBox.AppendText(s + "\n")),
                    s => Dispatcher.Invoke(() => OutputBox.AppendText(s + "\n")));
                if (File.Exists(keystorePath))
                {
                    OutputBox.AppendText("Keystore generated: " + keystorePath + "\n");
                }
                else
                {
                    OutputBox.AppendText("Keytool output:\n" + result.StandardOutput + result.StandardError + "\n");
                    OutputBox.AppendText("Keystore not created. Ensure keytool is available and JAVA_HOME is set.\n");
                }
            }
            catch (Exception ex)
            {
                OutputBox.AppendText("Error generating keystore: " + ex.Message + "\n");
            }
        }

        private Task<(string StandardOutput, string StandardError)> RunProcessAsync(string fileName, string arguments, string workingDirectory = null, Action<string> outputCallback = null, Action<string> errorCallback = null)
        {
            var tcs = new TaskCompletionSource<(string, string)>();
            try
            {
                var psi = new ProcessStartInfo(fileName, arguments)
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };
                if (!string.IsNullOrWhiteSpace(workingDirectory)) psi.WorkingDirectory = workingDirectory;

                var p = new Process { StartInfo = psi, EnableRaisingEvents = true };
                var stdOut = new StringBuilder();
                var stdErr = new StringBuilder();
                p.OutputDataReceived += (s, e) => { if (e.Data != null) { stdOut.AppendLine(e.Data); outputCallback?.Invoke(e.Data); } };
                p.ErrorDataReceived += (s, e) => { if (e.Data != null) { stdErr.AppendLine(e.Data); errorCallback?.Invoke(e.Data); } };
                p.Exited += (s, e) => tcs.TrySetResult((stdOut.ToString(), stdErr.ToString()));
                p.Start();
                p.BeginOutputReadLine();
                p.BeginErrorReadLine();
            }
            catch (Exception ex)
            {
                tcs.TrySetResult(("", ex.Message));
            }
            return tcs.Task;
        }
    }
}

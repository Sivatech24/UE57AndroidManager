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
using System.Threading;
using System.Net;

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
            string url = "https://edgedl.me.gvt1.com/edgedl/android/studio/install/2024.1.2.13/android-studio-2024.1.2.13-windows.exe";
            string filePath = @"C:\Temp\android-studio-koala.exe";

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                var cancelled = await DownloadFileWithProgressAsync(url, filePath);
                if (cancelled)
                {
                    OutputBox.AppendText("Download cancelled.\n");
                    return;
                }

                OutputBox.AppendText("Android Studio downloaded to: " + filePath + "\n");
                OutputBox.AppendText("\nInstallation instructions:\n");
                OutputBox.AppendText("1) Run the downloaded installer: " + filePath + "\n");
                OutputBox.AppendText("2) During installation choose the SDK location or note the default SDK path.\n");
                OutputBox.AppendText("3) After install, open Android Studio -> SDK Manager and ensure:\n   - Android SDK Platform 35 installed\n   - Android SDK Build-Tools 35.0.1 installed\n   - NDK (side by side) 27.x installed\n");
                OutputBox.AppendText("4) Set JAVA_HOME to your OpenJDK 21 installation path and restart the app.\n");
                OutputBox.AppendText("5) Then run 'Install Required SDK + NDK' in this tool to ensure packages.\n");
            }
            catch (Exception ex)
            {
                OutputBox.AppendText("Error downloading Android Studio: " + ex.Message + "\n");
            }
        }

        private async Task<bool> DownloadFileWithProgressAsync(string url, string destinationPath)
        {
            var progressWindow = new ProgressWindow();
            var cts = progressWindow.Cancellation;
            progressWindow.Owner = this;
            progressWindow.Show();

            try
            {
                using (var client = new HttpClient())
                using (var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cts.Token))
                {
                    response.EnsureSuccessStatusCode();
                    var contentLength = response.Content.Headers.ContentLength ?? -1L;
                    using (var contentStream = await response.Content.ReadAsStreamAsync())
                    using (var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        var buffer = new byte[81920];
                        long totalRead = 0;
                        int read;
                        while ((read = await contentStream.ReadAsync(buffer, 0, buffer.Length, cts.Token)) > 0)
                        {
                            await fileStream.WriteAsync(buffer, 0, read, cts.Token);
                            totalRead += read;
                            double percent = contentLength > 0 ? (totalRead * 100d / contentLength) : -1;
                            progressWindow.Report(percent, totalRead, contentLength);
                        }
                    }
                }
                return false;
            }
            catch (OperationCanceledException)
            {
                try { if (File.Exists(destinationPath)) File.Delete(destinationPath); } catch { }
                return true;
            }
            finally
            {
                progressWindow.Dispatcher.Invoke(() => progressWindow.Close());
            }
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
            // open keystore input window
            var win = new KeystoreWindow();
            win.Owner = this;
            if (win.ShowDialog() == true)
            {
                var model = win.Model;
                await CreateKeystoreAsync(model);
            }
        }

        private async Task CreateKeystoreAsync(KeystoreModel model)
        {
            try
            {
                string javaHome = Environment.GetEnvironmentVariable("JAVA_HOME");
                string keytoolPath = null;
                if (!string.IsNullOrWhiteSpace(javaHome))
                {
                    var candidate = Path.Combine(javaHome, "bin", "keytool.exe");
                    if (File.Exists(candidate)) keytoolPath = candidate;
                }
                if (keytoolPath == null) keytoolPath = "keytool.exe";

                string keystorePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), model.FileName ?? "ExampleKey.keystore");

                var dname = $"CN={model.CommonName}, OU={model.OrganizationalUnit}, O={model.Organization}, L={model.Locality}, S={model.State}, C={model.Country}";
                var args = $"-genkeypair -alias {model.Alias} -keyalg RSA -keysize {model.KeySize} -validity {model.ValidityDays} -keystore \"{keystorePath}\" -storepass {model.StorePassword} -keypass {model.KeyPassword} -dname \"{dname}\"";
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

        private async void DownloadJDK_Click(object sender, RoutedEventArgs e)
        {
            string[] urls = new[] {
                "https://download.oracle.com/java/21/archive/jdk-21.0.3_windows-x64_bin.exe",
                "https://download.oracle.com/java/21/archive/jdk-21.0.3_windows-x64_bin.msi"
            };
            string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
            Directory.CreateDirectory(folder);
            foreach (var url in urls)
            {
                var fileName = Path.GetFileName(new Uri(url).LocalPath);
                var dest = Path.Combine(folder, fileName);
                OutputBox.AppendText("Starting download: " + url + "\n");
                try
                {
                    var cancelled = await DownloadFileWithProgressAsync(url, dest);
                    if (cancelled)
                    {
                        OutputBox.AppendText("Download cancelled: " + url + "\n");
                    }
                    else
                    {
                        OutputBox.AppendText("Downloaded JDK to: " + dest + "\n");
                    }
                }
                catch (Exception ex)
                {
                    OutputBox.AppendText("Error downloading JDK from " + url + ": " + ex.Message + "\n");
                }
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

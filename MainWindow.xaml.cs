using System;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using System.Diagnostics;
using Microsoft.Win32;
using Microsoft.Web.WebView2.Core;

namespace FlowState
{
    public partial class MainWindow : Window
    {
        private FileSystemWatcher? _watcher;
        private PerformanceCounter? _cpuCounter;
        private DispatcherTimer? _perfTimer;

        public MainWindow()
        {
            InitializeComponent();
            InitializeWebView();
            InitializeProfiler();
        }

        private void InitializeProfiler()
        {
            try
            {
                // Setăm contorul pentru a citi procesorul total al sistemului
                _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                _cpuCounter.NextValue(); // Prima citire e mereu 0, o "consumăm" aici

                // Creăm un timer care ticăie o dată pe secundă
                _perfTimer = new DispatcherTimer();
                _perfTimer.Interval = TimeSpan.FromSeconds(1);
                _perfTimer.Tick += (s, e) =>
                {
                    // Extragem valoarea (ex: 12.5) și o rotunjim la un număr întreg
                    int cpuUsage = (int)_cpuCounter.NextValue();
                    
                    // Trimitem valoarea către frontend
                    string jsonMessage = $"{{\"type\": \"PERF_UPDATE\", \"cpu\": {cpuUsage}}}";
                    webView.CoreWebView2.PostWebMessageAsJson(jsonMessage);
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Eroare la inițializarea senzorilor: {ex.Message}");
            }
        }

        private async void InitializeWebView()
        {
            await webView.EnsureCoreWebView2Async(null);
            webView.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;
            
            string htmlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot", "index.html");
            webView.CoreWebView2.Navigate(htmlPath);

            // Pornim timer-ul abia după ce mediul web este încărcat
            webView.NavigationCompleted += (s, e) => {
                _perfTimer?.Start();
            };
        }

        private void CoreWebView2_WebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            string message = e.TryGetWebMessageAsString();

            if (message == "SELECT_FOLDER")
            {
                OpenFolderDialog dialog = new OpenFolderDialog { Title = "Alege folderul proiectului" };

                if (dialog.ShowDialog() == true)
                {
                    string selectedPath = dialog.FolderName;

                    // Trimitem JSON în loc de string simplu, pentru o structură mai clară
                    string jsonResponse = $"{{\"type\": \"FOLDER_SELECTED\", \"path\": \"{selectedPath.Replace("\\", "\\\\")}\"}}";
                    webView.CoreWebView2.PostWebMessageAsJson(jsonResponse);

                    StartWatchingFolder(selectedPath);
                }
            }
        }

        private void StartWatchingFolder(string path)
        {
            // Dacă monitorizam deja alt folder, îl oprim pe cel vechi
            if (_watcher != null)
            {
                _watcher.Dispose();
            }

            _watcher = new FileSystemWatcher(path)
            {
                IncludeSubdirectories = true, // Ne uităm și în subfoldere
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName
            };

            // Abonare la evenimentul de modificare a unui fișier
            _watcher.Changed += OnFileChanged;

            // Dăm drumul la monitorizare
            _watcher.EnableRaisingEvents = true;
        }

        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            try
            {
                System.Threading.Thread.Sleep(100);

                // Citim tot textul din fișierul modificat
                string fileContent = File.ReadAllText(e.FullPath);
                string safeContent = System.Text.Json.JsonSerializer.Serialize(fileContent);

                // Curățăm căile pentru JSON (înlocuim \ cu \\)
                string safePath = e.FullPath.Replace("\\", "\\\\");
                string safeName = e.Name?.Replace("\\", "\\\\") ?? "Unknown";

                string jsonMessage = $"{{\"type\": \"FILE_CHANGED\", \"path\": \"{safePath}\", \"name\": \"{safeName}\", \"content\": {safeContent}}}";

                Application.Current.Dispatcher.Invoke(() =>
                {
                    webView.CoreWebView2.PostWebMessageAsJson(jsonMessage);
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Eroare la citirea fișierului: {ex.Message}");
            }
        }
    }
}
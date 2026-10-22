using System;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using System.Diagnostics;
using Microsoft.Win32;
using Microsoft.Web.WebView2.Core;
using System.Net.Http;
using System.Text;

namespace FlowState
{
    public partial class MainWindow : Window
    {
        private FileSystemWatcher? _watcher;
        private PerformanceCounter? _cpuCounter;
        private DispatcherTimer? _perfTimer;
        private static readonly HttpClient _httpClient = new HttpClient();

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
            string jsonResponse = $"{{\"type\": \"FOLDER_SELECTED\", \"path\": \"{selectedPath.Replace("\\", "\\\\")}\"}}";
            webView.CoreWebView2.PostWebMessageAsJson(jsonResponse);
            StartWatchingFolder(selectedPath);
        }
    }
    // Interceptăm noul pachet JSON trimis pentru AI
    else if (message.StartsWith("{") && message.Contains("\"ASK_AI\""))
            {
                try
                {
                    var json = System.Text.Json.JsonDocument.Parse(message);
                    string prompt = json.RootElement.GetProperty("prompt").GetString() ?? "";
                    string context = json.RootElement.GetProperty("context").GetString() ?? "";

                    // Lansăm apelul HTTP asincron pe un thread separat ca să nu blocăm interfața (fereastra)
                    _ = AskOllamaAsync(prompt, context);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Eroare la parsarea mesajului AI: " + ex.Message);
                }
            }
        }

        private async System.Threading.Tasks.Task AskOllamaAsync(string userPrompt, string codeContext)
        {
            try
            {
                // Construim un prompt masiv care include și codul curent, dacă există
                string fullPrompt = string.IsNullOrEmpty(codeContext)
                    ? userPrompt
                    : $"Uite codul curent:\n\n{codeContext}\n\nÎntrebarea mea: {userPrompt}";

                // Creăm payload-ul pentru Ollama. Setăm stream: false pentru a primi tot răspunsul deodată.
                var payload = new
                {
                    model = "qwen2.5-coder", // Schimbă aici dacă ai descărcat alt model (ex: codellama sau qwen)
                    prompt = fullPrompt,
                    stream = false
                };

                string jsonPayload = System.Text.Json.JsonSerializer.Serialize(payload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                // Trimitem cererea către portul standard pe care rulează Ollama în fundal
                HttpResponseMessage response = await _httpClient.PostAsync("http://localhost:11434/api/generate", content);
                response.EnsureSuccessStatusCode();

                string responseJson = await response.Content.ReadAsStringAsync();
                var responseDoc = System.Text.Json.JsonDocument.Parse(responseJson);
                string aiReply = responseDoc.RootElement.GetProperty("response").GetString() ?? "";

                // Escapăm răspunsul pentru JSON (new line-uri, ghilimele duble)
                string safeReply = System.Text.Json.JsonSerializer.Serialize(aiReply);
                string finalMessage = $"{{\"type\": \"AI_RESPONSE\", \"text\": {safeReply}}}";

                Application.Current.Dispatcher.Invoke(() =>
                {
                    webView.CoreWebView2.PostWebMessageAsJson(finalMessage);
                });
            }
            catch (Exception ex)
            {
                // Tratăm cazul în care Ollama nu este pornit
                string errorSafe = System.Text.Json.JsonSerializer.Serialize($"Eroare conexiune: {ex.Message}. Asigură-te că Ollama rulează.");
                Application.Current.Dispatcher.Invoke(() =>
                {
                    webView.CoreWebView2.PostWebMessageAsJson($"{{\"type\": \"AI_RESPONSE\", \"text\": {errorSafe}}}");
                });
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
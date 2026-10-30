using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Windows;
using Microsoft.Web.WebView2.Core;

namespace FlowState.Desktop;

public partial class MainWindow : Window
{
    private static readonly HttpClient _httpClient = new HttpClient
    {
        BaseAddress = new Uri("http://localhost:5084/")
    };

    public MainWindow()
    {
        InitializeComponent();
        InitializeAsync();
    }

    async void InitializeAsync()
    {
        await MainWebView.EnsureCoreWebView2Async();
        
        string htmlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot", "index.html");
        MainWebView.CoreWebView2.Navigate(htmlPath);

        MainWebView.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;
    }

    private async void CoreWebView2_WebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        string jsonMessage = e.TryGetWebMessageAsString();
        
        try
        {
            using var doc = JsonDocument.Parse(jsonMessage);
            var root = doc.RootElement;
            string action = root.GetProperty("action").GetString() ?? "";
            string email = root.GetProperty("email").GetString() ?? "";
            string password = root.GetProperty("password").GetString() ?? "";
            string username = root.TryGetProperty("username", out var uProp) ? uProp.GetString() ?? "" : "";

            if (action == "login")
            {
                await HandleLogin(email, password);
            }
            else if (action == "register")
            {
                await HandleRegister(email, username, password);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Eroare: {ex.Message}", "Eroare", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task HandleLogin(string email, string password)
    {
        var data = new { email, password };
        var content = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json");

        try
        {
            var response = await _httpClient.PostAsync("api/Auth/login", content);
            if (response.IsSuccessStatusCode)
            {
                string resString = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(resString);
                string token = doc.RootElement.GetProperty("token").GetString() ?? "";

                MessageBox.Show("Autentificare reușită!", "Succes", MessageBoxButton.OK, MessageBoxImage.Information);
                // Aici vom deschide fereastra principală a aplicației în curând
            }
            else
            {
                string error = await response.Content.ReadAsStringAsync();
                MessageBox.Show($"Eroare: {error}", "Eșec", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        catch
        {
            MessageBox.Show("Nu se poate conecta la API.", "Eroare rețea", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task HandleRegister(string email, string username, string password)
    {
        var data = new { email, username, password };
        var content = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json");

        try
        {
            var response = await _httpClient.PostAsync("api/Auth/register", content);
            if (response.IsSuccessStatusCode)
            {
                MessageBox.Show("Cont creat cu succes! Te poți loga acum.", "Succes", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                string error = await response.Content.ReadAsStringAsync();
                MessageBox.Show($"Eroare la înregistrare: {error}", "Eșec", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        catch
        {
            MessageBox.Show("Nu se poate conecta la API.", "Eroare rețea", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
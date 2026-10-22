<div align="center">
  <!-- GitHub will display your icon here if you include it in the repository -->
  <img src="app.ico" alt="FlowState Logo" width="120">
  
  <h1>⚡ FlowState</h1>
  <p><b>An offline-first, modular, and ultra-fast desktop developer companion.</b></p>
</div>

---

## 🚀 About The Project
FlowState is a utility dashboard built on a hybrid architecture (C# .NET WPF backend, Web technologies frontend). It is designed to run silently as a system utility, offering real-time code visualization, hardware performance monitoring, and, in the near future, fully isolated AI assistance. All without accounts, logins, or databases.

## ✨ Core Features

*   **🔍 Code Mirror (Live File Tracker)**
    Select your working project folder, and FlowState uses `FileSystemWatcher` to intercept any saved file. The code is rendered instantly using the VS Code backend engine (**Monaco Editor**). Includes a sidebar tab system for quick switching between modified files.
*   **📈 System Profiler (Live Telemetry)**
    Extracts data from native Windows sensors (`PerformanceCounter`) and renders hardware telemetry directly in the frontend. Live charts track CPU fluctuations in real-time with zero lag.
*   **🧠 AI Architect (In Development)**
    An integrated contextual assistant. It will automatically read the currently selected file in the *Code Mirror* and generate architecture, refactorings, or explanations by running a **100% offline** AI model (via Ollama).

## 🛠️ Architecture & Tech Stack
The application communicates via a bidirectional asynchronous bridge (`PostWebMessageAsJson`), where C# acts as a local resource server, and JavaScript renders the modern graphical interface.

*   **Backend:** C# / .NET 8 / WPF
*   **Rendering Engine:** Microsoft Edge WebView2 (Evergreen mode)
*   **Frontend UI:** HTML5, CSS3 (Custom Dark & Frosted Glass Theme), Vanilla JS
*   **External Modules (via CDN):** Monaco Editor, Chart.js, FontAwesome

## ⚙️ Installation & Usage
Make sure you have the [.NET 8 SDK](https://dotnet.microsoft.com/) installed on your Windows machine.

1. Clone the local repository:
   ```bash
   git clone [https://github.com/your-username/FlowState.git](https://github.com/your-username/FlowState.git)
   cd FlowState
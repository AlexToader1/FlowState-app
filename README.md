<div align="center">
  <!-- GitHub va afișa iconița ta aici dacă o pui în repository -->
  <img src="app.ico" alt="FlowState Logo" width="120">
  
  <h1>⚡ FlowState</h1>
  <p><b>Un companion desktop offline-first pentru dezvoltatori, modular și ultra-rapid.</b></p>
</div>

---

## 🚀 Despre Proiect
FlowState este un dashboard utilitar construit pe o arhitectură hibridă (C# .NET WPF în spate, tehnologii Web în față). Este gândit să ruleze silențios ca un utilitar de sistem, oferind vizualizare de cod în timp real, monitorizare de performanță hardware și, în viitorul apropiat, asistență AI complet izolată. Totul fără conturi, login-uri sau baze de date.

## ✨ Funcționalități Principale

*   **🔍 Code Mirror (Live File Tracker)**
    Selectezi folderul proiectului la care lucrezi, iar FlowState folosește `FileSystemWatcher` pentru a intercepta orice fișier salvat. Codul este randat instantaneu folosind motorul din spate de la VS Code (**Monaco Editor**). Include un sistem de tab-uri laterale pentru comutarea rapidă între fișierele modificate.
*   **📈 System Profiler (Live Telemetry)**
    Extrage date din senzorii nativi de Windows (`PerformanceCounter`) și randează telemetria hardware direct în frontend. Graficele live urmăresc fluctuațiile de CPU în timp real cu zero lag.
*   **🧠 AI Architect (În dezvoltare)**
    Un asistent contextual integrat. Va citi automat fișierul selectat curent în *Code Mirror* și va genera arhitectură, refactorizări sau explicații rulând un model de inteligență artificială **100% offline** (via Ollama).

## 🛠️ Arhitectură și Tech Stack
Aplicația comunică printr-un bridge asincron bidirecțional (`PostWebMessageAsJson`), unde C#-ul acționează ca un server local de resurse, iar JavaScript-ul randează interfața grafică modernă.

*   **Backend:** C# / .NET 8 / WPF
*   **Motor Randare:** Microsoft Edge WebView2 (Evergreen mode)
*   **Frontend UI:** HTML5, CSS3 (Custom Dark & Frosted Glass Theme), Vanilla JS
*   **Module Externe (via CDN):** Monaco Editor, Chart.js, FontAwesome

## ⚙️ Instalare și Rulare
Asigură-te că ai instalat [.NET 8 SDK](https://dotnet.microsoft.com/) pe mașina ta Windows.

1. Clonează repository-ul local:
   ```bash
   git clone [https://github.com/user-ul-tau/FlowState.git](https://github.com/user-ul-tau/FlowState.git)
   cd FlowState
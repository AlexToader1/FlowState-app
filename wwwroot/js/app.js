const openFiles = {};
let activeFile = null;
let editor = null;

require.config({ paths: { 'vs': 'https://cdnjs.cloudflare.com/ajax/libs/monaco-editor/0.45.0/min/vs' }});

// --- CONFIGURARE CHART.JS ---
let cpuChart = null;
const maxDataPoints = 30;

// Inițializăm graficul direct, fără DOMContentLoaded
const ctx = document.getElementById('cpuChart').getContext('2d');
cpuChart = new Chart(ctx, {
    type: 'line',
    data: {
        labels: Array(maxDataPoints).fill(''),
        datasets: [{
            label: 'CPU Usage (%)',
            data: Array(maxDataPoints).fill(0),
            borderColor: '#00b894',
            backgroundColor: 'rgba(0, 184, 148, 0.1)',
            borderWidth: 2,
            pointRadius: 0,
            fill: true,
            tension: 0.4
        }]
    },
    options: {
        responsive: true,
        maintainAspectRatio: false,
        animation: false,
        scales: {
            y: { min: 0, max: 100 }
        }
    }
});

// Navigarea între tab-uri
const navItems = document.querySelectorAll('.nav-links li');
const views = document.querySelectorAll('.view');

navItems.forEach(item => {
    item.addEventListener('click', () => {
        navItems.forEach(nav => nav.classList.remove('active'));
        views.forEach(view => view.classList.remove('active'));
        item.classList.add('active');
        document.getElementById(item.getAttribute('data-target')).classList.add('active');
    });
});

// Comutarea fișierelor
function switchFile(path) {
    activeFile = path;
    document.querySelectorAll('#trackedFiles li').forEach(li => {
        li.classList.toggle('active', li.getAttribute('data-path') === path);
    });
    if (editor) editor.setValue(openFiles[path]);
}

// Comunicarea cu C#
document.getElementById('selectFolderBtn').addEventListener('click', () => {
    window.chrome.webview.postMessage("SELECT_FOLDER");
});

window.chrome.webview.addEventListener('message', event => {
    const data = event.data;

    if (data.type === "FOLDER_SELECTED") {
        document.getElementById('folderPath').innerText = data.path;
        document.getElementById('editorContainer').style.display = "block";
        
        if (!editor) {
            require(['vs/editor/editor.main'], function () {
                editor = monaco.editor.create(document.getElementById('editorContainer'), {
                    value: "// Oglinda este activă. Aștept modificări...",
                    language: 'javascript', 
                    theme: 'vs-dark',
                    readOnly: true,
                    minimap: { enabled: false },
                    scrollBeyondLastLine: false,
                    automaticLayout: true
                });
            });
        }
    } 
    else if (data.type === "FILE_CHANGED") {
        const pathKey = data.path.toLowerCase();
        const isNewFile = !openFiles[pathKey];
        
        openFiles[pathKey] = data.content;

        if (isNewFile) {
            document.getElementById('fileList').style.display = 'flex';
            
            const li = document.createElement('li');
            li.innerText = data.name;
            li.setAttribute('data-path', pathKey);
            li.onclick = () => switchFile(pathKey);
            
            document.getElementById('trackedFiles').appendChild(li);
            
            if (!activeFile) switchFile(pathKey);
        }

        if (activeFile === pathKey && editor) {
            const position = editor.getPosition();
            editor.setValue(data.content);
            if (position) editor.setPosition(position);
        }
    }
    else if (data.type === "PERF_UPDATE") {
        if (cpuChart) {
            const chartData = cpuChart.data.datasets[0].data;
            chartData.push(data.cpu);
            chartData.shift();
            cpuChart.update();
        }
    }
});
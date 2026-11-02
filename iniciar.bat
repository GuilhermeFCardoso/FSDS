@echo off
cd /d "%~dp0"
start "" "http://localhost:8765/index.html"
where python >nul 2>&1 && (python -m http.server 8765) || (py -m http.server 8765)

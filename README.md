# AutoIME (WinUI 3 Refactored)

A modern Windows application built with **C# .NET 8 + WinUI 3 (Windows App SDK)** for automatically switching input methods (IME) and keyboard layouts based on the active foreground window.

[ English | [简体中文](README_zh.md) ]

[![Platform](https://img.shields.io/badge/Platform-Windows%2010%20%2F%2011-0078D6.svg?style=flat-square)](https://microsoft.com/windows)
[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4.svg?style=flat-square)](https://dotnet.microsoft.com/)
[![WinUI 3](https://img.shields.io/badge/WinUI-3.0-0078D4.svg?style=flat-square)](https://learn.microsoft.com/windows/apps/winui/winui3/)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg?style=flat-square)](LICENSE)

---

## Overview

AutoIME is a lightweight background assistant tool running on Windows that automatically switches to the designated IME or keyboard layout when you switch between active application windows.

This project is a complete refactoring of the original AutoHotkey v2 + WebView2 implementation into a **native WinUI 3 + .NET 8 desktop application**:
- **Native Performance**: No external script interpreter or embedded Chromium runtime process required. Near-zero memory footprint and instantaneous startup.
- **Modern Windows 11 Fluent Design**: Mica backdrop material, rounded cards, seamless dark/light/system theme adaptation, and integrated Windows 11 title bar.
- **100% Config Compatible**: Directly reads and writes the existing `AutoIME_config.ini` format.
- **Portable Unpackaged App**: Runs directly as an unpackaged executable without MSIX certificate installation.

---

## Features

- **Process-Level Automatic Switching**: Uses Win32 `SetWinEventHook` to monitor `EVENT_SYSTEM_FOREGROUND` events with zero polling and zero latency, seamlessly switching input methods using underlying IMM32 APIs.
- **Native WinUI 3 Interface**: Renders crisp Windows 11 Fluent UI, dynamically extracting and displaying actual application high-res icons.
- **Dynamic System Layout Enumeration**: Enumerates all installed keyboard layouts and input methods from the Windows API and registry, generating dynamic tabs with app count badges.
- **🎯 Window Capture Mode**: Hover over any window with an interactive crosshair/HUD displaying the process name and window title, then click or press Enter/Space/F8 to bind rules instantly.
- **⌨️ Interactive Hotkey Recording**: Built-in modal dialog to capture physical key combinations (`Ctrl`, `Alt`, `Shift`, `Win` + any key) with global shortcut registration.
- **Search & Batch Management**: Real-time process filtering, select all/individual selection, batch deletion, and clearing tabs.
- **Theme Modes**: ☀️ Light, 🌙 Dark, and 💻 Follow System.
- **System Tray Integration**: Minimizes to tray, with a context menu for quick access, reloading, toggling tooltips, auto-start, and restarting/exiting.
- **Multi-Language Support**: English, Simplified Chinese, and Japanese using Windows-recommended `.resw` / PRI resource management.

---

## Build & Run

### Prerequisites

- **OS**: Windows 10 Version 1809 (Build 17763) or Windows 11 (Recommended)
- **SDK**: [.NET 8.0 SDK](https://dotnet.microsoft.com/) or later

### 1. Build Solution

```powershell
dotnet build
```

### 2. Run Application

```powershell
dotnet run --project src/AutoIME/AutoIME.csproj
```

### 3. Publish Standalone Single-File Binary

```powershell
dotnet publish src/AutoIME/AutoIME.csproj -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:WindowsPackageType=None -o ./dist
```

The resulting standalone executable will be generated at `./dist/AutoIME.exe`.

---

## License

This project is licensed under the [MIT License](LICENSE).


# AutoIME (WinUI 3 重构版)

基于 **C# .NET 8 + WinUI 3 (Windows App SDK)** 全新重构的现代化 Windows 输入法与键盘布局自动切换工具。

[ [English](README.md) | 简体中文 ]

[![Platform](https://img.shields.io/badge/Platform-Windows%2010%20%2F%2011-0078D6.svg?style=flat-square)](https://microsoft.com/windows)
[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4.svg?style=flat-square)](https://dotnet.microsoft.com/)
[![WinUI 3](https://img.shields.io/badge/WinUI-3.0-0078D4.svg?style=flat-square)](https://learn.microsoft.com/windows/apps/winui/winui3/)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg?style=flat-square)](LICENSE)

---

## 概述

AutoIME 是一个运行于 Windows 平台的后台智能辅助工具，能够根据当前前台活动窗口自动切换对应的输入法（IME）或键盘布局。

本项目将原有 AutoHotkey v2 + WebView2 架构**全面使用微软原生 WinUI 3 与 C# .NET 8 进行了现代模块化重构**：
- **纯原生执行性能**：告别脚本解释器与 Chromium 内核进程驻留，启动瞬时、内存占用极低。
- **现代化 Windows 11 Fluent 视觉**：支持 Mica 云母背景材质、圆角卡片、三态主题（浅色/深色/跟随系统）与 Windows 11 原生标题栏无缝融合。
- **完全兼容原有配置**：100% 兼容原项目的 `AutoIME_config.ini` 配置文件格式，开箱即用。
- **绿色免安装运行**：支持免 MSIX 打包的独立绿色运行模式（Unpackaged Mode），无需配置自签名证书。

---

## 核心特性

- **进程级别自动切换**：利用 Win32 `SetWinEventHook` 实时监听 `EVENT_SYSTEM_FOREGROUND` 前台窗口切换事件（零周期轮询、零系统额外开销），并通过底层 IMM32 接口瞬时切换目标输入法及状态。
- **原生 WinUI 3 交互界面**：基于 Windows App SDK 与 WinUI 3 打造，支持动态提取配置程序的真实高清图标显示在列表中。
- **系统布局动态枚举**：通过 Win32 API 与系统注册表动态读取系统当前已安装的所有键盘布局与输入法，自动生成对应的切换标签页与规则数量徽章。
- **🎯 窗口捕获模式**：点击后显示吸附悬浮 HUD，移动到目标程序窗口上方实时显示进程名与标题，单击左键或按回车/空格即可一键绑定规则。
- **⌨️ 交互式按键录制**：内置快捷键设置弹窗，实时监听物理按键组合（支持 `Ctrl`、`Alt`、`Shift`、`Win` 与任意按键），保存后全局生效。
- **规则检索与批量管理**：支持按进程名或备注实时模糊搜索、全选与多选批量删除及清空当前页规则。
- **三态主题切换**：支持 ☀️ 浅色模式、🌙 深色模式与 💻 跟随系统自动适配。
- **系统托盘深度集成**：支持最小化至系统通知区域常驻，托盘右键菜单提供快速打开、重载配置、悬浮提示切换、开机自启、在线检查更新及重启退出。
- **多语言国际化支持**：内置简体中文 (zh)、英文 (en) 与日语 (ja)，可随界面实时切换。

---

## 项目架构

```
AutoIME-winui3/
├── AutoIME.sln                  # 解决方案入口
├── src/AutoIME/                 # WinUI 3 主程序工程
│   ├── AutoIME.csproj          # 工程配置 (Unpackaged .NET 8)
│   ├── app.manifest            # Windows DPI 与特权清单
│   ├── App.xaml / App.xaml.cs   # 应用单实例生命周期
│   ├── MainWindow.xaml / .cs    # 主配置窗口 (Mica, 标签页, 工具栏, 规则表)
│   ├── Helpers/
│   │   └── Win32Native.cs      # IMM32、WinEventHook、User32 P/Invoke 接口定义
│   ├── Models/
│   │   ├── AppRule.cs          # 单条程序规则实体 (图标, 进程名, 备注)
│   │   ├── ImeInfo.cs          # 输入法/键盘布局模型 (HKL, 显示名称, 规则列表)
│   │   └── AppSettings.cs      # 全局设置模型 (语言, 主题, 快捷键, 悬浮提示)
│   ├── Services/
│   │   ├── ConfigService.cs    # AutoIME_config.ini 读写与兼容性解析器
│   │   ├── ImeEngine.cs        # 底层输入法枚举与激活引擎
│   │   ├── WindowWatcherService.cs # 全局前台窗口监听与规则匹配服务
│   │   ├── WindowCaptureService.cs # 靶心窗口捕获模式与鼠标/按键钩子
│   │   ├── HotkeyService.cs    # 全局唤醒热键注册与窗口子类化消息拦截
│   │   ├── FileIconHelper.cs   # 提取目标 EXE 文件高清图标并生成位图
│   │   ├── StartupService.cs   # Windows 开机自启动快捷方式管理
│   │   ├── LocalizationService.cs # 国际化多语言翻译字典与格式化器
│   │   └── UpdateService.cs    # GitHub Release 最新版本比对检查服务
│   ├── ViewModels/
│   │   └── MainViewModel.cs    # MVVM 响应式逻辑与命令处理
│   └── Views/
│       ├── HotkeyDialog.xaml   # 组合键录制设置对话框
│       ├── ManualAddDialog.xaml# 手动输入进程添加对话框
│       ├── ToolTipFloatingWindow.xaml # 悬浮提示微动效通知窗口
│       └── CaptureOverlayWindow.xaml  # 窗口捕获模式屏幕悬浮 HUD
└── AutoIME_config.example.ini  # 配置文件示例
```

---

## 构建与运行

### 环境要求

- **操作系统**：Windows 10 1809 (Build 17763) 或 Windows 11 (推荐)
- **开发工具**：[.NET 8.0 SDK](https://dotnet.microsoft.com/) 或以上

### 1. 编译项目

在工作区根目录下执行：

```powershell
dotnet build
```

### 2. 运行应用程序

```powershell
dotnet run --project src/AutoIME/AutoIME.csproj
```

### 3. 发布独立单文件安装包 (免运行库绿色版)

使用以下命令可一键发布单文件绿色可执行程序，无需目标机安装 .NET 或 Windows App SDK 运行库：

```powershell
dotnet publish src/AutoIME/AutoIME.csproj -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:WindowsPackageType=None -o ./dist
```

生成的文件位于 `./dist/AutoIME.exe`，直接双击即可运行。

---

## 配置文件说明

`AutoIME_config.ini` 与原 AutoIME 项目保持完全一致的 INI 格式：

```ini
[Settings]
DebugMode=0        ; 0: 关闭提示 | 1: 切换窗口时右下角弹出悬浮提示
Language=zh        ; 界面语言: zh (中文) | en (英文) | ja (日文)
Hotkey=^!i         ; 唤醒快捷键 (^=Ctrl, !=Alt, +=Shift, #=Win)
Theme=system       ; 外观主题: system (跟随系统) | light (浅色) | dark (深色)

[IME_0x08040804]   ; 微软拼音输入法 HKL 标识
DisplayName=中文(简体) - 微软拼音
weixin.exe=微信
qq.exe=QQ

[IME_0x04090409]   ; 英文键盘 HKL 标识
DisplayName=英语(美国) - 美式键盘
code.exe=Visual Studio Code
windowsterminal.exe=Windows Terminal
```

---

## 许可证

本项目基于 [MIT License](LICENSE) 开源发布。


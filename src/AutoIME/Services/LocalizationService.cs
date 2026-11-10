using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Windows.ApplicationModel.Resources;

namespace AutoIME.Services
{
    public partial class LocalizationService : ObservableObject
    {
        private static LocalizationService? _instance;
        public static LocalizationService Instance => _instance ??= new LocalizationService();

        [ObservableProperty]
        private string _currentLanguage = "zh";

        private readonly ResourceManager? _resourceManager;
        private readonly ResourceContext? _resourceContext;
        private readonly ResourceMap? _resourceMap;

        public LocalizationService()
        {
            try
            {
                var priPath = System.IO.Path.Combine(AppContext.BaseDirectory, "AutoIME.pri");
                if (!System.IO.File.Exists(priPath))
                {
                    priPath = System.IO.Path.Combine(AppContext.BaseDirectory, "resources.pri");
                }

                if (System.IO.File.Exists(priPath))
                {
                    _resourceManager = new ResourceManager(priPath);
                }
                else
                {
                    _resourceManager = new ResourceManager();
                }

                _resourceContext = _resourceManager.CreateResourceContext();
                try
                {
                    _resourceMap = _resourceManager.MainResourceMap.GetSubtree("Resources");
                }
                catch
                {
                    _resourceMap = _resourceManager.MainResourceMap;
                }
                UpdateResourceContextLanguage();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LocalizationService] MRT ResourceManager init warning: {ex.Message}");
                _resourceManager = null;
                _resourceContext = null;
                _resourceMap = null;
            }
        }

        private void UpdateResourceContextLanguage()
        {
            if (_resourceContext == null) return;

            var tag = CurrentLanguage switch
            {
                "zh" => "zh-CN",
                "ja" => "ja-JP",
                _ => "en-US"
            };

            _resourceContext.QualifierValues["Language"] = tag;
        }

        private readonly Dictionary<string, Dictionary<string, string>> _translations = new()
        {
            ["zh"] = new Dictionary<string, string>
            {
                ["app_title"] = "AutoIME - 输入法自动切换规则配置",
                ["header_title"] = "AutoIME 多输入法自动切换",
                ["header_desc"] = "系统已安装输入法对应下方各标签页。在面板中添加程序后，激活该程序时将自动切换至此输入法。",
                ["lang_label"] = "语言 / Language:",
                ["hotkey_label"] = "唤醒快捷键:",
                ["hotkey_updated"] = "激活快捷键已更新为: {1}",
                ["hotkey_failed"] = "快捷键注册失败 ({1}): {2}",
                ["ime_info"] = "输入法标识: {1} (LangID: 0x{2:X4})  |  已配置 {3} 个应用程序",
                ["btn_capture"] = "🎯 窗口捕获",
                ["btn_browse"] = "📁 浏览EXE",
                ["btn_manual"] = "➕ 手动添加",
                ["col_num"] = "#",
                ["col_exe"] = "程序进程名 (EXE)",
                ["col_desc"] = "程序备注 / 窗口标题说明",
                ["col_action"] = "操作",
                ["btn_delete"] = "❌ 删除选中",
                ["btn_clear"] = "🗑️ 清空本页",
                ["chk_debug"] = "启用悬浮提示 (ToolTip)",
                ["chk_startup"] = "开机自启动",
                ["btn_refresh_ime"] = "🔄 刷新输入法",
                ["btn_save"] = "💾 保存并应用",
                ["btn_minimize"] = "最小化到托盘",
                ["search_placeholder"] = "搜索已配置进程名或备注...",
                ["selected_count"] = "已选中 {1} 项",

                // 快捷键弹窗
                ["hk_dialog_title"] = "⌨️ 设置唤醒快捷键",
                ["hk_dialog_prompt"] = "请直接在键盘上按下想要绑定的按键组合：",
                ["hk_dialog_help"] = "支持组合: Ctrl, Alt, Shift, Win + 任意字母/数字/功能键\n设置后点击【确定】保存生效",
                ["hk_dialog_confirm"] = "确定",
                ["hk_dialog_cancel"] = "取消",
                ["hk_dialog_reset"] = "恢复默认 (Ctrl+Alt+I)",
                ["hk_dialog_empty"] = "请先按下有效的按键组合！",

                // 对话框与提示
                ["save_success"] = "✅ 配置已成功保存并立即生效！",
                ["del_success"] = "已移除选中的程序",
                ["del_select_hint"] = "请先在列表中选中要删除的程序行！",
                ["clear_confirm"] = "确定要清空【{1}】下的所有已配置程序吗？",
                ["clear_title"] = "确认清空",
                ["browse_title"] = "选择需要绑定的可执行程序",
                ["manual_prompt"] = "请输入程序进程文件名（例如 notepad.exe 或 code.exe）：",
                ["manual_title"] = "手动添加程序配置",
                ["manual_exe_placeholder"] = "例如：code.exe",
                ["manual_remark_label"] = "程序备注 (可选)：",
                ["manual_remark_placeholder"] = "例如：Visual Studio Code",
                ["manual_default_remark"] = "手动添加",
                ["already_in_tab"] = "程序 [{1}] 已在当前输入法【{2}】列表中！",
                ["conflict_prompt"] = "程序 [{1}] 已经被绑定在【{2}】中。\n\n是否将其转移到当前的【{3}】？",
                ["conflict_title"] = "规则冲突",
                ["add_success"] = "✅ 成功绑定: {1} -> {2}",

                // 窗口捕获模式
                ["capture_mode_title"] = "🎯 【AutoIME 窗口捕获模式】",
                ["capture_target_ime"] = "目标输入法: {1}",
                ["capture_hover_proc"] = "当前指向进程: {1}",
                ["capture_hover_title"] = "当前窗口标题: {1}",
                ["capture_no_title"] = "(无标题)",
                ["capture_hint_confirm"] = "👉 操作：[点击鼠标左键] 或按 [空格/回车/F8] 立即捕获",
                ["capture_hint_cancel"] = "👉 取消：按 [ESC 键] 退出捕获",
                ["capture_scanning"] = "🎯 【窗口捕获中】请指向目标窗口并点击 (按 ESC 取消)",
                ["capture_invalid"] = "未获取到有效窗口！",
                ["capture_self_window"] = "您点击了 AutoIME 自身的配置窗口，捕获已取消。",
                ["capture_failed_proc"] = "无法读取该窗口的进程名称！",
                ["capture_failed_title"] = "捕获失败",
                ["capture_cancelled"] = "已取消窗口捕获。",

                // 托盘与系统
                ["startup_enabled"] = "已开启开机自启",
                ["startup_disabled"] = "已关闭开机自启",
                ["startup_failed"] = "设置自启动失败: {1}",
                ["ime_reloaded"] = "输入法列表已重新加载！",
                ["tray_open_config"] = "⚙️ 打开配置界面 ({1})",
                ["tray_reload_config"] = "🔄 重新加载配置",
                ["tray_toggle_debug"] = "💬 切换悬浮提示 (ToolTip)",
                ["tray_toggle_startup"] = "🚀 开机自启动",
                ["tray_restart"] = "⚡ 重启 AutoIME",
                ["tray_exit"] = "❌ 退出 AutoIME",
                ["tray_tip_minimized"] = "AutoIME 已最小化到系统托盘，正在后台持续监听。\n双击托盘图标或按唤醒快捷键可重新打开配置。",
                ["debug_enabled"] = "调试悬浮提示已开启",
                ["debug_disabled"] = "调试悬浮提示已关闭",
                ["desc_dev_tool"] = "开发工具",
                ["desc_chat_tool"] = "即时通讯",

                // 主题
                ["theme_system"] = "💻 跟随系统",
                ["theme_light"] = "☀️ 浅色模式",
                ["theme_dark"] = "🌙 深色模式"
            },
            ["en"] = new Dictionary<string, string>
            {
                ["app_title"] = "AutoIME - Input Method Auto Switcher Configuration",
                ["header_title"] = "AutoIME Automatic Input Method Switcher",
                ["header_desc"] = "Tabs below correspond to installed system IMEs. Add applications to a panel to automatically switch to that IME when activated.",
                ["lang_label"] = "Language:",
                ["hotkey_label"] = "Hotkey:",
                ["hotkey_updated"] = "Hotkey updated to: {1}",
                ["hotkey_failed"] = "Failed to register hotkey ({1}): {2}",
                ["ime_info"] = "IME: {1} (LangID: 0x{2:X4})  |  Configured: {3} apps",
                ["btn_capture"] = "🎯 Capture Window",
                ["btn_browse"] = "📁 Browse EXE",
                ["btn_manual"] = "➕ Add Manual",
                ["col_num"] = "#",
                ["col_exe"] = "Process Name (EXE)",
                ["col_desc"] = "Description / Window Title",
                ["col_action"] = "Action",
                ["btn_delete"] = "❌ Delete Selected",
                ["btn_clear"] = "🗑️ Clear Tab",
                ["chk_debug"] = "Enable ToolTip hints",
                ["chk_startup"] = "Run on Windows Startup",
                ["btn_refresh_ime"] = "🔄 Refresh IMEs",
                ["btn_save"] = "💾 Save & Apply",
                ["btn_minimize"] = "Minimize to Tray",
                ["search_placeholder"] = "Search configured processes or remarks...",
                ["selected_count"] = "Selected {1} items",

                ["hk_dialog_title"] = "⌨️ Set Activation Hotkey",
                ["hk_dialog_prompt"] = "Press the new key combination directly on your keyboard:",
                ["hk_dialog_help"] = "Supports: Ctrl, Alt, Shift, Win + any Key\nClick [Confirm] to apply and save",
                ["hk_dialog_confirm"] = "Confirm",
                ["hk_dialog_cancel"] = "Cancel",
                ["hk_dialog_reset"] = "Reset Default (Ctrl+Alt+I)",
                ["hk_dialog_empty"] = "Please press a valid key combination first!",

                ["save_success"] = "✅ Configuration saved and applied immediately!",
                ["del_success"] = "Selected application(s) removed.",
                ["del_select_hint"] = "Please select a row in the list first!",
                ["clear_confirm"] = "Are you sure you want to clear all configured apps for [{1}]?",
                ["clear_title"] = "Confirm Clear",
                ["browse_title"] = "Select Executable File",
                ["manual_prompt"] = "Enter process executable name (e.g. notepad.exe or code.exe):",
                ["manual_title"] = "Add Application Manually",
                ["manual_exe_placeholder"] = "e.g. code.exe",
                ["manual_remark_label"] = "Remark / Description (Optional):",
                ["manual_remark_placeholder"] = "e.g. Visual Studio Code",
                ["manual_default_remark"] = "Manually added",
                ["already_in_tab"] = "Application [{1}] is already configured for [{2}]!",
                ["conflict_prompt"] = "Application [{1}] is currently assigned to [{2}].\n\nDo you want to move it to [{3}]?",
                ["conflict_title"] = "Rule Conflict",
                ["add_success"] = "✅ Successfully bound: {1} -> {2}",

                ["capture_mode_title"] = "🎯 [AutoIME Window Capture Mode]",
                ["capture_target_ime"] = "Target IME: {1}",
                ["capture_hover_proc"] = "Hovered Process: {1}",
                ["capture_hover_title"] = "Window Title: {1}",
                ["capture_no_title"] = "(No Title)",
                ["capture_hint_confirm"] = "👉 Action: [Left Click] or press [Space/Enter/F8] to capture",
                ["capture_hint_cancel"] = "👉 Cancel: Press [ESC] to exit",
                ["capture_scanning"] = "🎯 [Capturing] Point at target window and click (Press ESC to cancel)",
                ["capture_invalid"] = "Could not get a valid target window!",
                ["capture_self_window"] = "You selected AutoIME itself. Capture cancelled.",
                ["capture_failed_proc"] = "Could not read process name for this window!",
                ["capture_failed_title"] = "Capture Failed",
                ["capture_cancelled"] = "Window capture cancelled.",

                ["startup_enabled"] = "Launch on startup enabled",
                ["startup_disabled"] = "Launch on startup disabled",
                ["startup_failed"] = "Failed to configure startup: {1}",
                ["ime_reloaded"] = "IME list reloaded successfully!",
                ["tray_open_config"] = "⚙️ Open Settings ({1})",
                ["tray_reload_config"] = "🔄 Reload Config",
                ["tray_toggle_debug"] = "💬 Toggle ToolTip Hints",
                ["tray_toggle_startup"] = "🚀 Run on Startup",
                ["tray_restart"] = "⚡ Restart AutoIME",
                ["tray_exit"] = "❌ Exit AutoIME",
                ["tray_tip_minimized"] = "AutoIME is minimized to tray and running in background.\nDouble-click tray icon or press hotkey to open settings.",
                ["debug_enabled"] = "ToolTip hints enabled",
                ["debug_disabled"] = "ToolTip hints disabled",
                ["desc_dev_tool"] = "Development Tool",
                ["desc_chat_tool"] = "Communication App",

                ["theme_system"] = "💻 Follow System",
                ["theme_light"] = "☀️ Light",
                ["theme_dark"] = "🌙 Dark"
            },
            ["ja"] = new Dictionary<string, string>
            {
                ["app_title"] = "AutoIME - 入力方式自動切替ツールの設定",
                ["header_title"] = "AutoIME 入力方式自動切替",
                ["header_desc"] = "検出された入力方式が下のタブに表示されます。アプリを追加すると、そのアプリが前面になったとき自動的に切り替わります。",
                ["lang_label"] = "言語 / Language:",
                ["hotkey_label"] = "起動キー:",
                ["hotkey_updated"] = "ショートカットキーを更新しました: {1}",
                ["hotkey_failed"] = "ホットキーの登録に失敗しました ({1}): {2}",
                ["ime_info"] = "IME識別子: {1} (LangID: 0x{2:X4})  |  設定済みアプリ: {3} 個",
                ["btn_capture"] = "🎯 ウィンドウ取得",
                ["btn_browse"] = "📁 EXE参照",
                ["btn_manual"] = "➕ 手動追加",
                ["col_num"] = "#",
                ["col_exe"] = "プロセス名 (EXE)",
                ["col_desc"] = "メモ / ウィンドウタイトル",
                ["col_action"] = "操作",
                ["btn_delete"] = "❌ 選択項目を削除",
                ["btn_clear"] = "🗑️ タブ内を全消去",
                ["chk_debug"] = "フローティング通知 (ToolTip) を有効化",
                ["chk_startup"] = "Windows 起動時に自動実行",
                ["btn_refresh_ime"] = "🔄 入力方式を再読込",
                ["btn_save"] = "💾 保存して適用",
                ["btn_minimize"] = "トレイに最小化",
                ["search_placeholder"] = "プロセス名または備考を検索...",
                ["selected_count"] = "{1} 項目選択中",

                ["hk_dialog_title"] = "⌨️ 起動ホットキーの設定",
                ["hk_dialog_prompt"] = "キーボードで希望のキーの組み合わせを押してください：",
                ["hk_dialog_help"] = "対応修飾キー: Ctrl, Alt, Shift, Win + 任意のキー\n設定後に[確定]をクリックして保存します",
                ["hk_dialog_confirm"] = "確定",
                ["hk_dialog_cancel"] = "キャンセル",
                ["hk_dialog_reset"] = "初期設定に戻す (Ctrl+Alt+I)",
                ["hk_dialog_empty"] = "有効なキーの組み合わせを押してください！",

                ["save_success"] = "✅ 設定が正常に保存され、直ちに適用されました！",
                ["del_success"] = "選択したプログラムを削除しました",
                ["del_select_hint"] = "リストから削除対象の行を選択してください！",
                ["clear_confirm"] = "【{1}】に設定されたすべてのプログラムを削除してもよろしいですか？",
                ["clear_title"] = "全消去の確認",
                ["browse_title"] = "実行可能プログラムの選択",
                ["manual_prompt"] = "実行可能ファイル名を入力してください（例：notepad.exe や code.exe）：",
                ["manual_title"] = "手動でプログラムを追加",
                ["manual_exe_placeholder"] = "例: code.exe",
                ["manual_remark_label"] = "備考 / 説明（任意）：",
                ["manual_remark_placeholder"] = "例: Visual Studio Code",
                ["manual_default_remark"] = "手動追加",
                ["already_in_tab"] = "プログラム [{1}] は既に【{2}】に登録されています！",
                ["conflict_prompt"] = "プログラム [{1}] は既に【{2}】に割り当てられています。\n\n現在の【{3}】に移動しますか？",
                ["conflict_title"] = "ルールの競合",
                ["add_success"] = "✅ 関連付けに成功しました: {1} -> {2}",

                ["capture_mode_title"] = "🎯 【AutoIME ウィンドウ取得モード】",
                ["capture_target_ime"] = "対象の入力方式: {1}",
                ["capture_hover_proc"] = "カーソル位置のプロセス: {1}",
                ["capture_hover_title"] = "ウィンドウタイトル: {1}",
                ["capture_no_title"] = "(タイトルなし)",
                ["capture_hint_confirm"] = "👉 操作: [左クリック] または [Space/Enter/F8] で取得",
                ["capture_hint_cancel"] = "👉 中止: [ESC] キーでキャンセル",
                ["capture_scanning"] = "🎯 【取得中】対象ウィンドウをクリックしてください (ESCで中止)",
                ["capture_invalid"] = "有効なウィンドウを取得できませんでした！",
                ["capture_self_window"] = "AutoIME 自体の設定ウィンドウが選択されました。中止します。",
                ["capture_failed_proc"] = "このウィンドウのプロセス名を取得できませんでした！",
                ["capture_failed_title"] = "取得失敗",
                ["capture_cancelled"] = "ウィンドウ取得をキャンセルしました。",

                ["startup_enabled"] = "スタートアップを有効にしました",
                ["startup_disabled"] = "スタートアップを無効にしました",
                ["startup_failed"] = "自動起動の設定に失敗しました: {1}",
                ["ime_reloaded"] = "入力方式の一覧を再読み込みしました！",
                ["tray_open_config"] = "⚙️ 設定画面を開く ({1})",
                ["tray_reload_config"] = "🔄 設定を再読込",
                ["tray_toggle_debug"] = "💬 フローティング通知の切替",
                ["tray_toggle_startup"] = "🚀 スタートアップ自動実行",
                ["tray_restart"] = "⚡ AutoIME を再起動",
                ["tray_exit"] = "❌ AutoIME を終了",
                ["tray_tip_minimized"] = "AutoIME はシステムトレイに最小化され、バックグラウンドで監視中です。\nダブルクリックまたはホットキーで設定画面を開けます。",
                ["debug_enabled"] = "フローティング通知を有効にしました",
                ["debug_disabled"] = "フローティング通知を無効にしました",
                ["desc_dev_tool"] = "開発ツール",
                ["desc_chat_tool"] = "チャットツール",

                ["theme_system"] = "💻 システムに従う",
                ["theme_light"] = "☀️ ライト",
                ["theme_dark"] = "🌙 ダーク"
            }
        };

        public string Get(string key, params object[] args)
        {
            string? val = null;

            // 1. Always prioritize internal dictionary matching user-selected language
            var lang = CurrentLanguage;
            if (string.IsNullOrEmpty(lang) || !_translations.ContainsKey(lang))
                lang = "zh";

            if (_translations.TryGetValue(lang, out var dict) && dict.TryGetValue(key, out var localVal) && !string.IsNullOrEmpty(localVal))
            {
                val = localVal;
            }

            // 2. Fallback to Windows App SDK MRT Core (PRI / .resw)
            if (string.IsNullOrEmpty(val) && _resourceMap != null && _resourceContext != null)
            {
                try
                {
                    var candidate = _resourceMap.TryGetValue(key, _resourceContext);
                    if (candidate != null && !string.IsNullOrEmpty(candidate.ValueAsString))
                    {
                        val = candidate.ValueAsString;
                    }
                    else if (_resourceManager != null)
                    {
                        var fullCandidate = _resourceManager.MainResourceMap.TryGetValue($"Resources/{key}", _resourceContext);
                        if (fullCandidate != null && !string.IsNullOrEmpty(fullCandidate.ValueAsString))
                        {
                            val = fullCandidate.ValueAsString;
                        }
                    }
                }
                catch
                {
                    // Fallback to internal dictionary
                }
            }

            // 3. Fallback to English dictionary or raw key
            if (string.IsNullOrEmpty(val))
            {
                if (_translations["en"].TryGetValue(key, out var enVal))
                    val = enVal;
                else
                    val = key;
            }

            // 4. Format placeholders {1}, {2}...
            if (args != null && args.Length > 0)
            {
                for (int i = 0; i < args.Length; i++)
                {
                    val = val.Replace($"{{{i + 1}}}", args[i]?.ToString() ?? "");
                }
            }

            return val;
        }

        public string this[string key] => Get(key);
        public string this[string key, params object[] args] => Get(key, args);

        partial void OnCurrentLanguageChanged(string value)
        {
            UpdateResourceContextLanguage();
            OnPropertyChanged(string.Empty); // Notify all bindings to refresh
        }
    }
}

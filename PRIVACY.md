# Privacy Policy for AutoIME (隐私政策)

**Last updated (最后更新时间):** 2026-11-08

---

## English

### 1. Overview
AutoIME is an open-source, local-only desktop utility designed to automatically switch keyboard layouts and input method editors (IMEs) based on the active foreground window in Microsoft Windows.

### 2. Information Collection and Use
- **No Personal Data Collected:** AutoIME does not collect, record, log, or transmit any personal identifiable information, keystrokes, clipboard content, passwords, or browsing history.
- **Local Operation:** All features operate 100% locally and offline on your computer. Configuration settings (such as hotkeys, application rules, and theme preferences) are stored strictly on your local disk in the `AutoIME_config.ini` file located in the application directory.
- **No Network Communication:** AutoIME does not contain telemetry, background analytics, external tracking services, or remote network access capabilities.

### 3. Permissions and System Access
AutoIME requires standard Windows API permissions to function:
- **Foreground Window Detection:** Uses standard Win32 `SetWinEventHook` to detect when you switch application windows in order to match your configured IME rules.
- **IMM32 Input Management:** Calls official Windows IMM32 interfaces to switch the active input language for the targeted window.
- **Global Hotkeys:** Uses `RegisterHotKey` to allow you to bring up the settings interface using your custom key combination.

None of these system permissions are used to monitor or intercept user input.

### 4. Third-Party Services
AutoIME does not integrate with any third-party analytics, advertising, or cloud storage providers.

### 5. Changes to This Privacy Policy
If we update this policy, any changes will be reflected directly in this file in the open-source repository.

### 6. Contact
If you have any questions or concerns about this privacy policy, please open an issue in the project repository.

---

## 简体中文

### 1. 概述
AutoIME 是一款本地离线运行的开源输入法与键盘布局自动切换辅助工具，用于在 Windows 系统中根据当前活动前台窗口自动激活对应的输入法。

### 2. 信息收集与使用
- **不收集任何个人数据：** AutoIME 绝不收集、记录、存储或传输任何用户的个人身份信息、键盘敲击记录、剪贴板内容、账号密码或任何隐私数据。
- **纯本地离线运行：** 软件所有功能 100% 运行于用户本地计算机，规则配置（如按键偏好、应用对应列表与界面主题）仅保存在用户本地磁盘的 `AutoIME_config.ini` 文件中。
- **无网络通信：** 软件内部不包含任何数据埋点、用户行为分析、遥测或远程网络传输功能。

### 3. 系统权限与接口使用说明
AutoIME 需要以下基础 Windows API 接口以实现其核心功能：
- **前台窗口感知：** 使用 Win32 `SetWinEventHook` 监听窗口切换事件，以便匹配您配置的输入法规则；
- **IMM32 输入法管理：** 调用系统标准的 IMM32 接口向活动窗口发送切换输入法消息；
- **全局快捷键：** 使用 `RegisterHotKey` 注册用户自定义的唤醒热键。

以上接口仅用于进程名称比对与输入法指令发送，绝不截获或窥探用户的击键内容。

### 4. 第三方服务
本软件未集成任何第三方广告、统计或数据分析 SDK。

### 5. 联系方式
如果您对本隐私政策有任何疑问，请通过开源仓库提交 Issue 沟通。


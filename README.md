# ClipboardTranslator — Windows 剪贴板中英翻译助手

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4)](https://dotnet.microsoft.com/)
[![Platform](https://img.shields.io/badge/Platform-Windows_10%2F11-0078D6)]()

Windows 系统托盘常驻的中英文剪贴板翻译助手：复制文本，按下全局热键，即可通过 DeepSeek（OpenAI 兼容接口）完成翻译，结果以系统通知显示。

A lightweight Windows tray app for instant Chinese–English clipboard translation. Copy text, press a global hotkey, and get the translation right away via DeepSeek (OpenAI-compatible API), shown as a Windows notification.

## 特性 Features

- **全局热键**：默认 `Ctrl + Alt + Shift + T`，可在设置中自定义组合
- **自动识别语言方向**：本地判断中文/英文比例，不浪费 Token
- **保留代码标识符**：函数名、变量名、路径、URL、数字等在翻译中保持不变
- **结构化返回**：`temperature=0` + JSON 输出，结果稳定
- **API Key 加密存储**：Windows DPAPI（CryptProtectData），配置文件中无明文
- **零轮询**：空闲时 CPU 占用接近 0，不监听剪贴板变化
- **单实例**：重复启动自动唤醒已有进程
- **单文件便携**：支持单文件自包含发布（阶段3）

- Global hotkey (default `Ctrl + Alt + Shift + T`, fully customizable)
- Local language-direction detection (Chinese/English), no wasted tokens
- Preserves code identifiers, paths, URLs and numbers during translation
- Stable structured output: `temperature=0` with JSON response format
- API key protected with Windows DPAPI — never stored in plaintext
- Near-zero idle CPU: no polling, no clipboard monitoring
- Single-instance guard with existing-process activation
- Portable single-file publish supported (Phase 3)

## 修复记录 Release Notes

### v1.0.1

- 修复单个中文或英文词汇触发翻译时，模型可能返回“你好！请问有什么可以帮你的？”等普通聊天回复的问题。
- 修复模型首次响应格式异常时，重试请求丢失原始翻译方向提示词的问题；重试时现在仍会明确要求执行中译英或英译中，并返回 JSON。

### v1.0.1

- Fixed an issue where translating certain single Chinese or English words could display a generic assistant greeting instead of a translation.
- Fixed the retry path losing the original translation-direction instruction after a malformed model response; retries now preserve the Chinese↔English translation instruction while still requiring JSON output.

## 快速开始 Quick Start

### 1. 构建 Build

```bash
# 需要 .NET 8 SDK
dotnet restore
dotnet build -c Release

# 可选：单文件自包含发布
dotnet publish src/ClipboardTranslator -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
```

### 2. 配置 Configuration

首次运行后，配置文件位于：

```
%LOCALAPPDATA%\ClipboardTranslator\config.json
```

| 字段 Field | 说明 Description | 默认 Default |
|---|---|---|
| `baseUrl` | LLM 服务地址 Base URL | `https://api.deepseek.com` |
| `model` | 模型名称 Model name | `deepseek-chat` |
| `apiKeyEncrypted` | DPAPI 加密后的 API Key | — |
| `timeoutSeconds` | 请求超时（秒）Timeout (s) | `15` |
| `maxCharacters` | 最大文本长度 Max text length | `8000` |
| `chineseDetectionThreshold` | 中文识别阈值 Threshold | `0.1` |
| `hotkey` | 热键组合 Hotkey combo | `Ctrl+Alt+Shift+T` |

> **安全提示**：`apiKeyEncrypted` 由 DPAPI 加密，与当前 Windows 用户和本机绑定，配置文件中不会出现明文 API Key。建议在托盘菜单 → 设置 中填写 API Key，程序会自动加密保存。

> **Security note**: `apiKeyEncrypted` is protected by DPAPI and bound to the current Windows user and machine. The plaintext key never appears in the config file. Fill in your key via Tray Menu → Settings and it will be encrypted automatically.

### 3. 使用 Usage

1. 启动程序（驻留系统托盘，无窗口）
2. 复制任意中文或英文文本
3. 按 `Ctrl + Alt + Shift + T`（或托盘菜单 → 翻译当前剪贴板）
4. 翻译结果显示为 Windows 通知

1. Launch the app (runs in the system tray, no main window)
2. Copy any Chinese or English text
3. Press `Ctrl + Alt + Shift + T` (or Tray Menu → Translate clipboard)
4. The translation appears as a Windows notification

## 技术栈 Tech Stack

- C# / .NET 8 / WinForms
- Win32 `RegisterHotKey`（全局热键）
- `System.Text.Json`（结构化输出解析）
- Windows DPAPI（密钥保护）
- DeepSeek API（OpenAI Chat Completions 兼容接口）

## 测试 Tests

```bash
dotnet test
```

覆盖：语言方向检测、提示词构建、JSON 容错解析（含 Markdown 代码块清理）、DPAPI 加解密往返。

Covers: language detection, prompt building, resilient JSON parsing (incl. Markdown fence cleanup), and DPAPI round-trip.

## 隐私 Privacy

- 仅在你按热键或点击"翻译当前剪贴板"时才读取剪贴板并发送请求
- 不保存剪贴板历史、不记录翻译内容、不监听剪贴板变化
- 日志不包含 API Key、剪贴板原文或翻译结果

- Clipboard is read and requests are sent only when you trigger translation
- No history, no content logging, no clipboard monitoring
- Logs never contain API keys, source text or translated results

## 路线图 Roadmap

- [x] 阶段1：最小可运行（托盘 + 热键 + 翻译 + 通知）
- [x] 阶段2：设置窗口、DPAPI 密钥存储
- [ ] 阶段3：开机启动、完整结果窗口、单文件发布
- Phase 1: Minimal viable app (tray + hotkey + translation + notification)
- Phase 2: Settings window, DPAPI secret storage
- Phase 3: Auto-start, full-result window, single-file publish

## 许可证 License

MIT（以仓库 LICENSE 文件为准）

See [LICENSE](LICENSE) for details.

# 音乐桌面 · MusicDesktop

把「桌面壳」和「全屏歌词播放器」接在一起的 Windows 桌面音乐层：桌面上常驻一条歌词、一个迷你播放器、一条 Dock。

界面用 C# / .NET 9 / WPF 原生实现，不依赖 Electron，安装包体积按 MB 计。

![platform](https://img.shields.io/badge/platform-Windows%2010%2F11-0078D4)
![dotnet](https://img.shields.io/badge/.NET-9.0-512BD4)

## 它解决什么

GlassBoard 做桌面壳（Dock / 启动台 / 桌面组件），Folia 做全屏歌词播放器，两者在 Windows 上唯一的咬合点是 **SMTC（系统媒体会话）** 和 **Folia 的本地 HTTP 接口**。
「音乐桌面」就是把这两条缝补起来的那一层：

- 从 **SMTC** 拿当前曲目、艺人、封面、播放进度、播放状态；
- 从 **Folia 本地接口** 拿逐字歌词（`127.0.0.1:32109/v1/lyric`）和播放时间（`127.0.0.1:32107/stage/player/time`）；
- 在桌面上画出 **卡拉 OK 逐字扫光**、迷你播放器、Dock，并把播放控制回送给系统 / Folia。

不 fork、不改它们的源码，只对接公开接口，所以两者升级都不会打乱这里。

## 功能

| 模块 | 说明 |
| --- | --- |
| 桌面歌词 `LyricWindow` | 置顶透明层，逐字扫光，双语/音译，悬停出工具条（锁定穿透、字号、±200ms 校准、隐藏） |
| 迷你播放器 `MiniPlayerWindow` | 封面、标题、艺人、进度条（可点击跳转）、上一首/播放暂停/下一首 |
| Dock `DockWindow` | 玻璃质感启动条，读系统 exe 图标，拖拽排序，右键菜单 |
| 设置 `SettingsWindow` | 主题、字号、透明度、歌词偏移、窗口显隐、开机自启 |
| 托盘 | 显示/隐藏各层、进设置、退出 |
| 全局热键 | `Ctrl+Alt+L` 歌词、`Ctrl+Alt+K` 锁定、`Ctrl+Alt+M` 播放器、`Ctrl+Alt+D` Dock、`Ctrl+Alt+S` 设置 |

### 三级降级（关键设计）

本机可能既没装 Folia、也没在放歌，所以数据源按可用性逐级退：

1. **Folia** — 歌词接口通，用真歌词 + Stage 播放时间；
2. **SMTC** — 只有系统媒体会话，用会话里的曲目信息，歌词按行时间轴对齐；
3. **Demo** — 都没有，`--demo` 给一段演示歌词，界面照常可看可调。

任何一级断掉都不会白屏或崩，只是降级。

## 歌词时间轴

```csharp
lyricSeconds = max(0, playerPositionMs + clipStartMs - offset - userOffsetMs) / 1000.0
```

`clipStartMs` 补片段起点，`offset` 为正表示歌词本身延后所以要减掉，`userOffsetMs` 是界面上 ±200ms 的手动校准。行与字的时间单位是**秒**，这两个偏移是**毫秒**——单位混用是这类接口最容易踩的坑。

## 跑起来

```powershell
dotnet run --project src/MusicDesktop/MusicDesktop.csproj            # 正常启动
dotnet run --project src/MusicDesktop/MusicDesktop.csproj -- --demo   # 无 Folia / 无 SMTC 也要看界面
```

配置落在 `%LOCALAPPDATA%\MusicDesktop\settings.json`，日志同目录 `app.log`。

## 编译 / 打包

```powershell
# 本机
powershell -ExecutionPolicy Bypass -File build.ps1

# 线上（GitHub Actions，Windows runner）
# push 到 main 或手动 workflow_dispatch，产物：MusicDesktop-win-x64.zip
```

线上流程：`restore → build → publish(win-x64, self-contained, single-file) → Compress-Archive → SHA256 → artifact`，
并把 zip 传一份到公开直链，方便没登录 GitHub 的机器直接下载。

## 与两个上游项目的关系

- **GlassBoard**（`llm147369/GlassBoard-Source`，C#/WPF，无开源许可证）：只作为设计参照，代码一行没搬。
- **Folia**（`chthollyphile/folia-major`，AGPL-3.0）：只通过其公开的本地 HTTP 接口通信，不链接、不复制其代码，因此本项目不受 AGPL 传染；Folia 自身的分发仍受 AGPL 约束。

## 许可

MIT © 2026 YG

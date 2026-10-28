# cloud-build

GitHub Actions 云编译工作区。本仓库同时是「音乐桌面」的线上编译入口。

## music-desktop（音乐桌面）

Windows 桌面音乐层：置顶逐字歌词 + 迷你播放器 + Dock，接 Folia 本地接口与系统 SMTC。

- 源码：`music-desktop/`
- 流水线：`.github/workflows/build.yml`（windows-latest / .NET 9）
- 产物：`MusicDesktop-win-x64.zip`（自包含单文件，另附公开直链）
- 触发：push 到 `main` 或手动 `workflow_dispatch`

本地编译见 `music-desktop/build.ps1`。

# PotPlayerMultiControl

**PotPlayer 多窗口同步控制** · **Multi-window playback control for PotPlayer**

Windows Forms 小工具：同时控制多个 [PotPlayer](https://potplayer.daum.net/) 窗口的播放/暂停、快进/后退、回到起始点、进度对齐与显示/最小化。快捷键仅在本程序聚焦时生效。

A Windows Forms utility to control multiple PotPlayer windows at once: play/pause, seek, jump to start, timeline alignment, show/minimize. Shortcuts work only while this app has focus.

**文档 · Docs:** [中文详解](docs/zh.md) · [English guide](docs/en.md)

---

## 功能速览 · Features at a glance

- 自动发现 PotPlayer 窗口，批量播放/暂停、快进/后退、回到起始点、显示或最小化
- 进度对齐（主窗口 + 帧偏移，帧边界量化）与同步锁（播放中微调漂移）
- 窗体内左手快捷键（非全局）；控制窗口默认置顶
- Auto-discover PotPlayer windows; batch play/pause, skip, jump to start, show/minimize
- Timeline align (primary + frame offset) and sync lock (nudge drift while playing)
- In-window left-hand shortcuts (not global); control window topmost by default

## 快捷键 · Shortcuts

仅本窗口聚焦时生效 · Active only while this app is focused.

| 功能 / Action | 按键 / Key |
| --- | --- |
| 后退 / Skip back | `A` |
| 播放/暂停 / Play·pause | `S` |
| 快进 / Skip forward | `D` |
| 回到起始点 / Jump to start | `Q` |
| 对齐进度 / Align | `W` |
| 显示全部 / Show all | `E` |
| 最小化全部 / Minimize all | `R` |

## 快速开始 · Quick start

需要 Windows 与 [.NET 8 SDK](https://dotnet.microsoft.com/download) 或更高。

```powershell
dotnet build
dotnet run
```

发布独立单文件（示例输出到 `out`）：

```powershell
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o out
```

GitHub Release：把带工作流的提交推到 `main` 后，打 SemVer tag 并推送即可自动构建并上传 `win-x64` 单文件 zip（例如 `v0.1.0`；预发布用 `v0.2.0-beta.1`）。

```powershell
git tag v0.1.0
git push origin v0.1.0
```

更多用法、截图占位、日志路径与实现说明见 [docs/zh.md](docs/zh.md) / [docs/en.md](docs/en.md)。

## 贡献与许可 · Contributing & license

欢迎提交 issue 或 pull request。本项目采用 [MIT](LICENSE) 许可证。

Issues and PRs welcome. Licensed under the [MIT License](LICENSE).

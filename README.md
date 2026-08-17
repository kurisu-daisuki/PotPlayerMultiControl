# PotPlayer 多窗口控制 (PotPlayerMultiControl)

**简介**
- **项目**: 这是一个基于 Windows Forms 的小工具，用于同时控制多个 PotPlayer 窗口的播放/暂停、快进/后退、回到起始点，以及显示或最小化全部窗口。支持全局热键、管理员窗口识别与提权重启。实现见 [MainForm.cs](MainForm.cs)。

**功能**
- **自动发现**: 自动检测运行中的 PotPlayer 窗口（进程名包含 `PotPlayerMini` 变种）。
- **批量控制**: 一键向所有发现的窗口发送播放/暂停、快进/后退、回到起始点、显示或最小化。
- **快进/后退**: 默认按 5 秒跳转，可在界面修改时间跨度（1–600 秒），设置会记住。
- **全局热键**: 左手按住 `Ctrl+Alt`，右手用 `J/K/L` 控制后退、播放/暂停、快进；`H` 回到起始点，`PageUp`/`PageDown` 显示/最小化全部。
- **管理员窗口**: 列表中标记 `[管理员]`；普通权限无法控制时，可点击「以管理员身份重启」。
- **控制窗口置顶**: 可切换本程序是否始终位于最上层。
- **折叠面板**: 窗口列表与命令栏默认收起，点击标题即可展开或折叠。
- **日志**: 在本地应用数据目录记录运行日志，用于排查与审核。

**先决条件**
- **操作系统**: Windows（需要 Win32 窗口消息支持）。
- **SDK**: 安装 .NET 8 SDK 或更高（目标框架见项目文件）。查看项目文件: [PotPlayerMultiControl.csproj](PotPlayerMultiControl.csproj)

**构建与运行**
在仓库根目录（包含 `PotPlayerMultiControl.csproj`）中运行：

```powershell
dotnet build
dotnet run
```

或者发布为独立可执行文件（示例，发布到 `out` 目录）：

```powershell
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o out
```

**使用说明**
- 启动后为紧凑图标工具栏，鼠标悬停可查看功能与热键；窗口列表和命令栏默认折叠。
- 点击“播放/暂停全部”或使用 `Ctrl+Alt+K`，向每个 PotPlayer 窗口发送媒体播放/暂停。
- 点击“后退/快进”或使用 `Ctrl+Alt+J` / `Ctrl+Alt+L`，按当前时间跨度跳转；可在「时间跨度(秒)」中修改，默认 5 秒。
- 点击“回到起始点”或使用 `Ctrl+Alt+H`，将全部视频跳到文件开头。
- 点击“显示全部”或使用 `Ctrl+Alt+PageUp`，还原全部 PotPlayer 窗口并将其置于最上层（置顶，不抢焦点）。
- 点击“最小化全部”或使用 `Ctrl+Alt+PageDown`，最小化全部 PotPlayer 窗口。
- 点击“置顶控制窗口”让本程序始终位于最上层，再点一次可取消。
- 点击“刷新列表”以重新扫描当前窗口。
- 若列表中出现 `[管理员]` 且命令失败，点击“以管理员身份重启”。

**日志位置**
- 日志文件位于 `%LocalAppData%\PotPlayerMultiControl\app.log`，用于记录操作与错误信息。

**实现要点**
- 使用 Win32 API 枚举窗口并通过进程名过滤 PotPlayer（支持 `PotPlayerMini64/ PotPlayerMini/ PotPlayerMini32`）。
- 通过发送 `WM_APPCOMMAND`（`APPCOMMAND_MEDIA_PLAY_PAUSE`）实现播放/暂停控制。
- 通过 `WM_COMMAND` `10243` 将视频跳到文件开头。
- 通过 PotPlayer `WM_USER` 命令 `0x5004`/`0x5005` 读取并设置播放进度，实现自定义秒数的快进/后退。
- 通过 `ShowWindow` / `SetWindowPos(HWND_TOPMOST)` 还原并置顶全部 PotPlayer 窗口；最小化时取消置顶。

**贡献与许可**
- 欢迎贡献：提交 issue 或 pull request，保持变更小且有说明。
- 当前仓库未指定许可证：如需发布请添加合适的 `LICENSE` 文件。

---

如需我为 README 增加屏幕截图、打包脚本或发布说明，告诉我想要的格式和目标平台，我会继续补充。

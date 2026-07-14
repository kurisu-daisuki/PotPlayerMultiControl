# PotPlayer 多窗口控制 (PotPlayerMultiControl)

**简介**
- **项目**: 这是一个基于 Windows Forms 的小工具，用于向系统中所有运行的 PotPlayer 窗口发送播放/暂停命令（媒体播放/暂停），支持全局热键与图形界面。查看实现: [PotPlayerMultiControl/Program.cs](PotPlayerMultiControl/Program.cs)

**功能**
- **自动发现**: 自动检测运行中的 PotPlayer 窗口（进程名包含 `PotPlayerMini` 变种）。
- **批量控制**: 一键向所有发现的 PotPlayer 窗口发送播放/暂停命令。
- **全局热键**: 默认注册 `Ctrl+Alt+Space` 为播放/暂停快捷键。
- **日志**: 在本地应用数据目录记录运行日志，用于排查与审核。

**先决条件**
- **操作系统**: Windows（需要 Win32 窗口消息支持）。
- **SDK**: 安装 .NET 8 SDK 或更高（目标框架见项目文件）。查看项目文件: [PotPlayerMultiControl/PotPlayerMultiControl.csproj](PotPlayerMultiControl/PotPlayerMultiControl.csproj)

**构建与运行**
在项目根（包含 `PotPlayerMultiControl.csproj`）的父目录中运行：

```powershell
dotnet build PotPlayerMultiControl\PotPlayerMultiControl.csproj
dotnet run --project PotPlayerMultiControl\PotPlayerMultiControl.csproj
```

或者发布为独立可执行文件（示例，发布到 `out` 目录）：

```powershell
dotnet publish PotPlayerMultiControl\PotPlayerMultiControl.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o out
```

**使用说明**
- 启动应用后，窗口会列出检测到的 PotPlayer 窗口。
- 点击“播放/暂停全部”按钮或使用全局热键 `Ctrl+Alt+Space`，程序会向每个 PotPlayer 窗口发送媒体播放/暂停命令（等同于媒体键）。
- 点击“刷新列表”以重新扫描当前窗口。

**日志位置**
- 日志文件位于 `%LocalAppData%\PotPlayerMultiControl\app.log`，用于记录操作与错误信息。

**实现要点**
- 使用 Win32 API 枚举窗口并通过进程名过滤 PotPlayer（支持 `PotPlayerMini64/ PotPlayerMini/ PotPlayerMini32`）。
- 通过发送 `WM_APPCOMMAND`（`APPCOMMAND_MEDIA_PLAY_PAUSE`）实现播放/暂停控制。

**贡献与许可**
- 欢迎贡献：提交 issue 或 pull request，保持变更小且有说明。
- 当前仓库未指定许可证：如需发布请添加合适的 `LICENSE` 文件。

---

如需我为 README 增加屏幕截图、打包脚本或发布说明，告诉我想要的格式和目标平台，我会继续补充。

# PotPlayer 多窗口控制

实现见 [MainForm.cs](../MainForm.cs)。返回 [README](../README.md) · [English](en.md)

## 关键词

PotPlayer 多窗口控制、多实例同步播放、进度对齐、帧级对齐、帧偏移、同步锁、批量播放暂停、快进后退、回到起始点、显示全部窗口、最小化全部、左手单手快捷键、非全局热键、Win32 窗口消息、WM_APPCOMMAND、管理员提权、窗口置顶、Windows Forms、.NET 8。

常见用途：同一台电脑上开多个 PotPlayer Mini，对比剪辑、核对 A/B 素材，或让相关片段保持同步。

## 界面预览（截图占位）

将 PNG 放到 `docs/screenshots/` 后即可显示。建议宽度约 800–1200px。

| 说明 | 占位路径 |
| --- | --- |
| 紧凑工具栏（悬停提示快捷键） | [screenshots/zh-toolbar.png](screenshots/zh-toolbar.png) |
| 展开窗口列表 + 帧偏移 / 帧率 | [screenshots/zh-window-list.png](screenshots/zh-window-list.png) |
| 对齐进度 / 同步锁状态 | [screenshots/zh-align-sync.png](screenshots/zh-align-sync.png) |

![紧凑工具栏（占位：替换为实际截图）](screenshots/zh-toolbar.png)

![窗口列表与帧偏移（占位：替换为实际截图）](screenshots/zh-window-list.png)

![对齐与同步锁（占位：替换为实际截图）](screenshots/zh-align-sync.png)

## 功能

- **自动发现**：检测运行中的 PotPlayer 窗口（进程名包含 `PotPlayerMini` 变种）。
- **批量控制**：一键向所有窗口发送播放/暂停、快进/后退、回到起始点、显示或最小化。
- **快进/后退**：默认 5 秒，可在界面改为 1–600 秒；设置会记住。
- **对齐进度**：以列表中的**主窗口**（第一项）为基准，其余窗口 seek 到「基准时间 + 该窗口帧偏移」，并量化到帧边界。
- **帧偏移**：展开窗口列表后，可为非主窗口设置相对主窗口的帧偏移（可正可负），并选择假定帧率（24/25/30/50/60/120）。按窗口标题持久化。
- **同步锁**：对齐后可保持开启，定时监测相对进度；偏差达到约 1 帧时对偏离窗口微调。可随时关闭。
- **窗体内快捷键**：本程序聚焦时左手单手即可操作（见下表）。未聚焦时不拦截其他软件按键。
- **管理员窗口**：列表中标记 `管理员`；普通权限无法控制时，可「以管理员身份重启」。
- **控制窗口置顶**：启动后默认置顶，可切换。
- **折叠面板**：窗口列表与命令栏默认收起，点击标题展开或折叠。
- **日志**：写入本地应用数据目录，便于排查。

## 快捷键

仅本窗口聚焦，且焦点不在秒数 / 帧率 / 帧偏移输入框时生效。

| 功能 | 按键 |
| --- | --- |
| 后退 | `A` |
| 播放/暂停 | `S` |
| 快进 | `D` |
| 回到起始点 | `Q` |
| 对齐进度 | `W` |
| 显示全部 | `E` |
| 最小化全部 | `R` |

## 先决条件

- **操作系统**：Windows（需要 Win32 窗口消息）。
- **SDK**：.NET 8 SDK 或更高（目标框架见 [PotPlayerMultiControl.csproj](../PotPlayerMultiControl.csproj)）。

## 构建与运行

在仓库根目录（含 `PotPlayerMultiControl.csproj`）执行：

```powershell
dotnet build
dotnet run
```

发布为独立单文件（示例输出到 `out`）：

```powershell
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o out
```

## 使用说明

- 启动后为紧凑图标工具栏，鼠标悬停可查看功能与快捷键；窗口列表和运行日志默认折叠。控制窗口默认置顶，便于切回后单手操作。
- 「播放/暂停全部」或 `S`：向每个 PotPlayer 发送媒体播放/暂停。
- 「后退/快进」或 `A` / `D`：按当前时间跨度跳转；可在「秒」框修改，默认 5 秒。
- 「回到起始点」或 `Q`：全部视频跳到文件开头。
- 「对齐进度」或 `W`：各窗口对齐到主窗口进度（含帧偏移）。无窗口或仅一个窗口时会提示，不会崩溃。
- 展开「窗口列表」：第一项为主窗口；选中其他窗口后设置「相对主窗口」帧偏移。约定：正数表示该窗口画面比主窗口更靠后（同一时刻进度更大）。例如 A 比主窗口 B 快 240 帧时，A 填 `+240`。
- 同步锁图标：开关播放中自动微调。关闭后不再自动 seek。
- 「显示全部」或 `E`：还原全部 PotPlayer 并置于最上层（置顶，不抢焦点）。
- 「最小化全部」或 `R`：最小化全部 PotPlayer。
- 「置顶控制窗口」：取消或重新置顶本程序。
- 「刷新列表」：重新扫描当前窗口。
- 列表出现 `管理员` 且命令失败时，使用「以管理员身份重启」。

## 日志与设置位置

- 日志：`%LocalAppData%\PotPlayerMultiControl\app.log`
- 设置：`%LocalAppData%\PotPlayerMultiControl\settings.txt`（时间跨度、帧率、同步锁、各窗口帧偏移）

## 实现要点

- Win32 枚举窗口，按进程名过滤 PotPlayer（`PotPlayerMini64` / `PotPlayerMini` / `PotPlayerMini32`）。
- `WM_APPCOMMAND`（`APPCOMMAND_MEDIA_PLAY_PAUSE`）播放/暂停。
- `WM_COMMAND` `10243` 跳到文件开头。
- PotPlayer `WM_USER` `0x5004` / `0x5005` 读取并设置播放进度（自定义秒数快进/后退与对齐）。
- 对齐时按假定帧率将目标时间量化到帧边界；回读误差超约 1 帧时短延迟重试。
- 同步锁约 300ms 轮询；偏差达到 1 帧且经过冷却后才微调，避免频繁 seek。
- `ShowWindow` / `SetWindowPos(HWND_TOPMOST)` 还原并置顶全部 PotPlayer；最小化时取消置顶。
- 快捷键走窗体 `ProcessCmdKey`，不注册系统全局热键。

## 贡献与许可

欢迎提交 issue 或 pull request，变更宜小且附说明。本项目采用 [MIT](../LICENSE) 许可证。

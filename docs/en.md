# PotPlayer multi-window control

Implementation: [MainForm.cs](../MainForm.cs). Back to [README](../README.md) · [中文](zh.md)

## Keywords

PotPlayer multi-window control, multi-instance sync playback, timeline alignment, frame-accurate seek, frame offset, sync lock / drift correction, batch play/pause, skip forward/back, jump to start, show all / minimize all, left-hand one-handed shortcuts, window-scoped hotkeys (not global), Win32 window messages, WM_APPCOMMAND, run as administrator, always-on-top toolbar, Windows Forms, .NET 8.

Typical use: several PotPlayer Mini windows (64/32-bit) on one PC — compare cuts, check A/B takes, or keep related clips in step.

## Screenshots (placeholders)

Drop PNGs into `docs/screenshots/` using the names below. Suggested width 800–1200px.

| Description | Path |
| --- | --- |
| Compact toolbar (hover tooltips show shortcuts) | [screenshots/en-toolbar.png](screenshots/en-toolbar.png) |
| Expanded window list + frame offset / FPS | [screenshots/en-window-list.png](screenshots/en-window-list.png) |
| Align / sync-lock status | [screenshots/en-align-sync.png](screenshots/en-align-sync.png) |

![Compact toolbar (placeholder — replace with a real screenshot)](screenshots/en-toolbar.png)

![Window list and frame offset (placeholder — replace with a real screenshot)](screenshots/en-window-list.png)

![Align and sync lock (placeholder — replace with a real screenshot)](screenshots/en-align-sync.png)

If the UI is Chinese-only, reuse the `zh-*.png` files for this page as well.

## Features

- **Auto-discovery**: Finds running PotPlayer windows (process names containing `PotPlayerMini` variants).
- **Batch control**: Play/pause, skip, jump to start, show, or minimize all discovered windows in one click.
- **Skip amount**: Default 5 seconds; editable 1–600 s; persisted.
- **Align timelines**: Uses the **primary window** (first list item) as the reference; other windows seek to `reference time + that window’s frame offset`, quantized to a frame boundary.
- **Frame offset**: Expand the window list, set a signed frame offset relative to the primary window, and pick an assumed FPS (24/25/30/50/60/120). Saved by window title.
- **Sync lock**: After align, optionally poll relative progress and nudge windows that drift by about one frame. Can be turned off at any time.
- **In-window shortcuts**: Left-hand, one-handed while this app is focused (table below). Keys are not captured when the app is unfocused.
- **Elevated windows**: Listed as `管理员` (administrator). If commands fail, restart this app as administrator.
- **Always on top**: Control window starts topmost; can be toggled.
- **Collapsible panels**: Window list and command bar start collapsed.
- **Logging**: Written under local app data for troubleshooting.

## Shortcuts

Active only when this window is focused and focus is not in the seconds / FPS / frame-offset fields.

| Action | Key |
| --- | --- |
| Skip back | `A` |
| Play / pause | `S` |
| Skip forward | `D` |
| Jump to start | `Q` |
| Align timelines | `W` |
| Show all | `E` |
| Minimize all | `R` |

## Prerequisites

- **OS**: Windows (Win32 window messages).
- **SDK**: .NET 8 SDK or later (see [PotPlayerMultiControl.csproj](../PotPlayerMultiControl.csproj)).

## Build and run

From the repo root (the folder with `PotPlayerMultiControl.csproj`):

```powershell
dotnet build
dotnet run
```

Self-contained single-file publish (example output folder `out`):

```powershell
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o out
```

## Usage

- Starts as a compact icon toolbar; hover for actions and shortcuts. Window list and log start collapsed. The control window is topmost by default so you can switch back and use one hand.
- Play/pause all or `S`: sends media play/pause to each PotPlayer.
- Skip or `A` / `D`: seeks by the current span (default 5 s; edit in the seconds box).
- Jump to start or `Q`: seeks every video to the beginning of the file.
- Align or `W`: seeks every window to the primary timeline (including frame offsets). Zero or one window: status message, no crash.
- Expand **窗口列表** (window list): the first item is primary; select others and set offset relative to primary. Positive means that window’s picture is *later* than the primary at the same clock (larger position). If A is 240 frames ahead of primary B, set A to `+240`.
- Sync-lock icon: toggle automatic nudges during playback. Off means no automatic seeks.
- Show all or `E`: restore all PotPlayer windows and raise them (topmost, without stealing focus).
- Minimize all or `R`: minimize all PotPlayer windows.
- Toggle this app’s always-on-top.
- Refresh the window list to rescan.
- If a row shows `管理员` and commands fail, restart as administrator.

## Log and settings

- Log: `%LocalAppData%\PotPlayerMultiControl\app.log`
- Settings: `%LocalAppData%\PotPlayerMultiControl\settings.txt` (seek span, FPS, sync lock, per-window frame offsets)

## Implementation notes

- Enumerates windows via Win32 and filters by process name (`PotPlayerMini64` / `PotPlayerMini` / `PotPlayerMini32`).
- Play/pause: `WM_APPCOMMAND` (`APPCOMMAND_MEDIA_PLAY_PAUSE`).
- Jump to start: `WM_COMMAND` `10243`.
- Position: PotPlayer `WM_USER` `0x5004` / `0x5005` (custom-length skip and align).
- Align quantizes the target time to a frame boundary using the assumed FPS; retries shortly if read-back error exceeds about one frame.
- Sync lock polls about every 300 ms; nudges only after ~1 frame of error and a cooldown, to avoid seek spam.
- `ShowWindow` / `SetWindowPos(HWND_TOPMOST)` restore and raise all PotPlayer windows; minimize clears topmost.
- Shortcuts use form `ProcessCmdKey`; no `RegisterHotKey` global hotkeys.

## Contributing and license

Issues and pull requests are welcome; keep changes small and explained. Licensed under the [MIT License](../LICENSE).

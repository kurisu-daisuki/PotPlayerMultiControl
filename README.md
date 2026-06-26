# PotPlayer Multi Control (AutoHotkey)

使用 AutoHotkey 一键同时控制多个 PotPlayer 窗口。

## 功能

- 同时播放 / 暂停全部 PotPlayer 窗口
- 同时快进 / 后退
- 同时逐帧前进
- 同时静音 / 取消静音
- 仅向标题包含 `PotPlayer` 的窗口发送按键

## 环境要求

1. Windows
2. 安装 [AutoHotkey v2](https://www.autohotkey.com/)
3. 已打开多个 PotPlayer 窗口

## 快速开始

1. 克隆或下载本仓库
2. 安装 AutoHotkey v2
3. 双击运行 `potplayer_multi_control.ahk`
4. 打开多个 PotPlayer 窗口
5. 使用下方热键进行联动控制

## 默认热键

- `Ctrl + Alt + Space`：播放 / 暂停
- `Ctrl + Alt + Right`：快进
- `Ctrl + Alt + Left`：后退
- `Ctrl + Alt + .`：逐帧前进
- `Ctrl + Alt + M`：静音 / 取消静音

> 注意：脚本基于 PotPlayer 默认快捷键。如果你修改过 PotPlayer 的快捷键，请同步修改脚本中 `SendToPotPlayers()` 的按键参数。

## 自定义

在 `potplayer_multi_control.ahk` 中，修改如下调用即可：

```ahk
SendToPotPlayers("{Space}")
SendToPotPlayers("{Right}")
SendToPotPlayers("{Left}")
SendToPotPlayers("d")
SendToPotPlayers("m")
```

## 已知限制

- 这是“联动控制”，不是毫秒级精确同步。
- 某些全屏窗口或高权限窗口可能会拒收按键。
- 如果你的窗口标题不含 `PotPlayer`，请调整脚本中的窗口匹配规则。

## 文件说明

- `potplayer_multi_control.ahk`：主脚本

#Requires AutoHotkey v2.0
#SingleInstance Force

; PotPlayer Multi Control
; 同时向所有 PotPlayer 窗口发送快捷键

SetTitleMatchMode 2 ; 标题包含匹配

SendToPotPlayers(keys) {
    windows := WinGetList("PotPlayer")

    if windows.Length = 0 {
        ToolTip("未找到 PotPlayer 窗口")
        SetTimer(() => ToolTip(), -1000)
        return
    }

    for hwnd in windows {
        try {
            ; 向后台窗口发送按键，避免频繁切换焦点
            ControlSend(keys, , "ahk_id " hwnd)
        } catch {
            ; 某些窗口可能拒收，忽略继续
        }
    }

    ToolTip("已发送到 " windows.Length " 个 PotPlayer 窗口")
    SetTimer(() => ToolTip(), -800)
}

; Ctrl + Alt + Space = 播放/暂停
^!Space:: {
    SendToPotPlayers("{Space}")
}

; Ctrl + Alt + Right = 快进
^!Right:: {
    SendToPotPlayers("{Right}")
}

; Ctrl + Alt + Left = 后退
^!Left:: {
    SendToPotPlayers("{Left}")
}

; Ctrl + Alt + . = 逐帧前进
^!.:: {
    SendToPotPlayers("d")
}

; Ctrl + Alt + M = 静音/取消静音
^!m:: {
    SendToPotPlayers("m")
}

using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Windows.Forms;

namespace PotPlayerMultiControl;

public sealed partial class MainForm : Form
{
    private const int DefaultSeekSeconds = 5;
    private const int DefaultFps = 30;
    private const int SyncPollMs = 300;
    private const int AlignRetryDelayMs = 40;

    private const uint WmAppCommand = 0x0319;
    private const uint WmCommand = 0x0111;
    private const uint WmUser = 0x0400;
    private const int AppCommandMediaPlayPause = 14;
    private const int CmdGoToBeginning = 10243;
    private const int PotGetTotalTime = 0x5002;
    private const int PotGetCurrentTime = 0x5004;
    private const int PotSetCurrentTime = 0x5005;
    private const int SwMinimize = 6;
    private const int SwRestore = 9;
    private const uint SwpNoSize = 0x0001;
    private const uint SwpNoMove = 0x0002;
    private const uint SwpNoActivate = 0x0010;
    private const uint SwpShowWindow = 0x0040;
    private static readonly nint HwndTopmost = -1;
    private static readonly nint HwndNoTopmost = -2;
    private static readonly Color ColorWindow = Color.FromArgb(246, 247, 249);
    private static readonly Color ColorSurface = Color.White;
    private static readonly Color ColorBorder = Color.FromArgb(218, 220, 224);
    private static readonly Color ColorText = Color.FromArgb(32, 33, 36);
    private static readonly Color ColorMuted = Color.FromArgb(95, 99, 104);
    private static readonly Color ColorAccent = Color.FromArgb(26, 115, 232);
    private static readonly Color ColorAccentHover = Color.FromArgb(21, 99, 204);
    private static readonly Color ColorHover = Color.FromArgb(232, 240, 254);
    private static readonly Color ColorActive = Color.FromArgb(210, 227, 252);
    private static readonly Color ColorWarning = Color.FromArgb(179, 74, 12);
    private const int DetailsTop = 78;
    private const int HeaderHeight = 28;
    private const int SectionGap = 8;
    private const int ListHeight = 190;
    private const int LogHeight = 236;
    private const int BottomPadding = 12;
    private const int ContentLeft = 12;
    private const int ContentWidth = 508;
    private const int FormWidth = 532;
    private const int OffsetRowHeight = 28;
    private const string IconGoToStart = "\uE100";
    private const string IconRewind = "\uEB9E";
    private const string IconPlayPause = "\uE768";
    private const string IconForward = "\uEB9D";
    private const string IconAlign = "\uE8A9";
    private const string IconSyncOn = "\uE72E";
    private const string IconSyncOff = "\uE785";
    private const string IconShowAll = "\uE8A7";
    private const string IconMinimize = "\uE921";
    private const string IconPin = "\uE718";
    private const string IconUnpin = "\uE77A";
    private const string IconRefresh = "\uE72C";
    private const string IconAdmin = "\uEA18";

    private readonly Font _iconFont = CreateIconFont();
    private readonly Panel _sepPlayback = new();
    private readonly Panel _sepSeek = new();
    private readonly Panel _sepWindow = new();
    private readonly Label _frameOffsetUnitLabel = new();

    private readonly string _logFilePath;
    private readonly string _settingsFilePath;
    private readonly bool _isElevated = ProcessIntegrity.IsCurrentProcessElevated();
    private readonly TimeSpan _toggleCooldown = TimeSpan.FromMilliseconds(400);
    private readonly TimeSpan _seekCooldown = TimeSpan.FromMilliseconds(120);
    private readonly TimeSpan _syncCorrectCooldown = TimeSpan.FromMilliseconds(250);
    private readonly Dictionary<string, int> _frameOffsets = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<nint, DateTime> _lastSyncCorrectAt = new();
    private readonly System.Windows.Forms.Timer _syncTimer = new();
    private DateTime _lastToggleAt = DateTime.MinValue;
    private DateTime _lastSyncSummaryAt = DateTime.MinValue;
    private IReadOnlyList<PotPlayerWindow> _windows = [];
    private bool _toggleInProgress;
    private bool _loadingSettings;
    private bool _updatingFrameOffsetUi;
    private bool _windowListExpanded;
    private bool _logExpanded;
    private bool _syncLockEnabled = true;
    private int _discoveredWindowCount;
    private int _syncCorrectionCount;
    private long _syncMaxDriftMs;

    public MainForm()
    {
        InitializeComponent();
        DoubleBuffered = true;
        BackColor = ColorWindow;
        ForeColor = ColorText;

        Text = _isElevated ? "PotPlayer 多窗口控制（管理员）" : "PotPlayer 多窗口控制";
        ConfigureToolbar();

        var logDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PotPlayerMultiControl");
        Directory.CreateDirectory(logDir);
        _logFilePath = Path.Combine(logDir, "app.log");
        _settingsFilePath = Path.Combine(logDir, "settings.txt");

        LoadSettings();
        UpdateSeekButtonTexts();
        UpdateSyncLockButton();
        ApplyDetailsLayout();
        RefreshWindowList();
        TopMost = true;
        UpdatePinTopButton();
        ConfigureSyncTimer();
        Log(_isElevated ? "应用启动（管理员权限）" : "应用启动（普通权限）");
        Log("快捷键仅在本窗口聚焦时有效：Q 起始 / W 对齐 / A 后退 / S 播放暂停 / D 快进 / E 显示 / R 最小化");
    }

    private void ToggleButton_Click(object? sender, EventArgs e)
    {
        RequestCommand("按钮", "播放/暂停", TrySendPlayPause);
    }

    private void GoToStartButton_Click(object? sender, EventArgs e)
    {
        RequestCommand("按钮", "回到起始点", TrySendGoToStart);
    }

    private void ShowAllButton_Click(object? sender, EventArgs e)
    {
        RequestCommand("按钮", "显示窗口", TryShowWindow);
    }

    private void MinimizeAllButton_Click(object? sender, EventArgs e)
    {
        RequestCommand("按钮", "最小化窗口", TryMinimizeWindow);
    }

    private void RefreshButton_Click(object? sender, EventArgs e)
    {
        RefreshWindowList();
    }

    private void ElevateButton_Click(object? sender, EventArgs e)
    {
        RestartElevated();
    }

    private void PinTopButton_Click(object? sender, EventArgs e)
    {
        TopMost = !TopMost;
        UpdatePinTopButton();
        Log(TopMost ? "控制窗口已置顶" : "控制窗口已取消置顶");
    }

    private static Font CreateIconFont()
    {
        var names = FontFamily.Families.Select(family => family.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (names.Contains("Segoe MDL2 Assets"))
        {
            return new Font("Segoe MDL2 Assets", 12f);
        }

        if (names.Contains("Segoe Fluent Icons"))
        {
            return new Font("Segoe Fluent Icons", 12f);
        }

        return new Font("Segoe UI Symbol", 12f);
    }

    private void ConfigureToolbar()
    {
        const int buttonY = 36;
        const int buttonSize = 32;
        const int gap = 3;
        const int groupGap = 12;
        var x = ContentLeft;

        void Place(Button button, string glyph, string tooltip, bool primary = false)
        {
            button.Location = new Point(x, buttonY);
            button.Size = new Size(buttonSize, buttonSize);
            button.Font = _iconFont;
            button.Text = glyph;
            button.Tag = primary ? "primary" : "icon";
            StyleIconButton(button);
            toolTip.SetToolTip(button, tooltip);
            x += buttonSize + gap;
        }

        Place(goToStartButton, IconGoToStart, "回到起始点 (Q)");
        Place(rewindButton, IconRewind, "后退 (A)");
        Place(toggleButton, IconPlayPause, "播放/暂停全部 (S)", primary: true);
        Place(forwardButton, IconForward, "快进 (D)");
        Place(alignButton, IconAlign, "对齐进度 (W)");
        x += groupGap - gap;
        PlaceSeparator(_sepPlayback, x - groupGap / 2, buttonY, buttonSize);
        x += 2;

        seekSecondsUpDown.Location = new Point(x, buttonY + 4);
        seekSecondsUpDown.Size = new Size(46, 23);
        seekSecondsUpDown.BorderStyle = BorderStyle.FixedSingle;
        seekSecondsUpDown.BackColor = ColorSurface;
        toolTip.SetToolTip(seekSecondsUpDown, "快进/后退时间跨度（秒）");
        x += seekSecondsUpDown.Width + 4;
        seekSecondsLabel.Location = new Point(x, buttonY + 9);
        seekSecondsLabel.AutoSize = true;
        seekSecondsLabel.ForeColor = ColorMuted;
        seekSecondsLabel.Text = "秒";
        x += 18 + groupGap;
        PlaceSeparator(_sepSeek, x - groupGap / 2, buttonY, buttonSize);

        Place(showAllButton, IconShowAll, "显示全部并置顶 (E)");
        Place(minimizeAllButton, IconMinimize, "最小化全部 (R)");
        Place(syncLockButton, IconSyncOn, "同步锁：播放中自动微调对齐");
        x += groupGap - gap;
        PlaceSeparator(_sepWindow, x - groupGap / 2, buttonY, buttonSize);

        Place(pinTopButton, IconPin, "置顶控制窗口");
        Place(refreshButton, IconRefresh, "刷新窗口列表");
        Place(elevateButton, IconAdmin, _isElevated ? "当前已是管理员权限" : "以管理员身份重启");
        elevateButton.Enabled = !_isElevated;

        statusLabel.Location = new Point(ContentLeft, 10);
        statusLabel.Size = new Size(ContentWidth, 20);
        statusLabel.ForeColor = ColorMuted;
        StyleSectionToggle(windowListToggle);
        StyleSectionToggle(logToggle);
        windowListToggle.Size = new Size(ContentWidth, HeaderHeight);
        logToggle.Size = new Size(ContentWidth, HeaderHeight);

        listBox.BorderStyle = BorderStyle.FixedSingle;
        listBox.BackColor = ColorSurface;
        listBox.ForeColor = ColorText;
        listBox.IntegralHeight = false;
        listBox.Size = new Size(ContentWidth, ListHeight);

        logTextBox.BorderStyle = BorderStyle.FixedSingle;
        logTextBox.BackColor = ColorSurface;
        logTextBox.ForeColor = ColorMuted;
        logTextBox.Size = new Size(ContentWidth, LogHeight);

        UpdatePinTopButton();
        UpdateSeekButtonTexts();
        StyleIconButton(elevateButton);
        ConfigureOffsetRow();
    }

    private void ConfigureOffsetRow()
    {
        fpsLabel.ForeColor = ColorMuted;
        fpsLabel.AutoSize = true;
        fpsLabel.Text = "帧率";
        frameOffsetLabel.ForeColor = ColorMuted;
        frameOffsetLabel.AutoSize = true;
        frameOffsetLabel.Text = "相对主窗口";
        fpsComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        fpsComboBox.BackColor = ColorSurface;
        fpsComboBox.FlatStyle = FlatStyle.Flat;
        frameOffsetUpDown.BorderStyle = BorderStyle.FixedSingle;
        frameOffsetUpDown.BackColor = ColorSurface;
        frameOffsetUpDown.Minimum = -1000000;
        frameOffsetUpDown.Maximum = 1000000;
        _frameOffsetUnitLabel.AutoSize = true;
        _frameOffsetUnitLabel.ForeColor = ColorMuted;
        _frameOffsetUnitLabel.Text = "帧";
        if (!Controls.Contains(_frameOffsetUnitLabel))
        {
            Controls.Add(_frameOffsetUnitLabel);
        }

        toolTip.SetToolTip(fpsComboBox, "假定帧率，用于帧偏移换算与帧边界量化");
        toolTip.SetToolTip(frameOffsetUpDown, "相对主窗口的帧偏移（正数表示该窗口画面更靠后）");
    }

    private void PlaceSeparator(Panel separator, int x, int y, int height)
    {
        separator.BackColor = ColorBorder;
        separator.Location = new Point(x, y + 4);
        separator.Size = new Size(1, height - 8);
        if (!Controls.Contains(separator))
        {
            Controls.Add(separator);
            separator.BringToFront();
        }
    }

    private void StyleIconButton(Button button)
    {
        var primary = button.Tag as string == "primary";
        var active = (button == pinTopButton && TopMost) || (button == syncLockButton && _syncLockEnabled);
        button.Cursor = Cursors.Hand;
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseOverBackColor = primary ? ColorAccentHover : ColorHover;
        button.FlatAppearance.MouseDownBackColor = primary ? ColorAccentHover : ColorActive;
        button.UseVisualStyleBackColor = false;
        if (!button.Enabled)
        {
            button.BackColor = ColorWindow;
            button.ForeColor = Color.FromArgb(160, 160, 160);
            return;
        }

        if (primary)
        {
            button.BackColor = ColorAccent;
            button.ForeColor = Color.White;
            return;
        }

        if (active)
        {
            button.BackColor = ColorActive;
            button.ForeColor = ColorAccent;
            return;
        }

        button.BackColor = ColorWindow;
        button.ForeColor = ColorText;
    }

    private static void StyleSectionToggle(Button button)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseOverBackColor = ColorHover;
        button.UseVisualStyleBackColor = false;
        button.BackColor = ColorWindow;
        button.ForeColor = ColorText;
        button.TextAlign = ContentAlignment.MiddleLeft;
        button.Padding = new Padding(4, 0, 0, 0);
        button.Cursor = Cursors.Hand;
    }

    private void UpdatePinTopButton()
    {
        pinTopButton.Text = TopMost ? IconUnpin : IconPin;
        StyleIconButton(pinTopButton);
        toolTip.SetToolTip(pinTopButton, TopMost ? "取消控制窗口置顶" : "置顶控制窗口");
    }

    private void AlignButton_Click(object? sender, EventArgs e)
    {
        RequestAlign("按钮");
    }

    private void SyncLockButton_Click(object? sender, EventArgs e)
    {
        _syncLockEnabled = !_syncLockEnabled;
        UpdateSyncLockButton();
        SaveSettings();
        Log(_syncLockEnabled ? "同步锁已开启" : "同步锁已关闭");
        SetStatus(_syncLockEnabled ? "同步锁已开启，将监测并微调进度" : "同步锁已关闭");
    }

    private void UpdateSyncLockButton()
    {
        syncLockButton.Text = _syncLockEnabled ? IconSyncOn : IconSyncOff;
        StyleIconButton(syncLockButton);
        toolTip.SetToolTip(syncLockButton, _syncLockEnabled
            ? "关闭同步锁（当前开启：播放中自动微调）"
            : "开启同步锁（关闭中：不对齐后自动微调）");
        if (_syncLockEnabled)
        {
            _syncTimer.Start();
        }
        else
        {
            _syncTimer.Stop();
            _syncCorrectionCount = 0;
            _syncMaxDriftMs = 0;
        }
    }

    private void ConfigureSyncTimer()
    {
        _syncTimer.Interval = SyncPollMs;
        _syncTimer.Tick += SyncTimer_Tick;
        if (_syncLockEnabled)
        {
            _syncTimer.Start();
        }
    }

    private void FpsComboBox_SelectedIndexChanged(object? sender, EventArgs e)
    {
        SaveSettings();
        UpdateFrameOffsetHint();
    }

    private void FrameOffsetUpDown_ValueChanged(object? sender, EventArgs e)
    {
        if (_updatingFrameOffsetUi)
        {
            return;
        }

        var window = SelectedWindow();
        if (window is null || IsMasterWindow(window))
        {
            UpdateFrameOffsetHint();
            return;
        }

        _frameOffsets[OffsetKey(window)] = (int)frameOffsetUpDown.Value;
        SaveSettings();
        UpdateFrameOffsetHint();
    }

    private void ListBox_SelectedIndexChanged(object? sender, EventArgs e)
    {
        LoadFrameOffsetEditor();
    }

    private void RewindButton_Click(object? sender, EventArgs e)
    {
        RequestSeek("按钮", rewind: true);
    }

    private void ForwardButton_Click(object? sender, EventArgs e)
    {
        RequestSeek("按钮", rewind: false);
    }

    private void SeekSecondsUpDown_ValueChanged(object? sender, EventArgs e)
    {
        UpdateSeekButtonTexts();
        SaveSettings();
    }

    private void WindowListToggle_Click(object? sender, EventArgs e)
    {
        _windowListExpanded = !_windowListExpanded;
        ApplyDetailsLayout();
    }

    private void LogToggle_Click(object? sender, EventArgs e)
    {
        _logExpanded = !_logExpanded;
        ApplyDetailsLayout();
    }

    private void ApplyDetailsLayout()
    {
        SuspendLayout();
        var y = DetailsTop;

        windowListToggle.Location = new Point(ContentLeft, y);
        y += HeaderHeight + SectionGap;

        listBox.Visible = _windowListExpanded;
        var showOffsetRow = _windowListExpanded;
        fpsLabel.Visible = showOffsetRow;
        fpsComboBox.Visible = showOffsetRow;
        frameOffsetLabel.Visible = showOffsetRow;
        frameOffsetUpDown.Visible = showOffsetRow;
        _frameOffsetUnitLabel.Visible = showOffsetRow;
        if (_windowListExpanded)
        {
            listBox.Location = new Point(ContentLeft, y);
            y += ListHeight + 6;
            LayoutOffsetRow(y);
            y += OffsetRowHeight + SectionGap;
        }

        logToggle.Location = new Point(ContentLeft, y);
        y += HeaderHeight + SectionGap;
        UpdateSectionHeaders();

        logTextBox.Visible = _logExpanded;
        if (_logExpanded)
        {
            logTextBox.Location = new Point(ContentLeft, y);
            y += LogHeight + BottomPadding;
        }
        else
        {
            y += BottomPadding - SectionGap;
        }

        ClientSize = new Size(FormWidth, y);
        ResumeLayout(true);
    }

    private void LayoutOffsetRow(int y)
    {
        const int gap = 6;
        const int groupGap = 16;
        var x = ContentLeft;
        var labelY = y + 5;
        var fieldY = y + 2;

        fpsLabel.Location = new Point(x, labelY);
        x += fpsLabel.PreferredWidth + gap;
        fpsComboBox.Location = new Point(x, fieldY);
        fpsComboBox.Size = new Size(58, 23);
        x += fpsComboBox.Width + groupGap;

        frameOffsetLabel.Location = new Point(x, labelY);
        x += frameOffsetLabel.PreferredWidth + gap;
        frameOffsetUpDown.Location = new Point(x, fieldY);
        frameOffsetUpDown.Size = new Size(72, 23);
        x += frameOffsetUpDown.Width + gap;
        _frameOffsetUnitLabel.Location = new Point(x, labelY);
    }

    private void UpdateSectionHeaders()
    {
        var chevronList = _windowListExpanded ? "▾" : "▸";
        var chevronLog = _logExpanded ? "▾" : "▸";
        var count = _discoveredWindowCount > 0 ? $"  ·  {_discoveredWindowCount}" : "";
        windowListToggle.Text = $"{chevronList}  窗口列表{count}";
        logToggle.Text = $"{chevronLog}  运行日志";
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (IsTypingInInputControl())
        {
            return base.ProcessCmdKey(ref msg, keyData);
        }

        switch (keyData)
        {
            case Keys.A:
                Log("触发快捷键: A");
                RequestSeek("快捷键", rewind: true);
                return true;
            case Keys.S:
                Log("触发快捷键: S");
                RequestCommand("快捷键", "播放/暂停", TrySendPlayPause);
                return true;
            case Keys.D:
                Log("触发快捷键: D");
                RequestSeek("快捷键", rewind: false);
                return true;
            case Keys.Q:
                Log("触发快捷键: Q");
                RequestCommand("快捷键", "回到起始点", TrySendGoToStart);
                return true;
            case Keys.W:
                Log("触发快捷键: W");
                RequestAlign("快捷键");
                return true;
            case Keys.E:
                Log("触发快捷键: E");
                RequestCommand("快捷键", "显示窗口", TryShowWindow);
                return true;
            case Keys.R:
                Log("触发快捷键: R");
                RequestCommand("快捷键", "最小化窗口", TryMinimizeWindow);
                return true;
            default:
                return base.ProcessCmdKey(ref msg, keyData);
        }
    }

    private bool IsTypingInInputControl()
    {
        var focused = ActiveControl;
        return focused is NumericUpDown or ComboBox or TextBox { ReadOnly: false };
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _syncTimer.Stop();
        _syncTimer.Dispose();
        Log("应用退出");
        base.OnFormClosed(e);
    }

    private void RequestCommand(string source, string actionName, Func<nint, bool> send, TimeSpan? cooldown = null)
    {
        if (_toggleInProgress)
        {
            Log($"忽略重复触发: {source} {actionName}（上次操作尚未完成）");
            return;
        }

        var now = DateTime.UtcNow;
        if (now - _lastToggleAt < (cooldown ?? _toggleCooldown))
        {
            Log($"忽略重复触发: {source} {actionName}（冷却中）");
            return;
        }

        _lastToggleAt = now;
        SendToAll(actionName, send);
    }

    private void RequestSeek(string source, bool rewind)
    {
        var seconds = SeekSeconds;
        var deltaMs = seconds * 1000 * (rewind ? -1 : 1);
        var actionName = rewind ? $"后退 {seconds}秒" : $"快进 {seconds}秒";
        RequestCommand(source, actionName, hwnd => TrySendSeek(hwnd, deltaMs), _seekCooldown);
    }

    private void RequestAlign(string source)
    {
        if (_toggleInProgress)
        {
            Log($"忽略重复触发: {source} 对齐进度（上次操作尚未完成）");
            return;
        }

        var now = DateTime.UtcNow;
        if (now - _lastToggleAt < _seekCooldown)
        {
            Log($"忽略重复触发: {source} 对齐进度（冷却中）");
            return;
        }

        _lastToggleAt = now;
        AlignAll(source);
    }

    private void AlignAll(string source)
    {
        _toggleInProgress = true;
        try
        {
            var windows = PotPlayerWindowFinder.FindAll();
            if (windows.Count == 0)
            {
                SetStatus("未找到 PotPlayer 窗口");
                Log($"{source} 对齐失败: 未找到 PotPlayer 窗口");
                RefreshWindowList(windows);
                return;
            }

            if (windows.Count == 1)
            {
                SetStatus("仅发现 1 个窗口，无需对齐");
                Log($"{source} 对齐跳过: 仅 1 个窗口");
                RefreshWindowList(windows);
                return;
            }

            var master = windows[0];
            if (!TryGetCurrentTime(master.Handle, out var baseMs))
            {
                var blocked = !_isElevated && master.IsElevated;
                SetStatus(
                    blocked
                        ? "无法读取主窗口进度，管理员窗口无法控制，请提权后重试"
                        : "无法读取主窗口进度",
                    warning: true);
                Log($"{source} 对齐失败: 无法读取主窗口 0x{master.Handle.ToInt64():X8} {master.Title}");
                RefreshWindowList(windows);
                return;
            }

            var fps = SelectedFps;
            var success = 0;
            var elevationBlocked = 0;
            var maxErrorMs = 0L;
            foreach (var window in windows)
            {
                var targetMs = TargetTimeMs(baseMs, GetFrameOffset(window), fps);
                if (!TryGetTotalTime(window.Handle, out var totalMs) && Marshal.GetLastWin32Error() == 5)
                {
                    if (!_isElevated && window.IsElevated)
                    {
                        elevationBlocked++;
                    }

                    Log($"对齐失败: 0x{window.Handle.ToInt64():X8} {window.Title}");
                    continue;
                }

                targetMs = ClampTime(targetMs, totalMs);
                if (!TrySetCurrentTimeWithRetry(window.Handle, targetMs, fps, out var errorMs))
                {
                    if (!_isElevated && window.IsElevated)
                    {
                        elevationBlocked++;
                        Log($"对齐失败（UIPI，目标为管理员窗口）: 0x{window.Handle.ToInt64():X8} {window.Title}");
                    }
                    else
                    {
                        Log($"对齐失败: 0x{window.Handle.ToInt64():X8} {window.Title}");
                    }

                    continue;
                }

                success++;
                maxErrorMs = Math.Max(maxErrorMs, errorMs);
                var offset = GetFrameOffset(window);
                Log($"对齐成功: 0x{window.Handle.ToInt64():X8} {window.Title} 目标 {targetMs}ms 误差 {errorMs}ms 帧偏移 {offset}");
            }

            var frameMs = FrameDurationMs(fps);
            var maxErrorFrames = frameMs <= 0 ? 0 : (maxErrorMs + frameMs - 1) / frameMs;
            var summary =
                elevationBlocked > 0 && !_isElevated
                    ? $"对齐 {success}/{windows.Count}，{elevationBlocked} 个管理员窗口无法控制，请提权后重试"
                    : $"对齐 {success}/{windows.Count}，最大误差 {maxErrorMs}ms（约 {maxErrorFrames} 帧，{fps}fps）";
            SetStatus(summary, warning: elevationBlocked > 0 && !_isElevated);
            Log($"{source} {statusLabel.Text}");
            var resultStatus = statusLabel.Text;
            var resultWarning = statusLabel.ForeColor == ColorWarning;
            RefreshWindowList(windows);
            SetStatus(resultStatus, resultWarning);
        }
        finally
        {
            _toggleInProgress = false;
        }
    }

    private void SyncTimer_Tick(object? sender, EventArgs e)
    {
        if (!_syncLockEnabled || _toggleInProgress || !IsHandleCreated)
        {
            return;
        }

        var windows = PotPlayerWindowFinder.FindAll();
        if (windows.Count < 2)
        {
            return;
        }

        var master = windows[0];
        if (!TryGetCurrentTime(master.Handle, out var baseMs))
        {
            return;
        }

        var fps = SelectedFps;
        var thresholdMs = Math.Max(1, FrameDurationMs(fps));
        var now = DateTime.UtcNow;
        var corrected = 0;
        var tickMaxDrift = 0L;
        foreach (var window in windows)
        {
            var expected = TargetTimeMs(baseMs, GetFrameOffset(window), fps);
            if (!TryGetTotalTime(window.Handle, out var totalMs))
            {
                continue;
            }

            expected = ClampTime(expected, totalMs);
            if (!TryGetCurrentTime(window.Handle, out var actualMs))
            {
                continue;
            }

            var drift = Math.Abs(actualMs - expected);
            tickMaxDrift = Math.Max(tickMaxDrift, drift);
            _syncMaxDriftMs = Math.Max(_syncMaxDriftMs, drift);
            if (drift < thresholdMs)
            {
                continue;
            }

            if (_lastSyncCorrectAt.TryGetValue(window.Handle, out var lastAt) && now - lastAt < _syncCorrectCooldown)
            {
                continue;
            }

            if (!TrySetCurrentTime(window.Handle, expected))
            {
                continue;
            }

            _lastSyncCorrectAt[window.Handle] = now;
            _syncCorrectionCount++;
            corrected++;
        }

        if (corrected > 0 && now - _lastSyncSummaryAt >= TimeSpan.FromSeconds(5))
        {
            _lastSyncSummaryAt = now;
            Log($"同步锁微调 {corrected} 个窗口，累计 {_syncCorrectionCount} 次，本轮最大偏差 {tickMaxDrift}ms，会话最大 {_syncMaxDriftMs}ms");
        }
    }

    private void SendToAll(string actionName, Func<nint, bool> send)
    {
        _toggleInProgress = true;
        try
        {
            var windows = PotPlayerWindowFinder.FindAll();
            if (windows.Count == 0)
            {
                SetStatus("未找到 PotPlayer 窗口");
                Log($"{actionName}失败: 未找到 PotPlayer 窗口");
                return;
            }

            var success = 0;
            var elevationBlocked = 0;
            foreach (var window in windows)
            {
                if (send(window.Handle))
                {
                    success++;
                    Log($"{actionName}成功: 0x{window.Handle.ToInt64():X8} {window.Title}");
                }
                else if (!_isElevated && window.IsElevated)
                {
                    elevationBlocked++;
                    Log($"{actionName}失败（UIPI，目标为管理员窗口）: 0x{window.Handle.ToInt64():X8} {window.Title}");
                }
                else
                {
                    Log($"{actionName}失败: 0x{window.Handle.ToInt64():X8} {window.Title}");
                }
            }

            SetStatus(
                elevationBlocked > 0 && !_isElevated
                    ? $"已发送{actionName} {success}/{windows.Count}，{elevationBlocked} 个管理员窗口无法控制，请提权后重试"
                    : $"已发送{actionName}到 {success}/{windows.Count} 个窗口",
                warning: elevationBlocked > 0 && !_isElevated);
            Log(statusLabel.Text);
            var resultStatus = statusLabel.Text;
            var resultWarning = statusLabel.ForeColor == ColorWarning;
            RefreshWindowList(windows);
            SetStatus(resultStatus, resultWarning);
        }
        finally
        {
            _toggleInProgress = false;
        }
    }

    private bool TrySendPlayPause(nint hwnd)
    {
        var lParam = (nint)(AppCommandMediaPlayPause << 16);
        return SendMessage(hwnd, WmAppCommand, hwnd, lParam) != nint.Zero;
    }

    private bool TrySendGoToStart(nint hwnd)
    {
        if (PostMessage(hwnd, WmCommand, CmdGoToBeginning, nint.Zero))
        {
            return true;
        }

        _ = SendMessage(hwnd, WmCommand, CmdGoToBeginning, nint.Zero);
        return Marshal.GetLastWin32Error() != 5;
    }

    private int SeekSeconds => Math.Max(1, (int)seekSecondsUpDown.Value);

    private int SelectedFps
    {
        get
        {
            if (fpsComboBox.SelectedItem is string text && int.TryParse(text, out var fps) && fps > 0)
            {
                return fps;
            }

            return DefaultFps;
        }
    }

    private static int FrameDurationMs(int fps) => Math.Max(1, 1000 / fps);

    private static long ClampTime(long targetMs, long totalMs)
    {
        if (targetMs < 0)
        {
            return 0;
        }

        if (totalMs > 0 && targetMs > totalMs)
        {
            return totalMs;
        }

        return targetMs;
    }

    private static long QuantizeToFrame(long ms, int fps)
    {
        var frame = (ms * fps + 500L) / 1000L;
        return frame * 1000L / fps;
    }

    private static long TargetTimeMs(long baseMs, int frameOffset, int fps)
    {
        var offsetMs = frameOffset * 1000L / fps;
        return QuantizeToFrame(baseMs + offsetMs, fps);
    }

    private bool TryGetCurrentTime(nint hwnd, out long currentMs)
    {
        SetLastError(0);
        var current = SendMessage(hwnd, WmUser, PotGetCurrentTime, nint.Zero);
        if (Marshal.GetLastWin32Error() == 5)
        {
            currentMs = 0;
            return false;
        }

        currentMs = Math.Max(0L, current.ToInt64());
        return true;
    }

    private bool TryGetTotalTime(nint hwnd, out long totalMs)
    {
        SetLastError(0);
        var total = SendMessage(hwnd, WmUser, PotGetTotalTime, nint.Zero);
        if (Marshal.GetLastWin32Error() == 5)
        {
            totalMs = 0;
            return false;
        }

        totalMs = Math.Max(0L, total.ToInt64());
        return true;
    }

    private bool TrySetCurrentTime(nint hwnd, long targetMs)
    {
        SetLastError(0);
        _ = SendMessage(hwnd, WmUser, PotSetCurrentTime, (nint)targetMs);
        return Marshal.GetLastWin32Error() != 5;
    }

    private bool TrySetCurrentTimeWithRetry(nint hwnd, long targetMs, int fps, out long errorMs)
    {
        errorMs = 0;
        var threshold = FrameDurationMs(fps);
        for (var attempt = 0; attempt < 3; attempt++)
        {
            if (!TrySetCurrentTime(hwnd, targetMs))
            {
                return false;
            }

            if (attempt < 2)
            {
                Thread.Sleep(AlignRetryDelayMs);
            }

            if (!TryGetCurrentTime(hwnd, out var actualMs))
            {
                return false;
            }

            errorMs = Math.Abs(actualMs - targetMs);
            if (errorMs <= threshold)
            {
                return true;
            }
        }

        return true;
    }

    private bool TrySendSeek(nint hwnd, int deltaMs)
    {
        if (!TryGetCurrentTime(hwnd, out var currentMs) || !TryGetTotalTime(hwnd, out var totalMs))
        {
            return false;
        }

        return TrySetCurrentTime(hwnd, ClampTime(currentMs + deltaMs, totalMs));
    }

    private void UpdateSeekButtonTexts()
    {
        var seconds = SeekSeconds;
        toolTip.SetToolTip(rewindButton, $"后退 {seconds}秒 (A)");
        toolTip.SetToolTip(forwardButton, $"快进 {seconds}秒 (D)");
        toolTip.SetToolTip(seekSecondsUpDown, $"快进/后退时间跨度：{seconds} 秒");
    }

    private void LoadSettings()
    {
        _loadingSettings = true;
        try
        {
            var seconds = DefaultSeekSeconds;
            var fps = DefaultFps;
            _syncLockEnabled = true;
            _frameOffsets.Clear();

            if (File.Exists(_settingsFilePath))
            {
                var raw = File.ReadAllText(_settingsFilePath).Trim();
                if (int.TryParse(raw, out var legacySeconds)
                    && legacySeconds >= (int)seekSecondsUpDown.Minimum
                    && legacySeconds <= (int)seekSecondsUpDown.Maximum)
                {
                    seconds = legacySeconds;
                }
                else
                {
                    foreach (var line in raw.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
                    {
                        var trimmed = line.Trim();
                        if (trimmed.StartsWith("Offset\t", StringComparison.OrdinalIgnoreCase))
                        {
                            var parts = trimmed.Split('\t');
                            if (parts.Length >= 3 && int.TryParse(parts[^1], out var offset))
                            {
                                var title = string.Join('\t', parts.Skip(1).Take(parts.Length - 2));
                                if (!string.IsNullOrWhiteSpace(title))
                                {
                                    _frameOffsets[title] = offset;
                                }
                            }

                            continue;
                        }

                        var split = trimmed.Split('=', 2);
                        if (split.Length != 2)
                        {
                            continue;
                        }

                        var key = split[0].Trim();
                        var value = split[1].Trim();
                        if (key.Equals("SeekSeconds", StringComparison.OrdinalIgnoreCase)
                            && int.TryParse(value, out var savedSeconds)
                            && savedSeconds >= (int)seekSecondsUpDown.Minimum
                            && savedSeconds <= (int)seekSecondsUpDown.Maximum)
                        {
                            seconds = savedSeconds;
                        }
                        else if (key.Equals("Fps", StringComparison.OrdinalIgnoreCase)
                            && int.TryParse(value, out var savedFps)
                            && savedFps > 0)
                        {
                            fps = savedFps;
                        }
                        else if (key.Equals("SyncLock", StringComparison.OrdinalIgnoreCase))
                        {
                            _syncLockEnabled = value is "1" or "true" or "True";
                        }
                    }
                }
            }

            seekSecondsUpDown.Value = seconds;
            SelectFps(fps);
        }
        catch
        {
            seekSecondsUpDown.Value = DefaultSeekSeconds;
            SelectFps(DefaultFps);
            _syncLockEnabled = true;
        }
        finally
        {
            _loadingSettings = false;
        }
    }

    private void SelectFps(int fps)
    {
        var text = fps.ToString();
        var index = fpsComboBox.Items.IndexOf(text);
        if (index < 0)
        {
            fpsComboBox.Items.Add(text);
            index = fpsComboBox.Items.IndexOf(text);
        }

        fpsComboBox.SelectedIndex = index >= 0 ? index : fpsComboBox.Items.IndexOf(DefaultFps.ToString());
    }

    private void SaveSettings()
    {
        if (_loadingSettings)
        {
            return;
        }

        try
        {
            var lines = new List<string>
            {
                $"SeekSeconds={SeekSeconds}",
                $"Fps={SelectedFps}",
                $"SyncLock={(_syncLockEnabled ? "1" : "0")}"
            };
            foreach (var pair in _frameOffsets.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase))
            {
                lines.Add($"Offset\t{pair.Key.Replace('\t', ' ')}\t{pair.Value}");
            }

            File.WriteAllLines(_settingsFilePath, lines, Encoding.UTF8);
        }
        catch
        {
        }
    }

    private bool TryShowWindow(nint hwnd)
    {
        _ = ShowWindow(hwnd, SwRestore);
        _ = SetWindowPos(hwnd, HwndTopmost, 0, 0, 0, 0, SwpNoMove | SwpNoSize | SwpShowWindow | SwpNoActivate);
        return IsWindowVisible(hwnd) && !IsIconic(hwnd);
    }

    private bool TryMinimizeWindow(nint hwnd)
    {
        _ = SetWindowPos(hwnd, HwndNoTopmost, 0, 0, 0, 0, SwpNoMove | SwpNoSize | SwpNoActivate);
        _ = ShowWindowAsync(hwnd, SwMinimize);
        return IsIconic(hwnd);
    }

    private void RefreshWindowList()
    {
        RefreshWindowList(PotPlayerWindowFinder.FindAll());
    }

    private void RefreshWindowList(IReadOnlyList<PotPlayerWindow> windows)
    {
        var selectedKey = SelectedWindow() is { } selected ? OffsetKey(selected) : null;
        _windows = windows;
        _discoveredWindowCount = windows.Count;
        listBox.BeginUpdate();
        listBox.Items.Clear();
        for (var i = 0; i < windows.Count; i++)
        {
            var window = windows[i];
            var title = string.IsNullOrWhiteSpace(window.Title) ? window.ProcessName : window.Title;
            var elevationTag = window.IsElevated ? "  · 管理员" : "";
            var masterTag = i == 0 ? "  · 主窗口" : "";
            var offset = GetFrameOffset(window);
            var offsetTag = i == 0 || offset == 0 ? "" : $"  · {FormatFrameOffset(offset)}";
            listBox.Items.Add($"{title}{masterTag}{elevationTag}{offsetTag}");
        }

        listBox.EndUpdate();
        if (selectedKey is not null)
        {
            var restore = windows.ToList().FindIndex(window => OffsetKey(window) == selectedKey);
            if (restore >= 0)
            {
                listBox.SelectedIndex = restore;
            }
        }

        if (listBox.SelectedIndex < 0 && windows.Count > 0)
        {
            listBox.SelectedIndex = 0;
        }

        LoadFrameOffsetEditor();

        var elevatedCount = windows.Count(window => window.IsElevated);
        if (!_isElevated && elevatedCount > 0)
        {
            SetStatus($"发现 {windows.Count} 个窗口，其中 {elevatedCount} 个需管理员权限才能控制", warning: true);
            elevateButton.Enabled = true;
        }
        else
        {
            SetStatus(windows.Count == 0 ? "未发现 PotPlayer 窗口" : $"已发现 {windows.Count} 个 PotPlayer 窗口");
        }

        StyleIconButton(elevateButton);
        UpdateSectionHeaders();
        Log(statusLabel.Text);
    }

    private PotPlayerWindow? SelectedWindow()
    {
        var index = listBox.SelectedIndex;
        if (index < 0 || index >= _windows.Count)
        {
            return null;
        }

        return _windows[index];
    }

    private bool IsMasterWindow(PotPlayerWindow window) =>
        _windows.Count > 0 && window.Handle == _windows[0].Handle;

    private static string OffsetKey(PotPlayerWindow window) =>
        string.IsNullOrWhiteSpace(window.Title) ? window.ProcessName : window.Title;

    private int GetFrameOffset(PotPlayerWindow window) =>
        IsMasterWindow(window) ? 0 : _frameOffsets.GetValueOrDefault(OffsetKey(window));

    private static string FormatFrameOffset(int offset) =>
        offset > 0 ? $"+{offset} 帧" : $"{offset} 帧";

    private void LoadFrameOffsetEditor()
    {
        _updatingFrameOffsetUi = true;
        try
        {
            var window = SelectedWindow();
            if (window is null)
            {
                frameOffsetUpDown.Enabled = false;
                frameOffsetUpDown.Value = 0;
                UpdateFrameOffsetHint();
                return;
            }

            var isMaster = IsMasterWindow(window);
            frameOffsetUpDown.Enabled = !isMaster;
            var offset = GetFrameOffset(window);
            frameOffsetUpDown.Value = Math.Clamp(offset, (int)frameOffsetUpDown.Minimum, (int)frameOffsetUpDown.Maximum);
            UpdateFrameOffsetHint();
        }
        finally
        {
            _updatingFrameOffsetUi = false;
        }
    }

    private void UpdateFrameOffsetHint()
    {
        frameOffsetLabel.Text = "相对主窗口";
        var window = SelectedWindow();
        if (window is not null && IsMasterWindow(window))
        {
            toolTip.SetToolTip(frameOffsetUpDown, "列表第一项为主窗口，帧偏移固定为 0");
            return;
        }

        toolTip.SetToolTip(frameOffsetUpDown, "正数表示该窗口画面比主窗口更靠后（同一时刻进度更大）");
    }

    private void SetStatus(string text, bool warning = false)
    {
        statusLabel.Text = text;
        statusLabel.ForeColor = warning ? ColorWarning : ColorMuted;
    }

    private void RestartElevated()
    {
        var exePath = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(exePath))
        {
            Log("无法以管理员身份重启: 找不到当前程序路径");
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = exePath,
                UseShellExecute = true,
                Verb = "runas"
            });
            Log("已请求管理员权限，当前窗口即将退出");
            Close();
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == 1223)
        {
            Log("已取消管理员权限请求");
        }
        catch (Exception ex)
        {
            Log($"以管理员身份重启失败: {ex.Message}");
        }
    }

    private void Log(string message)
    {
        var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
        logTextBox.AppendText(line + Environment.NewLine);

        try
        {
            File.AppendAllText(_logFilePath, line + Environment.NewLine, Encoding.UTF8);
        }
        catch
        {
        }
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern nint SendMessage(nint hWnd, uint msg, nint wParam, nint lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool PostMessage(nint hWnd, uint msg, nint wParam, nint lParam);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(nint hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool ShowWindowAsync(nint hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(nint hWnd, nint hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(nint hWnd);

    [DllImport("user32.dll")]
    private static extern bool IsIconic(nint hWnd);

    [DllImport("kernel32.dll", EntryPoint = "SetLastError")]
    private static extern void SetLastError(uint dwErrCode);
}

internal sealed record PotPlayerWindow(nint Handle, uint ProcessId, string Title, string ProcessName, bool IsElevated);

internal static class ProcessIntegrity
{
    private const uint ProcessQueryLimitedInformation = 0x1000;
    private const uint TokenQuery = 0x0008;
    private const int TokenElevationClass = 20;

    public static bool IsCurrentProcessElevated()
    {
        using var identity = WindowsIdentity.GetCurrent();
        return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
    }

    public static bool IsProcessElevated(uint processId)
    {
        var processHandle = OpenProcess(ProcessQueryLimitedInformation, false, processId);
        if (processHandle == nint.Zero)
        {
            return true;
        }

        try
        {
            if (!OpenProcessToken(processHandle, TokenQuery, out var tokenHandle))
            {
                return Marshal.GetLastWin32Error() == 5;
            }

            try
            {
                var elevation = new TokenElevation();
                var size = Marshal.SizeOf<TokenElevation>();
                if (!GetTokenInformation(tokenHandle, TokenElevationClass, out elevation, size, out _))
                {
                    return true;
                }

                return elevation.TokenIsElevated != 0;
            }
            finally
            {
                _ = CloseHandle(tokenHandle);
            }
        }
        finally
        {
            _ = CloseHandle(processHandle);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct TokenElevation
    {
        public int TokenIsElevated;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern nint OpenProcess(uint dwDesiredAccess, bool bInheritHandle, uint dwProcessId);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool OpenProcessToken(nint processHandle, uint desiredAccess, out nint tokenHandle);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool GetTokenInformation(
        nint tokenHandle,
        int tokenInformationClass,
        out TokenElevation tokenInformation,
        int tokenInformationLength,
        out int returnLength);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(nint hObject);
}

internal static class PotPlayerWindowFinder
{
    private const int GwOwner = 4;
    private const int GwlExStyle = -20;
    private const long WsExToolWindow = 0x00000080;
    private const long WsExAppWindow = 0x00040000;

    public static IReadOnlyList<PotPlayerWindow> FindAll()
    {
        var candidates = new List<(nint Handle, uint ProcessId, string Title, string ProcessName, bool IsElevated, int Score)>();
        var currentProcessId = (uint)Environment.ProcessId;

        EnumWindows((hWnd, _) =>
        {
            if (!IsMainWindow(hWnd) || IsToolWindow(hWnd))
            {
                return true;
            }

            GetWindowThreadProcessId(hWnd, out var pid);
            if (pid == 0 || pid == currentProcessId)
            {
                return true;
            }

            Process process;
            try
            {
                process = Process.GetProcessById((int)pid);
            }
            catch
            {
                return true;
            }

            var processName = process.ProcessName;
            if (!IsPotPlayerProcess(processName))
            {
                return true;
            }

            var title = GetWindowTextString(hWnd);
            candidates.Add((hWnd, pid, title, processName, ProcessIntegrity.IsProcessElevated(pid), ScoreWindow(hWnd, title)));
            return true;
        }, nint.Zero);

        return candidates
            .GroupBy(candidate => candidate.ProcessId)
            .Select(group => group.OrderByDescending(candidate => candidate.Score).First())
            .Select(candidate => new PotPlayerWindow(
                candidate.Handle,
                candidate.ProcessId,
                string.IsNullOrWhiteSpace(candidate.Title) ? candidate.ProcessName : candidate.Title,
                candidate.ProcessName,
                candidate.IsElevated))
            .ToList();
    }

    private static bool IsPotPlayerProcess(string processName)
    {
        return string.Equals(processName, "PotPlayerMini64", StringComparison.OrdinalIgnoreCase)
            || string.Equals(processName, "PotPlayerMini", StringComparison.OrdinalIgnoreCase)
            || string.Equals(processName, "PotPlayerMini32", StringComparison.OrdinalIgnoreCase);
    }

    private static int ScoreWindow(nint hWnd, string title)
    {
        var score = 0;
        if (!string.IsNullOrWhiteSpace(title))
        {
            score += 4;
        }

        if (IsWindowVisible(hWnd) || IsIconic(hWnd))
        {
            score += 2;
        }

        return score;
    }

    private static bool IsToolWindow(nint hWnd)
    {
        var exStyle = GetWindowLongPtr(hWnd, GwlExStyle).ToInt64();
        return (exStyle & WsExToolWindow) != 0 && (exStyle & WsExAppWindow) == 0;
    }

    private static string GetWindowTextString(nint hWnd)
    {
        var length = GetWindowTextLength(hWnd);
        var sb = new StringBuilder(length + 1);
        _ = GetWindowText(hWnd, sb, sb.Capacity);
        return sb.ToString();
    }

    private static bool IsMainWindow(nint hWnd)
    {
        return GetWindow(hWnd, GwOwner) == nint.Zero;
    }

    private delegate bool EnumWindowsProc(nint hWnd, nint lParam);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, nint lParam);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(nint hWnd);

    [DllImport("user32.dll")]
    private static extern bool IsIconic(nint hWnd);

    [DllImport("user32.dll")]
    private static extern int GetWindowTextLength(nint hWnd);

    [DllImport("user32.dll")]
    private static extern nint GetWindow(nint hWnd, uint uCmd);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(nint hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(nint hWnd, out uint lpdwProcessId);

    private static nint GetWindowLongPtr(nint hWnd, int nIndex)
    {
        return nint.Size == 8
            ? GetWindowLongPtr64(hWnd, nIndex)
            : GetWindowLong32(hWnd, nIndex);
    }

    [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
    private static extern nint GetWindowLong32(nint hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    private static extern nint GetWindowLongPtr64(nint hWnd, int nIndex);
}

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
    private const int WmHotkey = 0x0312;
    private const uint ModControl = 0x0002;
    private const uint ModAlt = 0x0001;
    private const uint ModNoRepeat = 0x4000;
    private const uint VkPageUp = 0x21;
    private const uint VkPageDown = 0x22;
    private const uint VkH = 0x48;
    private const uint VkJ = 0x4A;
    private const uint VkK = 0x4B;
    private const uint VkL = 0x4C;
    private const int PlayPauseHotkeyId = 1001;
    private const int GoToStartHotkeyId = 1002;
    private const int ShowAllHotkeyId = 1003;
    private const int MinimizeAllHotkeyId = 1004;
    private const int RewindHotkeyId = 1005;
    private const int ForwardHotkeyId = 1006;
    private const int DefaultSeekSeconds = 5;

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
    private const int ContentWidth = 436;
    private const int FormWidth = 460;
    private const string IconGoToStart = "\uE100";
    private const string IconRewind = "\uEB9E";
    private const string IconPlayPause = "\uE768";
    private const string IconForward = "\uEB9D";
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

    private readonly string _logFilePath;
    private readonly string _settingsFilePath;
    private readonly bool _isElevated = ProcessIntegrity.IsCurrentProcessElevated();
    private readonly TimeSpan _toggleCooldown = TimeSpan.FromMilliseconds(400);
    private readonly TimeSpan _seekCooldown = TimeSpan.FromMilliseconds(120);
    private DateTime _lastToggleAt = DateTime.MinValue;
    private bool _toggleInProgress;
    private bool _loadingSettings;
    private bool _windowListExpanded;
    private bool _logExpanded;
    private int _discoveredWindowCount;

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
        ApplyDetailsLayout();
        RefreshWindowList();
        Log(_isElevated ? "应用启动（管理员权限）" : "应用启动（普通权限）");
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

        Place(goToStartButton, IconGoToStart, "回到起始点 (Ctrl+Alt+H)");
        Place(rewindButton, IconRewind, "后退 (Ctrl+Alt+J)");
        Place(toggleButton, IconPlayPause, "播放/暂停全部 (Ctrl+Alt+K)", primary: true);
        Place(forwardButton, IconForward, "快进 (Ctrl+Alt+L)");
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

        Place(showAllButton, IconShowAll, "显示全部并置顶 (Ctrl+Alt+PageUp)");
        Place(minimizeAllButton, IconMinimize, "最小化全部 (Ctrl+Alt+PageDown)");
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
        var active = button == pinTopButton && TopMost;
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
        if (_windowListExpanded)
        {
            listBox.Location = new Point(ContentLeft, y);
            y += ListHeight + SectionGap;
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

    private void UpdateSectionHeaders()
    {
        var chevronList = _windowListExpanded ? "▾" : "▸";
        var chevronLog = _logExpanded ? "▾" : "▸";
        var count = _discoveredWindowCount > 0 ? $"  ·  {_discoveredWindowCount}" : "";
        windowListToggle.Text = $"{chevronList}  窗口列表{count}";
        logToggle.Text = $"{chevronLog}  运行日志";
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        RegisterGlobalHotkey(PlayPauseHotkeyId, VkK, "Ctrl+Alt+K");
        RegisterGlobalHotkey(GoToStartHotkeyId, VkH, "Ctrl+Alt+H");
        RegisterGlobalHotkey(RewindHotkeyId, VkJ, "Ctrl+Alt+J");
        RegisterGlobalHotkey(ForwardHotkeyId, VkL, "Ctrl+Alt+L");
        RegisterGlobalHotkey(ShowAllHotkeyId, VkPageUp, "Ctrl+Alt+PageUp");
        RegisterGlobalHotkey(MinimizeAllHotkeyId, VkPageDown, "Ctrl+Alt+PageDown");
    }

    private void RegisterGlobalHotkey(int id, uint virtualKey, string displayName)
    {
        var registered = RegisterHotKey(Handle, id, ModControl | ModAlt | ModNoRepeat, virtualKey);
        Log(registered ? $"全局热键注册成功: {displayName}" : $"全局热键注册失败: {displayName}");
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _ = UnregisterHotKey(Handle, PlayPauseHotkeyId);
        _ = UnregisterHotKey(Handle, GoToStartHotkeyId);
        _ = UnregisterHotKey(Handle, ShowAllHotkeyId);
        _ = UnregisterHotKey(Handle, MinimizeAllHotkeyId);
        _ = UnregisterHotKey(Handle, RewindHotkeyId);
        _ = UnregisterHotKey(Handle, ForwardHotkeyId);
        Log("应用退出");
        base.OnFormClosed(e);
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WmHotkey)
        {
            var hotkeyId = m.WParam.ToInt32();
            if (hotkeyId == PlayPauseHotkeyId)
            {
                Log("触发热键: Ctrl+Alt+K");
                RequestCommand("热键", "播放/暂停", TrySendPlayPause);
                return;
            }

            if (hotkeyId == GoToStartHotkeyId)
            {
                Log("触发热键: Ctrl+Alt+H");
                RequestCommand("热键", "回到起始点", TrySendGoToStart);
                return;
            }

            if (hotkeyId == ShowAllHotkeyId)
            {
                Log("触发热键: Ctrl+Alt+PageUp");
                RequestCommand("热键", "显示窗口", TryShowWindow);
                return;
            }

            if (hotkeyId == MinimizeAllHotkeyId)
            {
                Log("触发热键: Ctrl+Alt+PageDown");
                RequestCommand("热键", "最小化窗口", TryMinimizeWindow);
                return;
            }

            if (hotkeyId == RewindHotkeyId)
            {
                Log("触发热键: Ctrl+Alt+J");
                RequestSeek("热键", rewind: true);
                return;
            }

            if (hotkeyId == ForwardHotkeyId)
            {
                Log("触发热键: Ctrl+Alt+L");
                RequestSeek("热键", rewind: false);
                return;
            }
        }

        base.WndProc(ref m);
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

    private bool TrySendSeek(nint hwnd, int deltaMs)
    {
        SetLastError(0);
        var current = SendMessage(hwnd, WmUser, PotGetCurrentTime, nint.Zero);
        if (Marshal.GetLastWin32Error() == 5)
        {
            return false;
        }

        SetLastError(0);
        var total = SendMessage(hwnd, WmUser, PotGetTotalTime, nint.Zero);
        if (Marshal.GetLastWin32Error() == 5)
        {
            return false;
        }

        var currentMs = Math.Max(0L, current.ToInt64());
        var totalMs = Math.Max(0L, total.ToInt64());
        var target = currentMs + deltaMs;
        if (target < 0)
        {
            target = 0;
        }
        else if (totalMs > 0 && target > totalMs)
        {
            target = totalMs;
        }

        SetLastError(0);
        _ = SendMessage(hwnd, WmUser, PotSetCurrentTime, (nint)target);
        return Marshal.GetLastWin32Error() != 5;
    }

    private void UpdateSeekButtonTexts()
    {
        var seconds = SeekSeconds;
        toolTip.SetToolTip(rewindButton, $"后退 {seconds}秒 (Ctrl+Alt+J)");
        toolTip.SetToolTip(forwardButton, $"快进 {seconds}秒 (Ctrl+Alt+L)");
        toolTip.SetToolTip(seekSecondsUpDown, $"快进/后退时间跨度：{seconds} 秒");
    }

    private void LoadSettings()
    {
        _loadingSettings = true;
        try
        {
            var seconds = DefaultSeekSeconds;
            if (File.Exists(_settingsFilePath)
                && int.TryParse(File.ReadAllText(_settingsFilePath).Trim(), out var saved)
                && saved >= (int)seekSecondsUpDown.Minimum
                && saved <= (int)seekSecondsUpDown.Maximum)
            {
                seconds = saved;
            }

            seekSecondsUpDown.Value = seconds;
        }
        catch
        {
            seekSecondsUpDown.Value = DefaultSeekSeconds;
        }
        finally
        {
            _loadingSettings = false;
        }
    }

    private void SaveSettings()
    {
        if (_loadingSettings)
        {
            return;
        }

        try
        {
            File.WriteAllText(_settingsFilePath, SeekSeconds.ToString(), Encoding.UTF8);
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
        _discoveredWindowCount = windows.Count;
        listBox.Items.Clear();
        foreach (var window in windows)
        {
            var title = string.IsNullOrWhiteSpace(window.Title) ? window.ProcessName : window.Title;
            var elevationTag = window.IsElevated ? "  · 管理员" : "";
            listBox.Items.Add($"{title}{elevationTag}");
        }

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
    private static extern bool RegisterHotKey(nint hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(nint hWnd, int id);

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

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
    private const uint VkSpace = 0x20;
    private const uint VkHome = 0x24;
    private const uint VkLeft = 0x25;
    private const uint VkUp = 0x26;
    private const uint VkRight = 0x27;
    private const uint VkDown = 0x28;
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
    private const int DetailsTop = 208;
    private const int HeaderHeight = 28;
    private const int SectionGap = 6;
    private const int ListHeight = 190;
    private const int LogHeight = 236;
    private const int BottomPadding = 16;
    private const int ContentLeft = 16;
    private const int ContentWidth = 568;
    private const int FormWidth = 600;

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

    public MainForm()
    {
        InitializeComponent();

        Text = _isElevated ? "PotPlayer 多窗口控制（管理员）" : "PotPlayer 多窗口控制";
        elevateButton.Text = _isElevated ? "已是管理员" : "以管理员身份重启";
        elevateButton.Enabled = !_isElevated;
        UpdatePinTopButton();

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

    private void UpdatePinTopButton()
    {
        pinTopButton.Text = TopMost ? "取消控制窗口置顶" : "置顶控制窗口";
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
        windowListToggle.Text = _windowListExpanded ? "▼ 窗口列表" : "▶ 窗口列表";
        y += HeaderHeight + SectionGap;

        listBox.Visible = _windowListExpanded;
        if (_windowListExpanded)
        {
            listBox.Location = new Point(ContentLeft, y);
            y += ListHeight + SectionGap;
        }

        logToggle.Location = new Point(ContentLeft, y);
        logToggle.Text = _logExpanded ? "▼ 命令栏" : "▶ 命令栏";
        y += HeaderHeight + SectionGap;

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

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        var playPauseRegistered = RegisterHotKey(Handle, PlayPauseHotkeyId, ModControl | ModAlt | ModNoRepeat, VkSpace);
        Log(playPauseRegistered ? "全局热键注册成功: Ctrl+Alt+Space" : "全局热键注册失败: Ctrl+Alt+Space");

        var goToStartRegistered = RegisterHotKey(Handle, GoToStartHotkeyId, ModControl | ModAlt | ModNoRepeat, VkHome);
        Log(goToStartRegistered ? "全局热键注册成功: Ctrl+Alt+Home" : "全局热键注册失败: Ctrl+Alt+Home");

        var showAllRegistered = RegisterHotKey(Handle, ShowAllHotkeyId, ModControl | ModAlt | ModNoRepeat, VkUp);
        Log(showAllRegistered ? "全局热键注册成功: Ctrl+Alt+Up" : "全局热键注册失败: Ctrl+Alt+Up");

        var minimizeAllRegistered = RegisterHotKey(Handle, MinimizeAllHotkeyId, ModControl | ModAlt | ModNoRepeat, VkDown);
        Log(minimizeAllRegistered ? "全局热键注册成功: Ctrl+Alt+Down" : "全局热键注册失败: Ctrl+Alt+Down");

        var rewindRegistered = RegisterHotKey(Handle, RewindHotkeyId, ModControl | ModAlt | ModNoRepeat, VkLeft);
        Log(rewindRegistered ? "全局热键注册成功: Ctrl+Alt+Left" : "全局热键注册失败: Ctrl+Alt+Left");

        var forwardRegistered = RegisterHotKey(Handle, ForwardHotkeyId, ModControl | ModAlt | ModNoRepeat, VkRight);
        Log(forwardRegistered ? "全局热键注册成功: Ctrl+Alt+Right" : "全局热键注册失败: Ctrl+Alt+Right");
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
                Log("触发热键: Ctrl+Alt+Space");
                RequestCommand("热键", "播放/暂停", TrySendPlayPause);
                return;
            }

            if (hotkeyId == GoToStartHotkeyId)
            {
                Log("触发热键: Ctrl+Alt+Home");
                RequestCommand("热键", "回到起始点", TrySendGoToStart);
                return;
            }

            if (hotkeyId == ShowAllHotkeyId)
            {
                Log("触发热键: Ctrl+Alt+Up");
                RequestCommand("热键", "显示窗口", TryShowWindow);
                return;
            }

            if (hotkeyId == MinimizeAllHotkeyId)
            {
                Log("触发热键: Ctrl+Alt+Down");
                RequestCommand("热键", "最小化窗口", TryMinimizeWindow);
                return;
            }

            if (hotkeyId == RewindHotkeyId)
            {
                Log("触发热键: Ctrl+Alt+Left");
                RequestSeek("热键", rewind: true);
                return;
            }

            if (hotkeyId == ForwardHotkeyId)
            {
                Log("触发热键: Ctrl+Alt+Right");
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
                statusLabel.Text = "未找到 PotPlayer 窗口";
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

            statusLabel.Text = elevationBlocked > 0 && !_isElevated
                ? $"已发送{actionName} {success}/{windows.Count}，{elevationBlocked} 个管理员窗口无法控制，请以管理员身份重启"
                : $"已发送{actionName}到 {success}/{windows.Count} 个窗口";
            Log(statusLabel.Text);
            var resultStatus = statusLabel.Text;
            RefreshWindowList(windows);
            statusLabel.Text = resultStatus;
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
        rewindButton.Text = $"后退 {seconds}秒 (Ctrl+Alt+←)";
        forwardButton.Text = $"快进 {seconds}秒 (Ctrl+Alt+→)";
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
        listBox.Items.Clear();
        foreach (var window in windows)
        {
            var elevationTag = window.IsElevated ? "  [管理员]" : "";
            listBox.Items.Add($"0x{window.Handle.ToInt64():X8}  {window.ProcessName}{elevationTag}  {window.Title}");
        }

        var elevatedCount = windows.Count(window => window.IsElevated);
        if (!_isElevated && elevatedCount > 0)
        {
            statusLabel.Text = $"已发现 {windows.Count} 个窗口，其中 {elevatedCount} 个以管理员运行，当前无法控制";
            elevateButton.Enabled = true;
        }
        else
        {
            statusLabel.Text = $"已发现 {windows.Count} 个 PotPlayer 窗口";
        }

        Log(statusLabel.Text);
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
    public static IReadOnlyList<PotPlayerWindow> FindAll()
    {
        var windows = new List<PotPlayerWindow>();
        var seenProcessIds = new HashSet<uint>();
        var currentProcessId = (uint)Environment.ProcessId;

        EnumWindows((hWnd, _) =>
        {
            if (!IsWindowVisible(hWnd) && !IsIconic(hWnd))
            {
                return true;
            }

            if (!IsMainWindow(hWnd))
            {
                return true;
            }

            GetWindowThreadProcessId(hWnd, out var pid);
            if (pid == 0)
            {
                return true;
            }

            if (pid == currentProcessId)
            {
                return true;
            }

            if (!seenProcessIds.Add(pid))
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
            if (!string.Equals(processName, "PotPlayerMini64", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(processName, "PotPlayerMini", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(processName, "PotPlayerMini32", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            var title = GetWindowTextString(hWnd);
            if (string.IsNullOrWhiteSpace(title))
            {
                return true;
            }

            windows.Add(new PotPlayerWindow(hWnd, pid, title, processName, ProcessIntegrity.IsProcessElevated(pid)));
            return true;
        }, nint.Zero);

        return windows;
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

    private const uint GwOwner = 4;

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
}

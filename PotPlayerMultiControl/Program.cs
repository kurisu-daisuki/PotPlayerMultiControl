using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace PotPlayerMultiControl;

// 程序入口
internal static class Program
{
    [STAThread]
    static void Main()
    {
        // 初始化 WinForms 应用程序配置并运行主窗口
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}

/// <summary>
/// 主窗口：负责界面、热键注册、日志与向 PotPlayer 窗口发送播放/暂停命令。
/// </summary>
public sealed class MainForm : Form
{
    // Windows 消息与热键相关常量
    private const int WmHotkey = 0x0312;
    private const uint ModControl = 0x0002; // Ctrl
    private const uint ModAlt = 0x0001; // Alt
    private const uint ModNoRepeat = 0x4000; // 防止按键重复触发
    private const uint VkSpace = 0x20; // 空格键
    private const int HotkeyId = 1001; // 热键 ID

    // 用于向窗口发送媒体命令的消息与参数
    private const uint WmAppCommand = 0x0319;
    private const int AppCommandMediaPlayPause = 14; // APPCOMMAND_MEDIA_PLAY_PAUSE

    // UI 控件
    private readonly Label _statusLabel;
    private readonly Button _toggleButton;
    private readonly Button _refreshButton;
    private readonly ListBox _listBox;
    private readonly TextBox _logTextBox;

    // 日志文件路径（保存在 %LocalAppData%）
    private readonly string _logFilePath;

    // 防抖/冷却机制，避免快速重复操作
    private readonly TimeSpan _toggleCooldown = TimeSpan.FromMilliseconds(400);
    private DateTime _lastToggleAt = DateTime.MinValue;
    private bool _toggleInProgress;

    public MainForm()
    {
        // 基本窗口属性
        Text = "PotPlayer 多窗口控制";
        StartPosition = FormStartPosition.CenterScreen;
        Width = 560;
        Height = 580;

        // 准备日志目录并确定日志文件路径
        var logDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PotPlayerMultiControl");
        Directory.CreateDirectory(logDir);
        _logFilePath = Path.Combine(logDir, "app.log");

        // 状态标签
        _statusLabel = new Label
        {
            AutoSize = true,
            Left = 16,
            Top = 16,
            Text = "检测中..."
        };

        // 播放/暂停按钮（也对应全局热键）
        _toggleButton = new Button
        {
            Left = 16,
            Top = 44,
            Width = 220,
            Height = 36,
            Text = "播放/暂停全部 (Ctrl+Alt+Space)"
        };
        _toggleButton.Click += (_, _) => RequestToggle("按钮");

        // 刷新窗口列表按钮
        _refreshButton = new Button
        {
            Left = 246,
            Top = 44,
            Width = 110,
            Height = 36,
            Text = "刷新列表"
        };
        _refreshButton.Click += (_, _) => RefreshWindowList();

        // 窗口列表
        _listBox = new ListBox
        {
            Left = 16,
            Top = 92,
            Width = 520,
            Height = 210
        };

        // 日志显示区（只读、可滚动）
        _logTextBox = new TextBox
        {
            Left = 16,
            Top = 310,
            Width = 520,
            Height = 220,
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            ReadOnly = true
        };

        // 将控件加入窗体
        Controls.Add(_statusLabel);
        Controls.Add(_toggleButton);
        Controls.Add(_refreshButton);
        Controls.Add(_listBox);
        Controls.Add(_logTextBox);

        // 初始化显示与日志
        RefreshWindowList();
        Log("应用启动");
    }

    // 窗口句柄创建后注册全局热键
    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        var result = RegisterHotKey(Handle, HotkeyId, ModControl | ModAlt | ModNoRepeat, VkSpace);
        Log(result ? "全局热键注册成功: Ctrl+Alt+Space" : "全局热键注册失败");
    }

    // 窗口关闭时注销热键并记录日志
    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _ = UnregisterHotKey(Handle, HotkeyId);
        Log("应用退出");
        base.OnFormClosed(e);
    }

    // 处理窗口消息，用于捕获热键消息
    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WmHotkey && m.WParam.ToInt32() == HotkeyId)
        {
            Log("触发热键: Ctrl+Alt+Space");
            RequestToggle("热键");
            return;
        }

        base.WndProc(ref m);
    }

    // 请求一次播放/暂停操作（包含防抖与并发保护）
    private void RequestToggle(string source)
    {
        if (_toggleInProgress)
        {
            Log($"忽略重复触发: {source}（上次操作尚未完成）");
            return;
        }

        var now = DateTime.UtcNow;
        if (now - _lastToggleAt < _toggleCooldown)
        {
            Log($"忽略重复触发: {source}（冷却中）");
            return;
        }

        _lastToggleAt = now;
        ToggleAll();
    }

    // 向所有已发现的 PotPlayer 窗口发送播放/暂停命令
    private void ToggleAll()
    {
        _toggleInProgress = true;
        try
        {
            var windows = PotPlayerWindowFinder.FindAll();
            if (windows.Count == 0)
            {
                _statusLabel.Text = "未找到 PotPlayer 窗口";
                Log("播放/暂停失败: 未找到 PotPlayer 窗口");
                return;
            }

            var success = 0;
            foreach (var window in windows)
            {
                if (TrySendPlayPause(window.Handle))
                {
                    success++;
                    Log($"发送成功: 0x{window.Handle.ToInt64():X8} {window.Title}");
                }
                else
                {
                    Log($"发送失败: 0x{window.Handle.ToInt64():X8} {window.Title}");
                }
            }

            _statusLabel.Text = $"已发送播放/暂停到 {success}/{windows.Count} 个窗口";
            Log(_statusLabel.Text);
            RefreshWindowList(windows);
        }
        finally
        {
            _toggleInProgress = false;
        }
    }

    // 通过发送 WM_APPCOMMAND (APPCOMMAND_MEDIA_PLAY_PAUSE) 到目标窗口实现播放/暂停
    private bool TrySendPlayPause(nint hwnd)
    {
        var lParam = (nint)(AppCommandMediaPlayPause << 16);
        return SendMessage(hwnd, WmAppCommand, hwnd, lParam) != nint.Zero;
    }

    // 刷新并显示当前发现的 PotPlayer 窗口
    private void RefreshWindowList()
    {
        RefreshWindowList(PotPlayerWindowFinder.FindAll());
    }

    private void RefreshWindowList(IReadOnlyList<PotPlayerWindow> windows)
    {
        _listBox.Items.Clear();
        foreach (var window in windows)
        {
            _listBox.Items.Add($"0x{window.Handle.ToInt64():X8}  {window.ProcessName}  {window.Title}");
        }

        _statusLabel.Text = $"已发现 {windows.Count} 个 PotPlayer 窗口";
        Log(_statusLabel.Text);
    }

    // 将日志写入界面与文件（简单容错，写文件失败时忽略）
    private void Log(string message)
    {
        var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
        _logTextBox.AppendText(line + Environment.NewLine);

        try
        {
            File.AppendAllText(_logFilePath, line + Environment.NewLine, Encoding.UTF8);
        }
        catch
        {
        }
    }

    // Win32 热键/消息相关 P/Invoke
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(nint hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(nint hWnd, int id);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern nint SendMessage(nint hWnd, uint msg, nint wParam, nint lParam);
}

// 表示一个 PotPlayer 窗口的简短信息（句柄、标题与进程名）
internal sealed record PotPlayerWindow(nint Handle, string Title, string ProcessName);

/// <summary>
/// 窗口查找器：枚举系统窗口并筛选出属于 PotPlayer 的主窗口。
/// </summary>
internal static class PotPlayerWindowFinder
{
    public static IReadOnlyList<PotPlayerWindow> FindAll()
    {
        var windows = new List<PotPlayerWindow>();
        var seenProcessIds = new HashSet<uint>();
        var currentProcessId = (uint)Environment.ProcessId;

        // 枚举所有顶层窗口，过滤可见且为主窗口的窗口
        EnumWindows((hWnd, _) =>
        {
            if (!IsWindowVisible(hWnd))
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

            // 忽略本进程
            if (pid == currentProcessId)
            {
                return true;
            }

            // 每个进程只处理一次
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
            // 仅识别 PotPlayerMini 系列进程名
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

            windows.Add(new PotPlayerWindow(hWnd, title, processName));
            return true;
        }, nint.Zero);

        return windows;
    }

    // 获取窗口标题字符串的帮助方法
    private static string GetWindowTextString(nint hWnd)
    {
        var length = GetWindowTextLength(hWnd);
        var sb = new StringBuilder(length + 1);
        _ = GetWindowText(hWnd, sb, sb.Capacity);
        return sb.ToString();
    }

    // 判断是否为主窗口（无 owner）
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
    private static extern int GetWindowTextLength(nint hWnd);

    [DllImport("user32.dll")]
    private static extern nint GetWindow(nint hWnd, uint uCmd);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(nint hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(nint hWnd, out uint lpdwProcessId);
}

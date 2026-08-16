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

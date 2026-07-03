using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

class Program
{
    [DllImport("kernel32.dll")]
    private static extern IntPtr GetConsoleWindow();

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    private const int SW_HIDE = 0;

    [STAThread]
    static void Main()
    {
        Console.WriteLine("程序已启动");
        
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        Application.ThreadException += Application_ThreadException;

        var consoleWindow = GetConsoleWindow();
        if (consoleWindow != IntPtr.Zero)
        {
#if !DEBUG
            ShowWindow(consoleWindow, SW_HIDE);
#endif
        }

        string appPath = AppDomain.CurrentDomain.BaseDirectory;
        string dataFolderPath = Path.Combine(appPath, "Data");

        if (!Directory.Exists(dataFolderPath))
        {
            Directory.CreateDirectory(dataFolderPath);
        }

        string defaultJsonPath = Path.Combine(dataFolderPath, "Default.json");

        if (!File.Exists(defaultJsonPath))
        {
            File.WriteAllText(defaultJsonPath, string.Empty);
        }
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new TrayApplicationContext(appPath, dataFolderPath));
    }

    private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    private static void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
    {
        Console.WriteLine($"Error: {e.Exception.Message}");
    }
}

class TrayApplicationContext : ApplicationContext
{
    private readonly NotifyIcon _trayIcon;
    private readonly string _dataFolderPath;
    private readonly string _appPath;

    public TrayApplicationContext(string appPath, string dataFolderPath)
    {
        _appPath = appPath;
        _dataFolderPath = dataFolderPath;

        var menu = new ContextMenuStrip();
        
        menu.Items.Add("设置", null, OpenExamSettings);
        menu.Items.Add("打开数据文件夹", null, OpenDataFolder);
        menu.Items.Add("重启", null, Restart);
        menu.Items.Add("退出", null, Exit);

        _trayIcon = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Text = "ExamHelper",
            ContextMenuStrip = menu,
            Visible = true
        };

        _trayIcon.DoubleClick += OpenDataFolder;
    }

    private void OpenDataFolder(object? sender, EventArgs e)
    {
        try
        {
            Console.WriteLine($"[ExamHelper.Services] 打开数据文件夹: {_dataFolderPath}");
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = _dataFolderPath,
                UseShellExecute = true,
                Verb = "open"
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ExamHelper.Services] 打开数据文件夹失败: {ex.Message}");
        }
    }

    private void OpenExamSettings(object? sender, EventArgs e)
    {
        try
        {
            Console.WriteLine("[ExamHelper.Services] 启动设置程序");
            var thread = new System.Threading.Thread(() =>
            {
                try
                {
                    ExamSettings.App.Main();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ExamHelper.Services] 启动设置程序失败: {ex.Message}");
                }
            });
            thread.SetApartmentState(System.Threading.ApartmentState.STA);
            thread.Start();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ExamHelper.Services] 启动设置程序失败: {ex.Message}");
            try
            {
                _trayIcon.ShowBalloonTip(5000, "启动失败", "无法启动 ExamSettings。", ToolTipIcon.Error);
            }
            catch { }
        }
    }

    private void Restart(object? sender, EventArgs e)
    {
        _trayIcon.Visible = false;
        _trayIcon.Dispose();
        Application.Restart();
    }

    private void Exit(object? sender, EventArgs e)
    {
        _trayIcon.Visible = false;
        _trayIcon.Dispose();
        Application.Exit();
    }
}
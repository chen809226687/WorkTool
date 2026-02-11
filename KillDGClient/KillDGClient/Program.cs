using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;

internal static class Program
{
    private static NotifyIcon? notifyIcon;
    private static SynchronizationContext? uiContext;
    private static CancellationTokenSource? cts;

    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        uiContext = SynchronizationContext.Current;
        cts = new CancellationTokenSource();

        notifyIcon = new NotifyIcon
        {
            Icon = LoadIcon(),
            Visible = true,
            Text = "Process Monitor Running",
            ContextMenuStrip = new ContextMenuStrip()
        };

        var exitItem = new ToolStripMenuItem("退出");
        exitItem.Click += (_, __) => Application.Exit();
        notifyIcon.ContextMenuStrip.Items.Add(exitItem);

        Application.ApplicationExit += (_, __) =>
        {
            cts?.Cancel();
            if (notifyIcon != null)
            {
                notifyIcon.Visible = false;
                notifyIcon.Dispose();
            }
        };

        var thread = new Thread(() => MonitorProcess(cts.Token))
        {
            IsBackground = true
        };
        thread.Start();

        Application.Run(new ApplicationContext());
    }

    private static Icon LoadIcon()
    {
        var iconPath = Path.Combine(AppContext.BaseDirectory, "kill.ico");
        if (File.Exists(iconPath))
        {
            try
            {
                return new Icon(iconPath);
            }
            catch
            {
                // Fall through to default icon
            }
        }

        return SystemIcons.Application;
    }

    private static void MonitorProcess(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            Process[] processes = Array.Empty<Process>();
            try
            {
                processes = Process.GetProcessesByName("DGClientAssist");
            }
            catch (Exception ex)
            {
                ShowBalloon("错误", "无法枚举进程: " + ex.Message);
            }

            foreach (var process in processes)
            {
                try
                {
                    process.Kill();
                }
                catch (Exception ex)
                {
                    ShowBalloon("错误", "无法终止进程: " + ex.Message);
                }
            }

            Thread.Sleep(100);
        }
    }

    private static void ShowBalloon(string title, string text)
    {
        if (notifyIcon == null)
        {
            return;
        }

        void Show()
        {
            notifyIcon.BalloonTipTitle = title;
            notifyIcon.BalloonTipText = text;
            notifyIcon.ShowBalloonTip(3000);
        }

        if (uiContext != null)
        {
            uiContext.Post(_ => Show(), null);
        }
        else
        {
            Show();
        }
    }
}

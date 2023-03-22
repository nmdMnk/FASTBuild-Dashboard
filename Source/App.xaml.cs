using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using FastBuild.Dashboard.Configuration;
using FastBuild.Dashboard.Support;

namespace FastBuild.Dashboard;

public partial class App : ISingleInstanceApp
{
    public App()
    {
        InitializeComponent();
        Current = this;
    }

    public new static App Current { get; private set; }
#if DEBUG
    public static bool IsInDesignTime { get; } = DesignerProperties.GetIsInDesignMode(new DependencyObject());
#endif
    public bool StartMinimized { get; private set; }
    public bool DoNotSpawnShadowExecutable { get; private set; }
    public bool IsShadowProcess { get; private set; }
    public ShadowContext ShadowContext { get; private set; }

    public bool SignalExternalCommandLineArgs(IList<string> args)
    {
        Application.Current.MainWindow.Show();
        Application.Current.MainWindow.Activate();
        return true;
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        ProcessArgs(e.Args);
        SettingsBase.Initialize();
        base.OnStartup(e);
    }

    internal void RaiseOnDeactivated()
    {
        this.OnDeactivated(EventArgs.Empty);
    }

    /*
    public bool SetStartupWithWindows(bool startUp)
    {
        var key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
        var entryAssembly = Assembly.GetEntryAssembly();

        if (key != null && !string.IsNullOrEmpty(entryAssembly.Location))
        {
            if (startUp)
            {
                var location = entryAssembly.Location;
                if (this.ShadowContext != null && !string.IsNullOrEmpty(this.ShadowContext.OriginalLocation))
                {
                    location = this.ShadowContext.OriginalLocation;
                }
                Debug.Assert(location != null, "location != null");

                key.SetValue(entryAssembly.GetName().Name, $"\"{location}\" -minimized");
            }
            else
            {
                key.DeleteValue(entryAssembly.GetName().Name, false);
            }

            return true;
        }

        return false;
    }
    */

    public void ProcessArgs(string[] args)
    {
        IsShadowProcess = args.Contains(AppArguments.ShadowProc);

#if DEBUG_SHADOW_PROCESS
			// start debugger as early as possible
			if (App.Current.IsShadowProcess)
			{
				Debugger.Launch();
			}
#endif

        StartMinimized = args.Contains(AppArguments.StartMinimized);
        DoNotSpawnShadowExecutable = args.Contains(AppArguments.NoShadow);
        if (IsShadowProcess) 
            LoadShadowContext();
    }

    private void LoadShadowContext()
    {
        try
        {
            ShadowContext = ShadowContext.Load();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to start FASTBuild Dashboard: error spawn shadow process.\n\n {ex.Message}",
                "FASTBuild Dashboard", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            Environment.Exit(-1);
        }
    }

    public static class CachedResource<T>
    {
        private static readonly Dictionary<string, T> CachedResources = new();

        public static T GetResource(string key)
        {
#if DEBUG
            if (IsInDesignTime) return default;
#endif

            if (!CachedResources.TryGetValue(key, out var resource))
            {
                resource = (T)Current.FindResource(key);
                CachedResources.Add(key, resource);
            }

            return resource;
        }
    }
}
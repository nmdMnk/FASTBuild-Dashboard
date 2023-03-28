using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using Caliburn.Micro;
using FastBuild.Dashboard.Services;
using FastBuild.Dashboard.Services.Build;
using FastBuild.Dashboard.Services.Build.SourceEditor;
using FastBuild.Dashboard.Services.Worker;
using FastBuild.Dashboard.Support;
using FastBuild.Dashboard.ViewModels;
using NLog;

namespace FastBuild.Dashboard;

internal class AppBootstrapper : BootstrapperBase
{
    private readonly SimpleContainer _container = new SimpleContainer();
    private static readonly Logger Logger = NLog.LogManager.GetCurrentClassLogger();

    public AppBootstrapper()
    {
        Logger.Info($"FBuild Dashboard");
        Logger.Info($"Version: {Assembly.GetExecutingAssembly().GetName().Version}");

        Initialize();
    }

    protected override void Configure()
    {
        Logger.Info("Configuring container...");
        _container.Singleton<IWindowManager, WindowManager>();
        _container.Singleton<IEventAggregator, EventAggregator>();
        _container.Singleton<IBuildViewportService, BuildViewportService>();
        _container.Singleton<IBrokerageService, BrokerageService>();
        _container.Singleton<IWorkerAgentService, WorkerAgentService>();
        _container.Singleton<IExternalSourceEditorService, ExternalSourceEditorService>();
        
        _container.PerRequest<MainWindowViewModel>();
    }
    protected override object GetInstance(Type service, string key)
    {
        return _container.GetInstance(service, key);
    }
    protected override IEnumerable<object> GetAllInstances(Type service)
    {
        return _container.GetAllInstances(service);
    }
    protected override void BuildUp(object instance)
    {
        _container.BuildUp(instance);
    }

    protected override void OnStartup(object sender, StartupEventArgs e)
    {
#if DEBUG && !DEBUG_SINGLE_INSTANCE
        Logger.Warn("Running in DEBUG!");
        Logger.Info("Display Main Window");
        DisplayRootViewForAsync<MainWindowViewModel>();
#else
        var assemblyLocation = Assembly.GetEntryAssembly()?.Location;

        var identifier = assemblyLocation?.Replace('\\', '_');
        if (!SingleInstance<App>.InitializeAsFirstInstance(identifier))
        {
            Logger.Warn($"Exiting as this is not the first instance!");
            Environment.Exit(0);
        }

        if (App.Current.DoNotSpawnShadowExecutable || App.Current.IsShadowProcess)
        {
            Logger.Info($"Display Main Window");
            DisplayRootViewForAsync<MainWindowViewModel>();
        }
        else
        {
            SpawnShadowProcess(e, assemblyLocation);
            Environment.Exit(0);
        }
#endif
    }
    protected override IEnumerable<Assembly> SelectAssemblies()
    {
        return new[] { Assembly.GetExecutingAssembly() };
    }

    private static void CreateShadowContext(string shadowPath)
    {
        var shadowContext = new ShadowContext();
        shadowContext.Save(shadowPath);
    }

    private static void SpawnShadowProcess(StartupEventArgs e, string assemblyLocation)
    {
        var assemblyLocationNoExtension = $"{Path.GetFileNameWithoutExtension(assemblyLocation)}";
        var assemblyDirectory = Path.GetDirectoryName(assemblyLocation);
        var shadowAssemblyName = $"{assemblyLocationNoExtension}.shadow.exe";
        var shadowAssemblyPath = Path.Combine(Path.GetTempPath(), "FBDashboard", shadowAssemblyName);
        var shadowDirectory = Path.GetDirectoryName(shadowAssemblyPath);
        Logger.Info($"Trying to spawn shadow process at {shadowAssemblyPath}");
        try
        {
            if (File.Exists(shadowAssemblyPath))
            {
                Logger.Info("Deleting current shadow exe");
                File.Delete(shadowAssemblyPath);
            }

            Debug.Assert(assemblyLocation != null, "assemblyLocation != null");
            Directory.CreateDirectory(shadowDirectory);
            Logger.Info("Copying shadow exe");
            File.Copy(assemblyLocation, shadowAssemblyPath);

            Logger.Info("Copying NLog.config");
            var shadowNLogConfigPath = Path.Combine(shadowDirectory, "NLog.config");
            var NLogConfigPath = Path.Combine($"{assemblyDirectory}","NLog.config");
            File.Copy(NLogConfigPath, shadowNLogConfigPath, true);
            
            var workerFolder = Path.Combine(assemblyDirectory, "FBuild");
            var workerTargetFolder = Path.Combine(shadowDirectory, "FBuild");
            if (Directory.Exists(workerFolder))
            {
                Logger.Info("Copying FBuild folder");
                Directory.CreateDirectory(workerTargetFolder);
                // Copy all worker files.
                foreach (var newPath in Directory.GetFiles(workerFolder, "*.*", SearchOption.TopDirectoryOnly))
                    File.Copy(newPath, newPath.Replace(workerFolder, workerTargetFolder), true);
            }
            else
            {
                Logger.Error("FBuild folder not found! Unable to copy!");
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            // may be already running
            Logger.Error($"Unable to spawn shadow process: {ex.ToString()}");
        }
        catch (IOException ex)
        {
            // may be already running
            Logger.Error($"Unable to spawn shadow process: {ex.ToString()}");
        }

        CreateShadowContext(shadowAssemblyPath);
        SingleInstance<App>.Cleanup();

        Logger.Info($"Starting shadow process");
        Process.Start(new ProcessStartInfo
        {
            FileName = shadowAssemblyPath,
            Arguments = string.Join(" ", e.Args.Concat(new[] { AppArguments.ShadowProc }))
        });
    }

    protected override void OnExit(object sender, EventArgs e)
    {
        SingleInstance<App>.Cleanup();
        base.OnExit(sender, e);
    }
}
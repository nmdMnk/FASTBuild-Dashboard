using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Windows.Media;
using Caliburn.Micro;
using Caliburn.Micro.Validation;
using FastBuild.Dashboard.Configuration;
using FastBuild.Dashboard.Services;
using FastBuild.Dashboard.Services.Worker;
using Ookii.Dialogs.Wpf;

namespace FastBuild.Dashboard.ViewModels.Settings;

internal sealed partial class SettingsViewModel : ValidatingScreen<SettingsViewModel>, IMainPage
{
    public SettingsViewModel()
    {
        MaximumCores = (uint)Environment.ProcessorCount;
        CoreTicks = new DoubleCollection(Enumerable.Range(1, (int)MaximumCores).Select(i => (double)i));

        DisplayName = "Settings";

        var brokerageService = IoC.Get<IBrokerageService>();
        brokerageService.WorkerCountChanged += BrokerageService_WorkerCountChanged;

        var workerService = IoC.Get<IWorkerAgentService>();
        workerService.WorkerRunStateChanged += WorkerServiceOnWorkerRunStateChanged;
    }

    private void WorkerServiceOnWorkerRunStateChanged(object sender, WorkerRunStateChangedEventArgs e)
    {
        NotifyOfPropertyChange(nameof(WorkerStatusText));
    }

    [CustomValidation(typeof(SettingsValidator), "ValidateFolderPath")]
    public string BrokeragePath
    {
        get => IoC.Get<IBrokerageService>().BrokeragePath;
        set
        {
            IoC.Get<IBrokerageService>().BrokeragePath = value;
            NotifyOfPropertyChange();
        }
    }

    [CustomValidation(typeof(SettingsValidator), "ValidateFolderPath")]
    public string CachePath
    {
        get => Environment.GetEnvironmentVariable("FASTBUILD_CACHE_PATH");
        set
        {
            Environment.SetEnvironmentVariable("FASTBUILD_CACHE_PATH", value);
            NotifyOfPropertyChange();
        }
    }

    public string DisplayWorkersInPool
    {
        get
        {
            var workerCount = IoC.Get<IBrokerageService>().WorkerNames.Length;
            switch (workerCount)
            {
                case 0:
                    return "no workers in pool";
                case 1:
                    return "1 worker in pool";
                default:
                    return $"{workerCount} workers in pool";
            }
        }
    }

    public string WorkerStatusText
    {
        get
        {
            var worker = IoC.Get<IWorkerAgentService>();
            if (worker.IsPendingRestart)
                return "Settings will be applied when worker is idle. Waiting for worker to be idle ... ";
            
            return "";
        }
    }

    public int WorkerMode
    {
        get => (int)IoC.Get<IWorkerAgentService>().WorkerMode;
        set
        {
            IoC.Get<IWorkerAgentService>().WorkerMode = (WorkerSettings.WorkerModeSetting)value;
            WorkerModeChanged?.Invoke(this, (WorkerSettings.WorkerModeSetting)value);
            NotifyOfPropertyChange();
            NotifyOfPropertyChange(nameof(WorkerThresholdEnabled));
            NotifyOfPropertyChange(nameof(WorkerStatusText));
        }
    }

    public bool WorkerThresholdEnabled =>
        IoC.Get<IWorkerAgentService>().WorkerMode == WorkerSettings.WorkerModeSetting.WorkWhenIdle;

    public uint WorkerThreshold
    {
        get => IoC.Get<IWorkerAgentService>().WorkerThreshold / 10;
        set
        {
            IoC.Get<IWorkerAgentService>().WorkerThreshold = value * 10;
            NotifyOfPropertyChange();
            NotifyOfPropertyChange(nameof(DisplayThreshold));
            NotifyOfPropertyChange(nameof(WorkerStatusText));
        }
    }

    public string DisplayThreshold => $"{WorkerThreshold * 10}%";

    public uint WorkerCores
    {
        get => IoC.Get<IWorkerAgentService>().WorkerCores;
        set
        {
            IoC.Get<IWorkerAgentService>().WorkerCores = Math.Max(1, Math.Min(MaximumCores, value));
            NotifyOfPropertyChange();
            NotifyOfPropertyChange(nameof(DisplayCores));
            NotifyOfPropertyChange(nameof(WorkerStatusText));
        }
    }

    public string DisplayCores => WorkerCores == 1 ? "1 core" : $"up to {WorkerCores} cores";

    public bool StartWithWindows
    {
        get => AppSettings.Default.StartWithWindows;
        set
        {
            AppSettings.Default.StartWithWindows = value;
            AppSettings.Default.Save();
            App.Current.SetStartupWithWindows(value);
            this.NotifyOfPropertyChange();
        }
    }
    
    public uint WorkerMinFreeMemoryMiB
    {
        get => IoC.Get<IWorkerAgentService>().MinFreeMemoryMiB;
        set
        {
            IoC.Get<IWorkerAgentService>().MinFreeMemoryMiB = Math.Max(0, value);
            NotifyOfPropertyChange();
            NotifyOfPropertyChange(nameof(DisplayMinFreeMemoryMiB));
            NotifyOfPropertyChange(nameof(WorkerStatusText));
        }
    }
    public string DisplayMinFreeMemoryMiB => $"{WorkerMinFreeMemoryMiB} MiB";

    public uint MaximumCores { get; }
    public DoubleCollection CoreTicks { get; }

    public string Icon => "Settings";
    public event EventHandler<WorkerSettings.WorkerModeSetting> WorkerModeChanged;

    private void BrokerageService_WorkerCountChanged(object sender, EventArgs e)
    {
        NotifyOfPropertyChange(nameof(DisplayWorkersInPool));
    }

    public void BrowseBrokeragePath()
    {
        BrokeragePath = BrowseFolderPath("Browse Cache Path", BrokeragePath);
    }

    public void BrowseCachePath()
    {
        CachePath = BrowseFolderPath("Browse Cache Path", CachePath);
    }

    private string BrowseFolderPath(string description, string selectedPath)
    {
        var dialog = new VistaFolderBrowserDialog
        {
            Description = description,
            SelectedPath = selectedPath,
            ShowNewFolderButton = false
        };

        if (dialog.ShowDialog(App.Current.MainWindow) == true) 
            return dialog.SelectedPath;

        return null;
    }
}
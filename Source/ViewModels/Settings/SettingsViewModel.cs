using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Windows.Media;
using Caliburn.Micro;
using Caliburn.Micro.Validation;
using FastBuild.Dashboard.Services;
using FastBuild.Dashboard.Services.Worker;
using Ookii.Dialogs.Wpf;

namespace FastBuild.Dashboard.ViewModels.Settings;

internal sealed partial class SettingsViewModel : ValidatingScreen<SettingsViewModel>, IMainPage
{
    public SettingsViewModel()
    {
        MaximumCores = Environment.ProcessorCount;
        CoreTicks = new DoubleCollection(Enumerable.Range(1, MaximumCores).Select(i => (double)i));

        this.DisplayName = "Settings";

        var brokerageService = IoC.Get<IBrokerageService>();
        brokerageService.WorkerCountChanged += BrokerageService_WorkerCountChanged;
    }

    [CustomValidation(typeof(SettingsValidator), "ValidateFolderPath")]
    public string BrokeragePath
    {
        get => IoC.Get<IBrokerageService>().BrokeragePath;
        set
        {
            IoC.Get<IBrokerageService>().BrokeragePath = value;
            this.NotifyOfPropertyChange();
        }
    }

    [CustomValidation(typeof(SettingsValidator), "ValidateFolderPath")]
    public string CachePath
    {
        get => Environment.GetEnvironmentVariable("FASTBUILD_CACHE_PATH");
        set
        {
            Environment.SetEnvironmentVariable("FASTBUILD_CACHE_PATH", value);
            this.NotifyOfPropertyChange();
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

    public int WorkerMode
    {
        get => (int)IoC.Get<IWorkerAgentService>().WorkerMode;
        set
        {
            IoC.Get<IWorkerAgentService>().WorkerMode = (WorkerMode)value;
            WorkerModeChanged?.Invoke(this, (WorkerMode)value);
            this.NotifyOfPropertyChange();
            this.NotifyOfPropertyChange(nameof(WorkerThresholdEnabled));
        }
    }

    public bool WorkerThresholdEnabled =>
        IoC.Get<IWorkerAgentService>().WorkerMode == Services.Worker.WorkerMode.WorkWhenIdle;

    public int WorkerThreshold
    {
        get => (int)IoC.Get<IWorkerAgentService>().WorkerThreshold;
        set
        {
            IoC.Get<IWorkerAgentService>().WorkerThreshold = value;
            this.NotifyOfPropertyChange();
            this.NotifyOfPropertyChange(nameof(DisplayThreshold));
        }
    }

    public string DisplayThreshold => $"{WorkerThreshold * 10}%";

    public int WorkerCores
    {
        get => IoC.Get<IWorkerAgentService>().WorkerCores;
        set
        {
            IoC.Get<IWorkerAgentService>().WorkerCores = Math.Max(1, Math.Min(MaximumCores, value));
            this.NotifyOfPropertyChange();
            this.NotifyOfPropertyChange(nameof(DisplayCores));
        }
    }

    public string DisplayCores => WorkerCores == 1 ? "1 core" : $"up to {WorkerCores} cores";

    /*
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
    */

    public int MaximumCores { get; }
    public DoubleCollection CoreTicks { get; }

    public string Icon => "Settings";
    public event EventHandler<WorkerMode> WorkerModeChanged;

    private void BrokerageService_WorkerCountChanged(object sender, EventArgs e)
    {
        this.NotifyOfPropertyChange(nameof(DisplayWorkersInPool));
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

        if (dialog.ShowDialog(App.Current.MainWindow) == true) return dialog.SelectedPath;

        return null;
    }
}
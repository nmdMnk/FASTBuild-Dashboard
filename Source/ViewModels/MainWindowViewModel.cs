using System.Threading;
using System.Threading.Tasks;
using Caliburn.Micro;
using FastBuild.Dashboard.ViewModels.About;
using FastBuild.Dashboard.ViewModels.Broker;
using FastBuild.Dashboard.ViewModels.Build;
using FastBuild.Dashboard.ViewModels.Settings;
using FastBuild.Dashboard.ViewModels.Worker;

namespace FastBuild.Dashboard.ViewModels;

internal sealed class MainWindowViewModel : Conductor<IMainPage>.Collection.AllActive
{
    private IMainPage _currentPage;

    public MainWindowViewModel()
    {
        Items.Add(BuildWatcherPage);
        Items.Add(WorkerPage);
        Items.Add(BrokerPage);
        Items.Add(SettingsPage);
        Items.Add(AboutPage);

        CurrentPage = BuildWatcherPage;
        DisplayName = "FASTBuild Dashboard";
    }

    public BuildWatcherViewModel BuildWatcherPage { get; } = new();
    public WorkerViewModel WorkerPage { get; } = new();
    public BrokerViewModel BrokerPage { get; } = new();
    public SettingsViewModel SettingsPage { get; } = new();
    public AboutViewModel AboutPage { get; } = new();

    public IMainPage CurrentPage
    {
        get => _currentPage;
        set
        {
            if (Equals(value, _currentPage)) return;

            _currentPage = value;
            NotifyOfPropertyChange();
        }
    }

    public override Task ActivateItemAsync(IMainPage item, CancellationToken cancellationToken = new CancellationToken())
    {
        CurrentPage = item;
        return base.ActivateItemAsync(item, cancellationToken);
    }
}
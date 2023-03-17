using System.Reflection;
using Caliburn.Micro;

namespace FastBuild.Dashboard.ViewModels.About;

internal class AboutViewModel : PropertyChangedBase, IMainPage
{
    public string Version => Assembly.GetExecutingAssembly().GetName().Version.ToString();
    public string Icon => "InformationOutline";
    public string DisplayName => "About";
}
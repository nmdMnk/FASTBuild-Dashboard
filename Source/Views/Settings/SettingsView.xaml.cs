using System.Windows.Controls;
using System.Windows.Input;

namespace FastBuild.Dashboard.Views.Settings;

/// <summary>
///     Interaction logic for SettingsView.xaml
/// </summary>
public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
    }

    private void WorkerMinFreeMemory_OnPreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        bool onlyNumbers = true;
        foreach (char c in e.Text)
        {
            if (!char.IsNumber(c))
            {
                onlyNumbers = false;
                break;
            }
        }

        e.Handled = !onlyNumbers;
    }
}
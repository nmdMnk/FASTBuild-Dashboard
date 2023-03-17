using System.IO;
using System.Windows;
using System.Windows.Input;
using Caliburn.Micro;
using FastBuild.Dashboard.Services.Build.SourceEditor;
using FastBuild.Dashboard.Support;

namespace FastBuild.Dashboard.ViewModels.Build;

internal class BuildErrorInfo
{
    public BuildErrorInfo(string filePath, int lineNumber, string errorMessage,
        BuildInitiatorProcessViewModel initiatorProcess)
    {
        FilePath = filePath;
        LineNumber = lineNumber;
        ErrorMessage = errorMessage;
        InitiatorProcess = initiatorProcess;
        OpenFileCommand = new SimpleCommand(ExecuteOpenFile, CanExecuteOpenFile);
    }

    public string FilePath { get; }
    public int LineNumber { get; }
    public string ErrorMessage { get; }
    public BuildInitiatorProcessViewModel InitiatorProcess { get; }
    public ICommand OpenFileCommand { get; }

    private bool CanExecuteOpenFile(object obj)
    {
        return File.Exists(FilePath);
    }

    private void ExecuteOpenFile(object obj)
    {
        if (!IoC.Get<IExternalSourceEditorService>()
                .OpenFile(FilePath, LineNumber, InitiatorProcess.InitiatorProcessId))
            MessageBox.Show(
                "Failed to open source file. Please go to the Settings page and check if the selected source editor is correctly configured.",
                "Open Source File",
                MessageBoxButton.OK,
                MessageBoxImage.Exclamation);
    }
}
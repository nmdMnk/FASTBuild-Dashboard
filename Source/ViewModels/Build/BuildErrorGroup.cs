using System.Collections.Generic;

namespace FastBuild.Dashboard.ViewModels.Build;

internal class BuildErrorGroup
{
    public BuildErrorGroup(string fileName, IEnumerable<BuildErrorInfo> errors)
    {
        FilePath = fileName;
        Errors = errors;
    }

    public string FilePath { get; }
    public IEnumerable<BuildErrorInfo> Errors { get; }
}
using System;

namespace FastBuild.Dashboard.Services.Build.SourceEditor;

internal class ExternalSourceEditorMetadata
{
    public ExternalSourceEditorMetadata(Type type, string key, string name, string description, bool allowOverridePath,
        bool allowSpecifyArgs, bool allowSpecifyAdditionalArgs)
    {
        Name = name;
        Description = description;
        AllowOverridePath = allowOverridePath;
        AllowSpecifyArgs = allowSpecifyArgs;
        AllowSpecifyAdditionalArgs = allowSpecifyAdditionalArgs;
        Key = key;
        Type = type;
    }

    public string Key { get; }
    public string Name { get; }
    public string Description { get; }
    public bool AllowOverridePath { get; }
    public bool AllowSpecifyArgs { get; }
    public bool AllowSpecifyAdditionalArgs { get; }
    public Type Type { get; }
}
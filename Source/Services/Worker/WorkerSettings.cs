using System;
using System.Collections.Generic;
using System.IO;
using FastBuild.Dashboard.Configuration;

namespace FastBuild.Dashboard.Services.Worker;

/// <summary>
/// Essentially this is a copy of FBuild/FBuildWorker/Worker/WorkerSettings from FBuild
/// </summary>
public class WorkerSettings
{
    private const byte FbuildworkerSettingsMinVersion = 1; // Oldest compatible version
    private const byte FbuildworkerSettingsCurrentVersion = 4; // Current version

    public enum WorkerModeSetting : UInt32
    {
        Disabled = 0, // Don't work for anyone
        WorkWhenIdle = 1, // Work for others when idle
        WorkAlways = 2, // Work for others always
        WorkProportional = 3 // Work for others proportional to free CPU
    };

    public WorkerModeSetting WorkerMode
    {
        get => _workerMode;
        set
        {
            _workerMode = value;
            _settingsAreDirty = true;
            Save();
        }
    }

    public uint IdleThresholdPercent
    {
        get => _idleThresholdPercent;
        set
        {
            _idleThresholdPercent = value;
            _settingsAreDirty = true;
            Save();
        }
    }

    public uint NumCPUsToUse
    {
        get => _numCpUsToUse;
        set
        {
            _numCpUsToUse = value;
            _settingsAreDirty = true;
            Save();
        }
    }

    public bool StartMinimized => true; // We don't want to expose this setting

    public uint MinimumFreeMemoryMiB // Not stored in worker settings but in dashboard settings
    {
        get => AppSettings.Default.WorkerMinFreeMemoryMiB;
        set
        {
            AppSettings.Default.WorkerMinFreeMemoryMiB = value;
            AppSettings.Default.Save();
            _settingsAreDirty = true;
        }
    }

    public bool SettingsAreDirty => _settingsAreDirty;

    private string SettingsPath => _workerExePath + ".settings";

    private readonly string _workerExePath;
    private bool _settingsAreDirty;
    private uint _numCpUsToUse;
    private WorkerModeSetting _workerMode;
    private uint _idleThresholdPercent;

    private bool _readWriteLock;

    public WorkerSettings(string workerExePath)
    {
        _workerExePath = workerExePath;
        Load();
    }

    public void Load()
    {
        if (_readWriteLock)
            return;

        _readWriteLock = true;
        
        if (!File.Exists(SettingsPath))
            return;

        byte[] bytesRead = File.ReadAllBytes(SettingsPath);
        int offset = 0;

        // Header
        byte settingsVersion = bytesRead[3];
        if (settingsVersion < FbuildworkerSettingsMinVersion ||
            settingsVersion > FbuildworkerSettingsCurrentVersion)
        {
            return; // Too old or new
        }

        offset += 4;

        // Settings
        WorkerMode = (WorkerModeSetting)BitConverter.ToUInt32(bytesRead, offset);
        offset += sizeof(UInt32);

        if (settingsVersion >= 4)
        {
            IdleThresholdPercent = BitConverter.ToUInt32(bytesRead, offset);
            offset += sizeof(UInt32);
        }

        NumCPUsToUse = BitConverter.ToUInt32(bytesRead, offset);
        offset += sizeof(UInt32);

        //StartMinimized = BitConverter.ToBoolean(bytesRead, offset);
        offset += sizeof(bool);

        // Reset dirty flag as it was just freshly laoded
        _settingsAreDirty = false;

        _readWriteLock = false;
    }

    public void Save()
    {
        if (_readWriteLock)
            return;

        _readWriteLock = true;
        List<byte> bytes = new List<byte>();
        
        // Header
        bytes.Add((byte)'F');
        bytes.Add((byte)'W');
        bytes.Add((byte)'S');
        bytes.Add(FbuildworkerSettingsCurrentVersion);

        // Settings
        bytes.AddRange(BitConverter.GetBytes((UInt32)WorkerMode));
        bytes.AddRange(BitConverter.GetBytes(IdleThresholdPercent));
        bytes.AddRange(BitConverter.GetBytes(NumCPUsToUse));
        bytes.AddRange(BitConverter.GetBytes(StartMinimized));

        File.WriteAllBytes(SettingsPath, bytes.ToArray());
        _readWriteLock = false;
    }
}
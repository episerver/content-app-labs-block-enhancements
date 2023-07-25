using System;
using EPiServer.Core;
using EPiServer.PlugIn;
using EPiServer.Scheduler;

namespace EPiServer.Labs.BlockEnhancements.SharedBlocksConverter;

[ScheduledPlugIn(DisplayName = "Remove empty assets pane folders", GUID = "a2229005-3e76-4886-b3c7-9a025a0c2603")]
public class CleanupFoldersJob : ScheduledJobBase
{
    private bool _stopSignaled;

    private readonly CleanupFolders _cleanupFolders;

    public CleanupFoldersJob(CleanupFolders cleanupFolders)
    {
        _cleanupFolders = cleanupFolders;
        IsStoppable = true;
    }

    public override void Stop()
    {
        _stopSignaled = true;
    }

    public override string Execute()
    {
        //Call OnStatusChanged to periodically notify progress of job for manually started jobs
        OnStatusChanged(String.Format("Starting execution of {0}", this.GetType()));

        var analyzedFolders = _cleanupFolders.Convert(ContentReference.RootPage, out var deletedFolders, OnStatusChanged);

        if (_stopSignaled)
        {
            return "Stop of job was called";
        }

        return $"Analyzed {analyzedFolders} folders and deleted {deletedFolders}";
    }
}

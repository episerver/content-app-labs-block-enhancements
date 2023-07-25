using System;
using EPiServer.Core;
using EPiServer.PlugIn;
using EPiServer.Scheduler;

namespace EPiServer.Labs.BlockEnhancements.SharedBlocksConverter;

[ScheduledPlugIn(DisplayName = "Convert shared blocks to local blocks", GUID = "a2319008-3e76-4886-b3c7-9a025a0c2603")]
public class ScheduledJobExample : ScheduledJobBase
{
    private bool _stopSignaled;

    private readonly ConvertSharedBlocks _convertSharedBlocks;

    public ScheduledJobExample(ConvertSharedBlocks convertSharedBlocks)
    {
        _convertSharedBlocks = convertSharedBlocks;
        IsStoppable = true;
    }

    public ScheduledJobExample()
    {
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

        var analyzedBlocksCount = _convertSharedBlocks.Convert(ContentReference.RootPage, out var convertedInlineBlocks, OnStatusChanged);

        if (_stopSignaled)
        {
            return "Stop of job was called";
        }

        return $"Analyzed {analyzedBlocksCount} shared blocks and converted {convertedInlineBlocks}";
    }
}

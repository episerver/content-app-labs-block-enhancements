using System;
using EPiServer.Core;
using EPiServer.PlugIn;
using EPiServer.Scheduler;

namespace EPiServer.Labs.BlockEnhancements.SharedBlocksConverter;

[ScheduledPlugIn(DisplayName = "Convert inline blocks to local blocks", GUID = "a1159008-3e76-4886-b3c7-9a025a0c2603")]
public class ConvertInlineBlocksJob : ScheduledJobBase
{
    private bool _stopSignaled;

    private readonly ConvertInlineBlocks _convertInlineBlocks;

    public ConvertInlineBlocksJob(ConvertInlineBlocks convertInlineBlocks)
    {
        _convertInlineBlocks = convertInlineBlocks;
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

        var analyzedInlineBlocks = _convertInlineBlocks.Convert(ContentReference.RootPage,
            out var convertedInlineBlocks, out var convertedContentItems, OnStatusChanged);

        if (_stopSignaled)
        {
            return "Stop of job was called";
        }

        return $"Analyzed {analyzedInlineBlocks} content items and converted {convertedInlineBlocks} in {convertedContentItems} content items";
    }
}

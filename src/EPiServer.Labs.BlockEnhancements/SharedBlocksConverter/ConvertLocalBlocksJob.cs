using System;
using EPiServer.Core;
using EPiServer.PlugIn;
using EPiServer.Scheduler;

namespace EPiServer.Labs.BlockEnhancements.SharedBlocksConverter;

[ScheduledPlugIn(DisplayName = "Inline local blocks into ContentArea properties", GUID = "a6119008-3e76-4886-b3c7-9a025a0c2603")]
public class ConvertLocalBlocksJob : ScheduledJobBase
{
    private bool _stopSignaled;

    private readonly ConvertLocalBlocks _convertLocalBlocks;

    public ConvertLocalBlocksJob(ConvertLocalBlocks convertLocalBlocks)
    {
        _convertLocalBlocks = convertLocalBlocks;
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

        var analyzedContentCount = _convertLocalBlocks.Convert(ContentReference.RootPage,
            out var convertedContentCount, OnStatusChanged);

        if (_stopSignaled)
        {
            return "Stop of job was called";
        }

        return $"Analyzed {analyzedContentCount} contents and converted {convertedContentCount} to inline blocks";
    }
}

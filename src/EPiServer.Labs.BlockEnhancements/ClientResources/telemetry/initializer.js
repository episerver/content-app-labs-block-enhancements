define([
    "episerver-labs-block-enhancements/telemetry/tracker",
    "episerver-labs-block-enhancements/telemetry/patch-cms-commands"
], function (
    tracker,
    patchCmsCommands
) {
    return function (options) {
        patchCmsCommands();

        var trackingOptions = Object.assign({}, options);

        tracker.trackEvent("featureOptions", trackingOptions);
    };
});

EPiServer.Labs.BlockEnhancements


Installation
============


In order to start using BlockEnhancements you need to add it explicitly to your site.
Please add the following statement to your Startup.cs


public class Startup
{
    ...
    public void ConfigureServices(IServiceCollection services)
    {
        ...
        services.AddBlockEnhancements();
        ...
    }
    ...
}

AddBlockEnhancements extension method also accepts optional parameter of Action<BlockEnhancementsOptions> which
lets you configure the add-on according to your needs.
Full documentation can be found here: https://github.com/episerver/episerver.labs.blockenhancements#episerver-labs---block-enhancements

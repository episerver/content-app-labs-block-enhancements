using System;
using System.Linq;
using EPiServer.Labs.BlockEnhancements.SharedBlocksConverter;
using EPiServer.Shell.Modules;
using Microsoft.Extensions.DependencyInjection;

namespace EPiServer.Labs.BlockEnhancements
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddBlockEnhancements(this IServiceCollection services)
        {
            services.Configure<ProtectedModuleOptions>(
                pm =>
                {
                    if (!pm.Items.Any(i =>
                        i.Name.Equals("episerver-labs-block-enhancements", StringComparison.OrdinalIgnoreCase)))
                    {
                        pm.Items.Add(new ModuleDetails { Name = "episerver-labs-block-enhancements" });
                    }
                });

            services.UseSystemTextJsonSerialization(new[] { typeof(SharedBlocksConverterPluginController) });

            return services;
        }
    }
}

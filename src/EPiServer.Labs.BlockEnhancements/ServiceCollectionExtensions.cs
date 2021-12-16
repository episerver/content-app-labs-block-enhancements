using System;
using System.Linq;
using EPiServer.Shell.Modules;
using Microsoft.Extensions.DependencyInjection;

namespace EPiServer.Labs.BlockEnhancements
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddBlockEnhancements(this IServiceCollection services,
            Action<BlockEnhancementsOptions> blockEnhancementsOptions = null)
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

            if (blockEnhancementsOptions != null)
            {
                services.Configure(blockEnhancementsOptions);
            }

            return services;
        }
    }
}
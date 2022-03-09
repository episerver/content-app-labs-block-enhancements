using EPiServer.Cms.Shell;
using EPiServer.Core;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using EPiServer.ServiceLocation;
using EPiServer.Shell.Modules;
using EPiServer.Shell.ObjectEditing;

namespace EPiServer.Labs.BlockEnhancements.InlineBlocksEditing
{
    [InitializableModule]
    [ModuleDependency(typeof(InitializableModule))]
    public class ContentAreaInitializableModule : IInitializableModule
    {
        public void Initialize(InitializationEngine context)
        {
            var metadataHandlerRegistry = context.Locate.Advanced.GetInstance<MetadataHandlerRegistry>();
            var options = context.Locate.Advanced.GetInstance<BlockEnhancementsOptions>();
            var moduleTable = context.Locate.Advanced.GetInstance<ModuleTable>();

            // we need to check if the assembly is loaded by episerver module system
            // this condition will be false if the Labs nuget was installed but the module was never added to protectedModules
            // by either calling AddBlockEnhancements() in Startup.cs or by adding the module to appsettings.json manually
            var isBlockEnhancementsModuleLoaded = moduleTable.TryGetModule(this.GetType().Assembly, out _);
            if (isBlockEnhancementsModuleLoaded)
            {
                metadataHandlerRegistry.RegisterMetadataHandler(typeof(ContentArea),
                    context.Locate.Advanced.GetInstance<ContentAreaDescriptor>(), options.ContentAreaSettings.UIHint,
                    options.ContentAreaSettings.ContentAreaEditorDescriptorBehavior);
            }
        }

        void IInitializableModule.Uninitialize(InitializationEngine context) { }
    }
}

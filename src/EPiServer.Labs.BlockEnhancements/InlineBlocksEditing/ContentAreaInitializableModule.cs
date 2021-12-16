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

            var isContentManagerModuleAdded = moduleTable.TryGetModule(this.GetType().Assembly, out _);
            if (isContentManagerModuleAdded)
            {
                metadataHandlerRegistry.RegisterMetadataHandler(typeof(ContentArea),
                    context.Locate.Advanced.GetInstance<ContentAreaDescriptor>(), options.ContentAreaSettings.UIHint,
                    options.ContentAreaSettings.ContentAreaEditorDescriptorBehavior);
            }
        }

        void IInitializableModule.Uninitialize(InitializationEngine context) { }
    }
}

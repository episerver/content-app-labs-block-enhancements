using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using EPiServer.ServiceLocation;
using EPiServer.Shell;
using EPiServer.Web;

namespace EPiServer.Labs.BlockEnhancements.InlineBlocksEditing
{
    [ModuleDependency(typeof(Web.InitializationModule))]
    public class DraftContentAreaPreviewInitializerInitializer : IConfigurableModule
    {
        public void ConfigureContainer(ServiceConfigurationContext context)
        {
            context.Services.Intercept<IContentAreaLoader>(
                (locator, defaultContentAreaLoader) => new CustomContentAreaLoader(defaultContentAreaLoader,
                    ServiceLocator.Current.GetInstance<IContextModeResolver>(),
                    ServiceLocator.Current.GetInstance<IContentVersionMapper>()));

            context.Services.Intercept<ViewConfiguration>((locator, view) =>
            {
                if (view.Key == CmsViewNames.OnPageEditView)
                {
                    view.ViewType = "episerver-labs-block-enhancements/inline-editing/OnPageEditing";
                }

                if (view.Key == CmsViewNames.AllPropertiesView)
                {
                    view.ViewType = "episerver-labs-block-enhancements/inline-editing/FormEditing";
                }

                return view;
            });
        }

        public void Initialize(InitializationEngine context)
        {

        }

        public void Uninitialize(InitializationEngine context)
        {

        }
    }
}

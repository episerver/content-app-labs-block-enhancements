using System;
using System.Collections.Generic;
using System.Linq;
using EPiServer.Cms.Shell.UI.Rest.Models.Internal;
using EPiServer.Core;
using EPiServer.Labs.BlockEnhancements.InlineBlocksEditing;
using EPiServer.Shell.Services.Rest;
using Microsoft.AspNetCore.Mvc;

namespace EPiServer.Labs.BlockEnhancements
{
    [RestStore("episerverlabsblockenhancements")]
    public class BlockEnhancementsStore : RestControllerBase
    {
        private readonly LatestContentVersionResolver _latestContentVersionResolver;
        private readonly DependenciesResolver _dependenciesResolver;
        private readonly IContentLoader _contentLoader;

        public BlockEnhancementsStore(LatestContentVersionResolver latestContentVersionResolver,
            DependenciesResolver dependenciesResolver, IContentLoader contentLoader)
        {
            _latestContentVersionResolver = latestContentVersionResolver;
            _dependenciesResolver = dependenciesResolver;
            _contentLoader = contentLoader;
        }

        /// <summary>
        /// Get either unpublished dependencies of a given ContentReference (id, default)
        /// or latest versions of a give list of ContentReferences (ids)
        /// or the first-non local owner of a given ContentReference (contentLink)
        /// </summary>
        [HttpGet]
        public ActionResult Get(ContentReference id, IEnumerable<ContentReference> ids, ContentReference contentLink)
        {
            if (id != null)
            {
                var dependencies = _dependenciesResolver.GetUnpublishedDependencies(id);
                return Rest(dependencies);
            }

            if (contentLink != null)
            {
                var localContentOwner = _contentLoader.GetAncestors(contentLink).FirstOrDefault(x =>
                    x is ContentAssetFolder folder && folder.ContentOwnerID != Guid.Empty) as ContentAssetFolder;

                if (localContentOwner == null)
                {
                    throw new InvalidOperationException("Cannot return owner for non-local content item");
                }

                var content = _contentLoader.Get<IContent>(localContentOwner.ContentOwnerID);
                return Rest(content.ContentLink);
            }

            var items = new List<EnhancedStructureStoreContentDataModel>();
            foreach (var itemId in ids)
            {
                items.Add(_latestContentVersionResolver.GetLatestVersion(itemId,
                    ControllerContext.HttpContext.Request.Query.ToNameValues()));
            }

            return Rest(items);
        }

        [HttpPost]
        public ActionResult ConvertToLocalBlock(ContentReference id)
        {
            return new EmptyResult();
        }
    }
}

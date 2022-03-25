using System.Collections.Specialized;
using Microsoft.AspNetCore.Http;

namespace EPiServer.Labs.BlockEnhancements
{
    internal static class QueryCollectionExtensions
    {
        public static NameValueCollection ToNameValues(this IQueryCollection query)
        {
            var nameValues = new NameValueCollection();
            foreach (var entry in query)
            {
                nameValues.Add(entry.Key, entry.Value.ToString());
            }
            return nameValues;
        }
    }
}

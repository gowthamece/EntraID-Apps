using Microsoft.Graph;

namespace TechnoNimbus_Entra
{
    public class CollectionProcessor<T>
    {
        /// <summary>
        /// Processes the MS Graph collection page.
        /// </summary>
        /// <param name="graphServiceClient">The graph service client.</param>
        /// <param name="collectionPage">The collection page.</param>
        /// <returns></returns>
        public static async Task<List<T>> ProcessGraphCollectionPageAsync(GraphServiceClient graphServiceClient, ICollectionPage<T> collectionPage, int maxRows = -1)
        {
            List<T> allItems = new List<T>();
            bool breaktime = false;

            var pageIterator = PageIterator<T>.CreatePageIterator(graphServiceClient, collectionPage, (item) =>
            {
                allItems.Add(item);
                //Debug.WriteLine($"1.allItems.Count-{allItems.Count}");

                if (maxRows != -1 && allItems.Count >= maxRows)
                {
                    breaktime = true;
                    return false;
                }

                return true;
            });

            // Start iteration
            await pageIterator.IterateAsync();

            while (pageIterator.State != PagingState.Complete)
            {
                //Debug.WriteLine($"2.allItems.Count-{allItems.Count}");

                if (breaktime)
                {
                    break;
                }

                // Keep iterating till complete.
                await pageIterator.ResumeAsync();
            }

            return allItems;
        }
    }
}

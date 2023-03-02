using System.Collections.Generic;
using System.Linq;


namespace Mail.Extensions
{
    internal static class ICollectionExtension
    {
        public static void AddRange<T>(this ICollection<T> Collection, IEnumerable<T> InputCollection)
        {
            foreach (T Item in InputCollection.ToArray())
            {
                Collection.Add(Item);
            }
        }

        public static void RemoveRange<T>(this ICollection<T> Collection, IEnumerable<T> InputCollection)
        {
            foreach (T Item in InputCollection.ToArray())
            {
                Collection.Remove(Item);
            }
        }
    }
}

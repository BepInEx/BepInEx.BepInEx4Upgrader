using System;
using System.Collections.Generic;
using System.Linq;

namespace Harmony
{
    public static class CollectionExtensions
    {
        public static void Do<T>(this IEnumerable<T> sequence, Action<T> action)
        {
            if (sequence == null) return;
            foreach (var obj in sequence) action(obj);
        }

        public static void DoIf<T>(this IEnumerable<T> sequence, Func<T, bool> condition, Action<T> action)
        {
            sequence.Where(condition).Do(action);
        }

        public static IEnumerable<T> Add<T>(this IEnumerable<T> sequence, T item)
        {
            return (sequence ?? Enumerable.Empty<T>()).Concat(new[]
            {
                item
            });
        }

        public static T[] AddRangeToArray<T>(this T[] sequence, T[] items)
        {
            return (sequence ?? Enumerable.Empty<T>()).Concat(items).ToArray();
        }

        public static T[] AddToArray<T>(this T[] sequence, T item)
        {
            return sequence.Add(item).ToArray();
        }
    }
}
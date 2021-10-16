using System;
using System.Collections.Generic;

namespace Observables.Extensions
{
    internal static class Extensions
    {
        internal static bool Contains<TElement>(this List<TElement> list, Predicate<TElement> predicate)
        {
            for (int startIndex = 0, endIndex = list.Count - 1; startIndex <= endIndex; startIndex++, endIndex--)
            {
                TElement currentStart = list[startIndex];
                TElement currentEnd = list[endIndex];
                if (predicate.Invoke(currentStart) || predicate.Invoke(currentEnd))
                {
                    return true;
                }
            }

            return false;
        }

        internal static bool RemoveWhere<TElement>(this List<TElement> list, Predicate<TElement> predicate)
        {
            for (int startIndex = 0, endIndex = list.Count - 1; startIndex <= endIndex; startIndex++, endIndex--)
            {
                TElement currentStart = list[startIndex];
                TElement currentEnd = list[endIndex];

                if (predicate.Invoke(currentStart))
                {
                    list.RemoveAt(startIndex);
                    return true;
                }

                if (predicate.Invoke(currentEnd))
                {
                    list.RemoveAt(endIndex);
                    return true;
                }
            }

            return false;
        }

        internal static WeakReference<TResult> GetWeakReferenceByInstance<TResult>(this List<WeakReference<TResult>> list, TResult obj)
            where TResult : class
        {
            for (int index = 0; index < list.Count; index++)
            {
                WeakReference<TResult> current = list[index];
                if (!current.TryGetTarget(out TResult target)) continue;
                if (!target.Equals(obj)) continue;

                return current;
            }

            return null;
        }
    }
}
using System;
using System.Collections.Generic;

namespace Observables.Extensions
{
    internal static class Extensions
    {
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
    }
}
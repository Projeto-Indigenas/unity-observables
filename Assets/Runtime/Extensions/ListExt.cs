using System;
using System.Collections.Generic;

namespace Observables.Extensions
{
    internal static class ListExt
    {
        internal static bool Contains<TElement>(this List<TElement> list, Predicate<TElement> predicate)
        {
            for (int startIndex = 0, endIndex = list.Count - 1; startIndex < endIndex; startIndex++, endIndex--)
            {
                if (predicate.Invoke(list[startIndex]) || predicate.Invoke(list[endIndex]))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
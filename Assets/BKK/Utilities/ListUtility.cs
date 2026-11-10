using System.Collections.Generic;

public static class ListUtility
{
    public static IList<T> Swap<T>(this IList<T> list, int indexA, int indexB)
    {
        (list[indexA], list[indexB]) = (list[indexB], list[indexA]);// ???

        return list;
    }
}

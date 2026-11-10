using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class ColorExtension
{
    public enum RGB
    {
        R,G,B
    }
    
    public enum Order
    {
        Normal,
        Ascending,
        Descending,
    }
    
    public static List<(RGB rgb, float value)> GetColorArray(this Color color, Order order = Order.Normal)
    {
        (RGB rgb, float value)[] cArr = { (RGB.R, color.r), (RGB.G, color.g), (RGB.B, color.b) };// 어떤게 R,G,B인지 체크하기 위해 튜플 활용 
        List<(RGB rgb, float value)> cList = null;

        switch (order)
        {
            case Order.Normal:
                cList = cArr.ToList();
                break;
            case Order.Ascending:
                cList = cArr.OrderBy(x => x.value).ToList();// 오름차순으로 정렬
                break;
            case Order.Descending:
                cList = cArr.OrderByDescending(x => x.value).ToList();// 오름차순으로 정렬
                break;
        }

        return cList;
    }
}

using System;
using UnityEngine;

namespace EditorEnhanced.Utils;

public static class ColorAssignment
{
    public const int HueRange = 128 * 3;

    public const int WhiteIndex = -1;
    public const int RedIndex = HueRange * 0 / 6;
    public const int YellowIndex = HueRange * 1 / 6;
    public const int GreenIndex = HueRange * 2 / 6;
    public const int CyanIndex = HueRange * 3 / 6;
    public const int BlueIndex = HueRange * 4 / 6;
    public const int MagentaIndex = HueRange * 5 / 6;

    public static int GetColorIndexEventBox(int eventBoxIdx, int idx = 0, bool distributed = false)
    {
        return distributed
            ? HueRange / 2 * eventBoxIdx + idx * 2 % HueRange
            : eventBoxIdx == 0
                ? WhiteIndex
                : (eventBoxIdx - 1) * HueRange / 12 % HueRange;
    }

    public static Color GetColorFromIndex(int index)
    {
        return Color.HSVToRGB(Convert.ToSingle(index) / Convert.ToSingle(HueRange), 1f, 1f);
    }
}
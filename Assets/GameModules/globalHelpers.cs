using UnityEngine;
using System.Collections.Generic;

// DELETE FOR PRODUCTION
public class globalHelpers : MonoBehaviour
{
    public static void printRectList(List<Rect> list)
    {
        string result = "";
        foreach (Rect rect in list)
        {
            result += $"({rect.x},{rect.y},{rect.width},{rect.height})";
        }
        Debug.Log(result);
    }
}

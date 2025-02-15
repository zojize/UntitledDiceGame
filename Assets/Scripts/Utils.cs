using System;
using System.Collections.Generic;
using UnityEngine;

public static class Utils
{
    private static readonly Dictionary<(float, int), List<Vector2>> pointsCache = new();
    public static List<Vector2> GeneratePoints(float r, int n)
    {
        var key = (r, n);
        if (pointsCache.TryGetValue(key, out var cachedPoints))
        {
            Debug.Log($"Using cached points for r={r} and n={n}");
            return cachedPoints;
        }

        if (n <= 0)
        {
            return new();
        }

        List<Vector2> points = new();

        if (n == 1)
        {
            points.Add(new Vector2(0, 0));
            pointsCache[key] = points;
            return new List<Vector2>(points);
        }

        if (n == 3)
        {
            points.Add(new Vector2(0, 2 * r / Mathf.Sqrt(3)));
            points.Add(new Vector2(-r, -r / Mathf.Sqrt(3)));
            points.Add(new Vector2(r, -r / Mathf.Sqrt(3)));
            pointsCache[key] = points;
            return points;
        }
        
        int currentLayer = 1;
        points.Add(new Vector2(0, 0)); // 中心点
        
        while (points.Count < n)
        {
            float circumference = 2 * Mathf.PI * (2 * r * currentLayer);
            int circlesInLayer = (int)(circumference / (2 * r));
            
            circlesInLayer = Mathf.Min(circlesInLayer, n - points.Count);
            
            for (int i = 0; i < circlesInLayer && points.Count < n; i++)
            {
                float angle = 2 * Mathf.PI * i / circlesInLayer;
                float radius = 2 * r * currentLayer;
                float x = radius * Mathf.Cos(angle);
                float y = radius * Mathf.Sin(angle);
                points.Add(new Vector2(x, y));
            }
            
            currentLayer++;
        }
        

        Debug.Log($"Generated {points.Count} points for r={r} and n={n}");

        pointsCache[key] = points;
        return points;
    }
}
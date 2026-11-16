using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace StickFigure;

public enum RecognizedShape { None, Circle, Rectangle }

public static class ShapeRecognizer
{
    public static RecognizedShape Recognize(IReadOnlyList<Point> points)
    {
        if (points.Count < 12) return RecognizedShape.None;
        double minX = points.Min(p => p.X), maxX = points.Max(p => p.X);
        double minY = points.Min(p => p.Y), maxY = points.Max(p => p.Y);
        double width = maxX - minX, height = maxY - minY;
        double diagonal = Math.Sqrt(width * width + height * height);
        if (width < 22 || height < 22 || diagonal < 34) return RecognizedShape.None;
        if ((points[0] - points[^1]).Length > Math.Max(16, diagonal * 0.18))
            return RecognizedShape.None;

        double aspect = width / height;
        if (aspect > 0.72 && aspect < 1.38 &&
            IsCircle(points, minX, minY, width, height))
            return RecognizedShape.Circle;

        return IsRectangle(points, minX, minY, width, height, diagonal)
            ? RecognizedShape.Rectangle : RecognizedShape.None;
    }

    private static bool IsCircle(IReadOnlyList<Point> points, double minX, double minY, double width, double height)
    {
        double cx = minX + width / 2, cy = minY + height / 2;
        var radii = points.Select(p => Math.Sqrt(Math.Pow(p.X - cx, 2) + Math.Pow(p.Y - cy, 2))).ToArray();
        double mean = radii.Average();
        if (mean < 8) return false;
        double variance = radii.Average(r => Math.Pow(r - mean, 2));
        return Math.Sqrt(variance) / mean < 0.23;
    }

    private static bool IsRectangle(IReadOnlyList<Point> points, double minX, double minY,
        double width, double height, double diagonal)
    {
        double tolerance = Math.Max(7, diagonal * 0.055);
        int hits = 0;
        foreach (Point p in points)
        {
            double d = Math.Min(Math.Min(Math.Abs(p.X - minX), Math.Abs(p.X - minX - width)),
                Math.Min(Math.Abs(p.Y - minY), Math.Abs(p.Y - minY - height)));
            if (d <= tolerance) hits++;
        }
        if (hits / (double)points.Count < 0.78) return false;
        return points.Any(p => Math.Abs(p.Y - minY) <= tolerance) &&
               points.Any(p => Math.Abs(p.Y - minY - height) <= tolerance) &&
               points.Any(p => Math.Abs(p.X - minX) <= tolerance) &&
               points.Any(p => Math.Abs(p.X - minX - width) <= tolerance);
    }
}

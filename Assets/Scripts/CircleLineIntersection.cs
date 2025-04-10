using System;
using UnityEngine;

public static class CircleLineIntersection
{
    /// <summary>
    /// Calculate intersection points between a circle and a line
    /// </summary>
    /// <param name="circleCenter">Position of Transform A</param>
    /// <param name="circleRadius">Radius of the circle</param>
    /// <param name="lineStart">Position of Transform B</param>
    /// <param name="lineDirection">Direction vector of Transform B</param>
    /// <param name="intersectionPoint">Position of Intersection</param>
    /// <returns>Array of intersection points (empty if none, single element if tangent, two elements if intersecting)</returns>
    public static bool CalculateIntersectionPoint(Vector3 circleCenter, float circleRadius, Vector3 lineStart, Vector3 lineDirection, out Vector3 intersectionPoint)
    {
        // Initialize out parameter
        intersectionPoint = Vector3.zero;
    
        // Normalize the line direction
        lineDirection.Normalize();

        // Calculate vector from line start to circle center
        Vector3 lineToCircle = circleCenter - lineStart;

        // Project circle center onto line
        float dotProduct = Vector3.Dot(lineToCircle, lineDirection);
        Vector3 projectionPoint = lineStart + lineDirection * dotProduct;

        // Calculate perpendicular distance squared more efficiently
        float distanceSquared = (projectionPoint - circleCenter).sqrMagnitude;

        // Check if line intersects circle
        if (distanceSquared > circleRadius * circleRadius)
            return false;

        // Calculate offset from projection point to intersection points
        float offset = Mathf.Sqrt(circleRadius * circleRadius - distanceSquared);

        // Calculate only the intersection point in positive direction
        intersectionPoint = projectionPoint + lineDirection * offset;
        return true;
    }
}
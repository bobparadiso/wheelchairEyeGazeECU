//-----------------------------------------------------------------------
// Copyright 2014 Tobii Technology AB. All rights reserved.
//-----------------------------------------------------------------------

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Represents the rectangular bounding box and z order of a game object as seen by a particular camera.
/// </summary>
public struct ProjectedRect
{
    /// <summary>
    /// Indicates whether the projection is valid: essentially, if the game object is within camera's clipping planes.
    /// </summary>
    public bool isValid;

    /// <summary>
    /// The bounding rectangle in GUI space.
    /// </summary>
    public Rect rect;

    /// <summary>
    /// The position of the object on the z axis, specified as the distance from the far clipping plane. 
    /// A higher value means that the object is closer to the camera.
    /// </summary>
    public double relativeZ;

    /// <summary>
    /// Gets the projection for a given bounds and camera.
    /// </summary>
    /// <param name="bounds">The axis-aligned bounds of the game object.</param>
    /// <param name="camera">The camera.</param>
    /// <param name="allowOverlap">Indicates if overlapping the screen bounds is allowed.</param>
    /// <returns>The projection.</returns>
    public static ProjectedRect GetProjectedRect(Bounds bounds, Camera camera, bool allowOverlap)
    {
        return GetProjectedRect(GetBoundingCornerPoints(bounds), camera, allowOverlap);
    }

    /// <summary>
    /// Gets the projection for a given box collider and camera.
    /// </summary>
    /// <param name="boxCollider">The box collider for the game object.</param>
    /// <param name="camera">The camera.</param>
    /// <param name="allowOverlap">Indicates if overlapping the screen bounds is allowed.</param>
    /// <returns>The projection.</returns>
    public static ProjectedRect GetProjectedRect(BoxCollider boxCollider, Camera camera, bool allowOverlap)
    {
        return GetProjectedRect(GetBoundingCornerPoints(boxCollider), camera, allowOverlap);
    }

    /// <summary>
    /// Gets the projection for a given set of corner points and camera.
    /// </summary>
    /// <param name="boundingCornerPoints">The bounding corner points of the game object (in World space).</param>
    /// <param name="camera">The camera.</param>
    /// <param name="allowOverlap">Indicates if overlapping the screen bounds is allowed.</param>
    /// <returns>The projection.</returns>
    public static ProjectedRect GetProjectedRect(IEnumerable<Vector3> boundingCornerPoints, Camera camera, bool allowOverlap)
    {
        float xMin = 0, xMax = 0, yMin = 0, yMax = 0, zMin = 0, zMax = 0;

        bool first = true;
        foreach (var point in boundingCornerPoints)
        {
            var projectedPoint = camera.WorldToScreenPoint(point);

            // convert to GUI space
            projectedPoint.y = Screen.height - projectedPoint.y;

            if (first)
            {
                xMin = xMax = projectedPoint.x;
                yMin = yMax = projectedPoint.y;
                zMin = zMax = projectedPoint.z;
            }
            else
            {
                if (projectedPoint.x < xMin)
                {
                    xMin = projectedPoint.x;
                }
                else if (projectedPoint.x > xMax)
                {
                    xMax = projectedPoint.x;
                }

                if (projectedPoint.y < yMin)
                {
                    yMin = projectedPoint.y;
                }
                else if (projectedPoint.y > yMax)
                {
                    yMax = projectedPoint.y;
                }

                if (projectedPoint.z < zMin)
                {
                    zMin = projectedPoint.z;
                }
                else if (projectedPoint.z > zMax)
                {
                    zMax = projectedPoint.z;
                }
            }

            first = false;
        }

        var potentialRect = new Rect(xMin, yMin, xMax - xMin, yMax - yMin);
        var screenRect = new Rect(0, 0, Screen.width, Screen.height);

        // Return invalid rect if projection is outside or covering more than the whole screen
        var overlaps = potentialRect.Overlaps(screenRect);
        var biggerThanScreen = !allowOverlap && potentialRect.width > Screen.width && potentialRect.height > Screen.height;
        if (!overlaps || biggerThanScreen)
        {
            return new ProjectedRect { isValid = false };
        }

        // Remaining projection rects are at least partially within the screen bounds.
        // Clip projection rects at screen bounds (so no interactors extend outside 
        // the game window).
        xMin = xMin < 0 ? 0 : xMin;
        xMax = xMax > Screen.width ? Screen.width : xMax;
        yMin = yMin < 0 ? 0 : yMin;
        yMax = yMax > Screen.height ? Screen.height : yMax;

        if (camera.nearClipPlane <= zMax && zMax <= camera.farClipPlane)
        {
            // Use mean z value as an approximation
            var zMean = (zMin + zMax) / 2.0f;
            return new ProjectedRect
            {
                isValid = true,
                rect = new Rect(xMin, yMin, xMax - xMin, yMax - yMin),
                relativeZ = camera.farClipPlane - zMean
            };
        }
        else
        {
            return new ProjectedRect { isValid = false };
        }
    }

    private static IEnumerable<Vector3> GetBoundingCornerPoints(Bounds boundingBox)
    {
        var center = boundingBox.center;
        var extents = boundingBox.extents;

        foreach (var cornerFactor in GetCornerFactors())
        {
            var offset = new Vector3(cornerFactor.x * extents.x, cornerFactor.y * extents.y, cornerFactor.z * extents.z);
            yield return center + offset;
        }
    }

    private static IEnumerable<Vector3> GetBoundingCornerPoints(BoxCollider boxCollider)
    {
        var center = boxCollider.center;
        var size = boxCollider.size;

        foreach (var cornerFactor in GetCornerFactors())
        {
            var offset = new Vector3(cornerFactor.x * size.x, cornerFactor.y * size.y, cornerFactor.z * size.z) / 2.0f;
            var cornerPointInWorldSpace = boxCollider.gameObject.transform.TransformPoint(center + offset);
            yield return cornerPointInWorldSpace;
        }
    }

    private static IEnumerable<Vector3> GetCornerFactors()
    {
        // Some bit fiddling to iterate over the corners...
        for (int corner = 0; corner < 8; corner++)
        {
            int xFactor = (corner & 1) == 0 ? +1 : -1;
            int yFactor = (corner & 2) == 0 ? +1 : -1;
            int zFactor = (corner & 4) == 0 ? +1 : -1;

            yield return new Vector3(xFactor, yFactor, zFactor);
        }
    }
}

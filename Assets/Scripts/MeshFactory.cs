using System.Collections.Generic;
using UnityEngine;

public static class MeshFactory
{
    /// <summary>
    /// Builds a flat quad (XZ plane) of length `range` (Z) and width `width` (X),
    /// centered on the local origin. If flipWinding=true, its normals face down.
    /// </summary>
    public static Mesh BuildRectangle(float range, float width, bool flipWinding = false)
    {
        float hr = range * 0.5f;
        float hw = width * 0.5f;

        Vector3[] verts = {
            new(-hw, 0f, -hr), // 0 back-left
            new( hw, 0f, -hr), // 1 back-right
            new( hw, 0f,  hr), // 2 front-right
            new(-hw, 0f,  hr), // 3 front-left
        };

        int[] tris = flipWinding
            // face down: reverse winding
            ? new[]{ 0, 2, 1,  2, 0, 3 }
            // face up: normal winding
            : new[]{ 0, 1, 2,  2, 3, 0 };

        Vector2[] uvs = {
            new(0,0), new(1,0), new(1,1), new(0,1)
        };

        var m = new Mesh();
        m.name      = "Procedural_Rectangle";
        m.vertices  = verts;
        m.triangles = tris;
        m.uv        = uvs;
        m.RecalculateNormals();
        return m;
    }
}

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
            ? new[] { 0, 2, 1, 2, 0, 3 }
            : new[] { 0, 1, 2, 2, 3, 0 };

        Vector2[] uvs = {
            new(0, 0), new(1, 0), new(1, 1), new(0, 1)
        };

        var m = new Mesh();
        m.name = "Procedural_Rectangle";
        m.vertices = verts;
        m.triangles = tris;
        m.uv = uvs;
        m.RecalculateNormals();
        return m;
    }

    public static Mesh BuildCircle(float radius, int segments)
    {
        Mesh mesh = new Mesh();
        Vector3[] vertices = new Vector3[segments + 1];
        Vector2[] uvs = new Vector2[segments + 1];
        int[] triangles = new int[segments * 3];

        vertices[0] = Vector3.zero;
        uvs[0] = new Vector2(0.5f, 0.5f);

        for (int i = 0; i < segments; i++)
        {
            float angle = 2 * Mathf.PI * i / segments;
            float x = Mathf.Cos(angle);
            float z = Mathf.Sin(angle);
            vertices[i + 1] = new Vector3(x, 0, z) * radius;
            uvs[i + 1] = new Vector2(x * 0.5f + 0.5f, z * 0.5f + 0.5f);
        }

        for (int i = 0; i < segments; i++)
        {
            int start = i + 1;
            int end = (i == segments - 1) ? 1 : i + 2;
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = end;
            triangles[i * 3 + 2] = start;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals();
        return mesh;
    }

    public static Mesh BuildCone(float radius, float angleDegrees, int segments = 32)
    {
        return BuildSector(radius, angleDegrees, segments);
    }

    public static Mesh BuildSector(float radius, float angleDegrees, int segments)
    {
        Mesh mesh = new Mesh();
        float angleRad = Mathf.Deg2Rad * angleDegrees;

        Vector3[] vertices = new Vector3[segments + 2];
        Vector2[] uvs = new Vector2[segments + 2];
        int[] triangles = new int[segments * 3];

        vertices[0] = Vector3.zero;
        uvs[0] = new Vector2(0.5f, 0.5f);

        for (int i = 0; i <= segments; i++)
        {
            float angle = -angleRad / 2 + angleRad * i / segments;
            float x = Mathf.Sin(angle);
            float z = Mathf.Cos(angle);
            vertices[i + 1] = new Vector3(x, 0, z) * radius;
            uvs[i + 1] = new Vector2(x * 0.5f + 0.5f, z * 0.5f + 0.5f);
        }

        for (int i = 0; i < segments; i++)
        {
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = i + 1;
            triangles[i * 3 + 2] = i + 2;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals();
        return mesh;
    }
}

using System.Collections.Generic;
using UnityEngine;

public static class MeshFactory
{
    /// <summary>
    /// Builds a flat quad (XZ plane) of length `range` (Z) and width `width` (X),
    /// centered on the local origin.
    /// </summary>

    public static Mesh BuildRectangle(float length, float width, float maxSegmentSize = 0.3f)
    {
        int xSegments = Mathf.Max(1, Mathf.CeilToInt(width / maxSegmentSize));
        int zSegments = Mathf.Max(1, Mathf.CeilToInt(length / maxSegmentSize));

        float halfWidth = width * 0.5f;
        float halfLength = length * 0.5f;

        Vector3[] vertices = new Vector3[(xSegments + 1) * (zSegments + 1)];
        Vector2[] uvs = new Vector2[vertices.Length];
        int[] triangles = new int[xSegments * zSegments * 6];

        for (int z = 0; z <= zSegments; z++)
        {
            for (int x = 0; x <= xSegments; x++)
            {
                int i = z * (xSegments + 1) + x;
                float xPos = Mathf.Lerp(-halfWidth, halfWidth, (float)x / xSegments);
                float zPos = Mathf.Lerp(-halfLength, halfLength, (float)z / zSegments);
                vertices[i] = new Vector3(xPos, 0f, zPos);
                uvs[i] = new Vector2((float)x / xSegments, (float)z / zSegments);
            }
        }

        int tri = 0;
        for (int z = 0; z < zSegments; z++)
        {
            for (int x = 0; x < xSegments; x++)
            {
                int i = z * (xSegments + 1) + x;

                triangles[tri++] = i;
                triangles[tri++] = i + xSegments + 1;
                triangles[tri++] = i + 1;

                triangles[tri++] = i + 1;
                triangles[tri++] = i + xSegments + 1;
                triangles[tri++] = i + xSegments + 2;
            }
        }

        Mesh m = new Mesh();
        m.name = $"GridRect_{width:F2}x{length:F2}";
        m.vertices = vertices;
        m.triangles = triangles;
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

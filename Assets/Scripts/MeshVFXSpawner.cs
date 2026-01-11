using UnityEngine;

public static class MeshVFXSpawner
{
    public static GameObject Spawn(
        Mesh mesh,
        Material mat,
        Vector3 position,
        Quaternion rotation,
        float duration,
        MaterialPropertyBlock materialPropertyBlock = null,
        Transform parent = null)
    {
        var go = new GameObject("MeshVFX");
        if (parent != null)
        {
            // make it a child, and treat position/rotation as local
            go.transform.SetParent(parent, worldPositionStays: false);
            go.transform.localPosition = position;
            go.transform.localRotation = rotation;
        }
        else
        {
            go.transform.position = position;
            go.transform.rotation = rotation;
        }

        var mf = go.AddComponent<MeshFilter>();
        mf.mesh = mesh;
        var mr = go.AddComponent<MeshRenderer>();
        mr.material = mat;

        mr.SetPropertyBlock(materialPropertyBlock);

        Object.Destroy(go, duration);

        return go;
    }
}


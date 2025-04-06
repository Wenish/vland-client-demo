using UnityEngine;

[CreateAssetMenu(fileName = "NewModel", menuName = "Game/Model/Model")]
public class ModelData : ScriptableObject
{
    public string modelName;
    public GameObject prefab;
}
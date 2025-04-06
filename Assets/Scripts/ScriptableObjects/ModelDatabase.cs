using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewModelDatabase", menuName = "Game/Model/Database")]
public class ModelDatabase : ScriptableObject
{
    public List<ModelData> allModels = new List<ModelData>();

    public ModelData GetModelByName(string name)
    {
        return allModels.Find(model => model.modelName == name);
    }
}
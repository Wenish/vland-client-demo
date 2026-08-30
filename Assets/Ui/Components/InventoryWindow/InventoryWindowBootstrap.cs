using UnityEngine;
using VContainer.Unity;
using ShadowInfection.DI;

namespace ShadowInfection.UI.InventoryWindow
{
    public sealed class InventoryWindowBootstrap : IStartable
    {
        private const string PrefabResourcePath = "UI/UiDocumentInventoryWindow";

        public void Start()
        {
            var scope = GameLifetimeScope.FindOrCreate();
            if (scope == null)
                return;

            if (scope.GetComponentInChildren<InventoryWindowController>(true) != null)
                return;

            var prefab = Resources.Load<GameObject>(PrefabResourcePath);
            if (prefab == null)
            {
                UnityEngine.Debug.LogError($"Missing inventory window prefab at Resources/{PrefabResourcePath}.");
                return;
            }

            Object.Instantiate(prefab, scope.transform);
        }
    }
}

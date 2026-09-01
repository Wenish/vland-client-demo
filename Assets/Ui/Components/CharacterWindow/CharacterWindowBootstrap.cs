using UnityEngine;
using VContainer.Unity;
using ShadowInfection.DI;

namespace ShadowInfection.UI.CharacterWindow
{
    public sealed class CharacterWindowBootstrap : IStartable
    {
        private const string PrefabResourcePath = "UI/UiDocumentCharacterWindow";

        public void Start()
        {
            var scope = GameLifetimeScope.FindOrCreate();
            if (scope == null)
                return;

            if (scope.GetComponentInChildren<CharacterWindowController>(true) != null)
                return;

            var prefab = Resources.Load<GameObject>(PrefabResourcePath);
            if (prefab == null)
            {
                UnityEngine.Debug.LogError($"Missing character window prefab at Resources/{PrefabResourcePath}.");
                return;
            }

            Object.Instantiate(prefab, scope.transform);
        }
    }
}

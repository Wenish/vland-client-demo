using System.Collections.Generic;
using UnityEngine.UIElements;

namespace ShadowInfection.UI.Nameplates
{
    internal sealed class NameplateLayerView
    {
        private readonly VisualElement container;
        private readonly Stack<UnitNameplateElement> pool = new();

        public NameplateLayerView(VisualElement root)
        {
            container = root.Q<VisualElement>("nameplate-container")
                ?? root.Q<VisualElement>("nameplate-layer-root")
                ?? root;
        }

        public UnitNameplateElement Acquire()
        {
            if (pool.Count > 0)
                return pool.Pop();

            var element = new UnitNameplateElement();
            container.Add(element);
            return element;
        }

        public void Release(UnitNameplateElement element)
        {
            if (element == null)
                return;

            element.HideAndReset();
            pool.Push(element);
        }

        public VisualElement Container => container;
    }
}

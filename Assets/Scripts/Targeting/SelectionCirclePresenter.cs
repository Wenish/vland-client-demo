using MessagePipe;
using MyGame.Events;
using UnityEngine;
using VContainer.Unity;

namespace ShadowInfection.Targeting
{
    public sealed class SelectionCirclePresenter : IStartable, ITickable, System.IDisposable
    {
        private static Shader runtimeShader;

        private readonly IPlayerTarget playerTarget;
        private readonly SelectionCircleSettings settings;
        private readonly ISubscriber<MyPlayerUnitSpawnedEvent> myUnitSpawned;
        private readonly ISubscriber<PlayerTargetChangedEvent> targetChanged;

        private GameObject root;
        private Transform follow;
        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private Material runtimeMaterial;
        private Mesh runtimeMesh;
        private bool ownsRuntimeMesh;
        private UnitController localPlayer;
        private UnitController current;
        private System.IDisposable targetChangedSub;
        private System.IDisposable myUnitSpawnedSub;

        public SelectionCirclePresenter(
            IPlayerTarget playerTarget,
            SelectionCircleSettings settings,
            ISubscriber<MyPlayerUnitSpawnedEvent> myUnitSpawned,
            ISubscriber<PlayerTargetChangedEvent> targetChanged)
        {
            this.playerTarget = playerTarget;
            this.settings = settings;
            this.myUnitSpawned = myUnitSpawned;
            this.targetChanged = targetChanged;
        }

        public void Start()
        {
            myUnitSpawnedSub = myUnitSpawned.Subscribe(OnMyPlayerUnitSpawned);
            targetChangedSub = targetChanged.Subscribe(OnTargetChanged);
            Apply(playerTarget != null ? playerTarget.Current : null);
        }

        public void Tick()
        {
            if (root == null || !root.activeSelf || current == null)
                return;

            if (current.Equals(null))
            {
                Hide();
                return;
            }

            var position = current.transform.position;
            position.y += settings != null ? settings.HeightOffset : 0.05f;
            root.transform.position = position;
            root.transform.rotation = Quaternion.identity;
        }

        public void Dispose()
        {
            targetChangedSub?.Dispose();
            myUnitSpawnedSub?.Dispose();
            targetChangedSub = null;
            myUnitSpawnedSub = null;
            DestroyVisual();
            current = null;
            localPlayer = null;
        }

        private void OnMyPlayerUnitSpawned(MyPlayerUnitSpawnedEvent evt)
        {
            localPlayer = evt.PlayerCharacter;
            if (current != null)
                ApplyColor();
        }

        private void OnTargetChanged(PlayerTargetChangedEvent evt)
        {
            Apply(evt.Current);
        }

        private void Apply(UnitController unit)
        {
            current = unit != null && !unit.Equals(null) ? unit : null;
            if (current == null)
            {
                Hide();
                return;
            }

            EnsureVisual();
            if (root == null)
                return;

            root.SetActive(true);
            follow = current.transform;
            ApplyScale();
            ApplyColor();

            var position = follow.position;
            position.y += settings != null ? settings.HeightOffset : 0.05f;
            root.transform.position = position;
        }

        private void Hide()
        {
            current = null;
            follow = null;
            if (root != null)
                root.SetActive(false);
        }

        private void EnsureVisual()
        {
            if (root != null)
                return;

            if (settings != null && settings.Prefab != null)
            {
                root = Object.Instantiate(settings.Prefab);
                root.name = "SelectionCircle";
                DisableColliders(root);
                meshRenderer = root.GetComponentInChildren<MeshRenderer>();
                if (meshRenderer != null)
                    ApplyMaterialOverride(meshRenderer, cloneExisting: settings.Material == null);
                return;
            }

            root = new GameObject("SelectionCircle");
            meshFilter = root.AddComponent<MeshFilter>();
            runtimeMesh = MeshFactory.BuildCircle(1f, 48);
            ownsRuntimeMesh = true;
            meshFilter.sharedMesh = runtimeMesh;
            meshRenderer = root.AddComponent<MeshRenderer>();
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
            ApplyMaterialOverride(meshRenderer, cloneExisting: false);
        }

        private void ApplyMaterialOverride(MeshRenderer renderer, bool cloneExisting)
        {
            if (renderer == null)
                return;

            if (runtimeMaterial == null)
            {
                if (settings != null && settings.Material != null)
                {
                    runtimeMaterial = new Material(settings.Material);
                }
                else if (cloneExisting && renderer.sharedMaterial != null)
                {
                    runtimeMaterial = new Material(renderer.sharedMaterial);
                }
                else
                {
                    EnsureRuntimeShader();
                    runtimeMaterial = new Material(runtimeShader != null
                        ? runtimeShader
                        : Shader.Find("Hidden/InternalErrorShader"));
                    runtimeMaterial.name = "SelectionCircleRuntime";
                }
            }

            var texture = settings != null ? settings.Texture : null;
            if (texture != null)
            {
                if (runtimeMaterial.HasProperty("_MainTex"))
                    runtimeMaterial.mainTexture = texture;
                if (runtimeMaterial.HasProperty("_BaseMap"))
                    runtimeMaterial.SetTexture("_BaseMap", texture);
            }

            renderer.sharedMaterial = runtimeMaterial;
        }

        private void ApplyScale()
        {
            if (root == null)
                return;

            var size = settings != null ? Mathf.Max(0.05f, settings.Radius) : 0.8f;
            if (settings != null && settings.ScaleWithCollider && current != null)
            {
                var col = current.GetComponent<Collider>();
                if (col != null)
                {
                    var extents = col.bounds.extents;
                    var colliderRadius = Mathf.Max(extents.x, extents.z) * settings.ColliderRadiusMultiplier;
                    size = Mathf.Max(size, colliderRadius);
                }
            }

            root.transform.localScale = new Vector3(size, 1f, size);
        }

        private void ApplyColor()
        {
            if (runtimeMaterial == null && meshRenderer != null)
                runtimeMaterial = meshRenderer.material;

            var color = ResolveColor();
            var opacity = settings != null ? settings.Opacity : 0.75f;
            color.a = opacity;

            if (runtimeMaterial != null)
            {
                if (runtimeMaterial.HasProperty("_BaseColor"))
                    runtimeMaterial.SetColor("_BaseColor", color);
                if (runtimeMaterial.HasProperty("_Color"))
                    runtimeMaterial.SetColor("_Color", color);
                runtimeMaterial.color = color;
            }

            if (meshRenderer != null && runtimeMaterial != null)
                meshRenderer.sharedMaterial = runtimeMaterial;
        }

        private Color ResolveColor()
        {
            if (settings == null || current == null)
                return Color.white;

            if (localPlayer != null && current == localPlayer)
                return settings.SelfColor;

            if (localPlayer != null && current.team == localPlayer.team)
                return settings.AllyColor;

            return settings.EnemyColor;
        }

        private void DestroyVisual()
        {
            if (runtimeMaterial != null)
            {
                Object.Destroy(runtimeMaterial);
                runtimeMaterial = null;
            }

            if (ownsRuntimeMesh && runtimeMesh != null)
            {
                Object.Destroy(runtimeMesh);
                runtimeMesh = null;
                ownsRuntimeMesh = false;
            }

            if (root != null)
            {
                Object.Destroy(root);
                root = null;
            }

            meshFilter = null;
            meshRenderer = null;
        }

        private static void DisableColliders(GameObject go)
        {
            if (go == null)
                return;

            var colliders = go.GetComponentsInChildren<Collider>(true);
            for (var i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != null)
                    colliders[i].enabled = false;
            }
        }

        private static void EnsureRuntimeShader()
        {
            if (runtimeShader != null)
                return;

            runtimeShader = Shader.Find("Sprites/Default");
            if (runtimeShader == null)
                runtimeShader = Shader.Find("Unlit/Transparent");
            if (runtimeShader == null)
                runtimeShader = Shader.Find("Universal Render Pipeline/Unlit");
        }
    }
}

using UnityEngine;

namespace ShadowInfection.Skills.Indicators
{
    /// <summary>
    /// Runtime visuals for one indicator session (preview or server-driven).
    /// </summary>
    public sealed class SkillIndicatorSessionView : System.IDisposable
    {
        private static Texture2D _defaultRangeTexture;
        private static Texture2D _defaultPlacementTexture;
        private static Shader _runtimeShader;

        private readonly UnitController _caster;
        private readonly Transform _root;
        private readonly Transform _rangeRing;
        private readonly Transform _circle;
        private readonly Transform _directional;
        private readonly Material _rangeRingMaterialInstance;
        private readonly Material _placementMaterialInstance;

        public SkillIndicatorDisplayParams Display { get; private set; }
        private Vector3 _aimPoint;

        private const string DefaultRangeTextureResource =
            "SkillIndicators/rangeskillindicator";
        private const string DefaultPlacementTextureResource =
            "SkillIndicators/aoeskillindicator_nobackground";

        public static SkillIndicatorSessionView Create(
            UnitController caster,
            SkillIndicatorDisplayParams display,
            Vector3 aimPoint,
            SkillIndicatorData visualSource = null)
        {
            return new SkillIndicatorSessionView(caster, display, aimPoint, visualSource);
        }

        private SkillIndicatorSessionView(
            UnitController caster,
            SkillIndicatorDisplayParams display,
            Vector3 aimPoint,
            SkillIndicatorData visualSource)
        {
            _caster = caster;
            Display = display;
            _aimPoint = aimPoint;

            visualSource ??= SkillIndicatorVisualCatalog.Get(display.indicatorAssetName);
            if (visualSource != null)
                SkillIndicatorVisualCatalog.Register(visualSource);

            var rootGo = new GameObject("SkillIndicatorSession");
            _root = rootGo.transform;

            Texture2D rangeTex = visualSource != null && visualSource.rangeRingTexture != null
                ? visualSource.rangeRingTexture
                : GetDefaultRangeTexture();
            Texture2D placementTex = visualSource != null && visualSource.placementTexture != null
                ? visualSource.placementTexture
                : GetDefaultPlacementTexture();

            _rangeRingMaterialInstance = CreateMaterialInstance(
                visualSource != null ? visualSource.rangeRingMaterial : null,
                rangeTex);
            _placementMaterialInstance = CreateMaterialInstance(
                visualSource != null ? visualSource.placementMaterial : null,
                placementTex);

            _rangeRing = CreateMeshChild(
                "RangeRing",
                MeshFactory.BuildCircle(1f, 64),
                display.showRangeRing && display.castRange > 0f,
                _rangeRingMaterialInstance);

            _circle = CreateMeshChild(
                "Circle",
                MeshFactory.BuildCircle(1f, 64),
                display.shape == SkillIndicatorData.IndicatorShape.Circle,
                _placementMaterialInstance);

            _directional = CreateMeshChild(
                "Directional",
                MeshFactory.BuildRectangle(1f, 1f),
                display.shape == SkillIndicatorData.IndicatorShape.Directional,
                _placementMaterialInstance);

            Tick();
        }

        public void SetAimPoint(Vector3 aimPoint)
        {
            _aimPoint = aimPoint;

            // Live follow clamps to current cast range. Locked aim keeps the confirmed world point.
            if (Display.aimFollowMode == SkillIndicatorData.AimFollowMode.FollowWhileActive
                && Display.castRange > 0f
                && _caster != null)
            {
                _aimPoint = SkillAimUtil.ClampAimPoint(
                    _caster.transform.position,
                    _aimPoint,
                    Display.castRange);
            }
        }

        public void Tick()
        {
            if (_root == null || _caster == null)
                return;

            Vector3 casterPos = _caster.transform.position;
            casterPos.y += 0.05f;

            Vector3 aim = _aimPoint;
            aim.y = casterPos.y;

            if (Display.aimFollowMode == SkillIndicatorData.AimFollowMode.FollowWhileActive
                && Display.castRange > 0f)
            {
                aim = SkillAimUtil.ClampAimPoint(casterPos, aim, Display.castRange);
            }

            if (_rangeRing != null && _rangeRing.gameObject.activeSelf)
            {
                _rangeRing.position = casterPos;
                _rangeRing.rotation = Quaternion.identity;
                float radius = Mathf.Max(0.01f, Display.castRange);
                _rangeRing.localScale = new Vector3(radius, 1f, radius);
            }

            if (_circle != null && _circle.gameObject.activeSelf)
            {
                Vector3 circlePos = Display.placement == SkillIndicatorData.IndicatorPlacement.Self
                    ? casterPos
                    : aim;
                _circle.position = circlePos;
                _circle.rotation = Quaternion.identity;
                float radius = Mathf.Max(0.05f, Display.effectRadius);
                _circle.localScale = new Vector3(radius, 1f, radius);
            }

            if (_directional != null && _directional.gameObject.activeSelf)
            {
                Vector3 flatAim = aim;
                flatAim.y = casterPos.y;
                Vector3 dir = flatAim - casterPos;
                dir.y = 0f;
                if (dir.sqrMagnitude < 0.0001f)
                    dir = _caster.transform.forward;

                dir.Normalize();
                float length = Mathf.Max(0.01f, Display.effectRange > 0f ? Display.effectRange : 1f);
                float width = Mathf.Max(0.01f, Display.effectWidth > 0f ? Display.effectWidth : 1f);
                _directional.position = casterPos + dir * (length * 0.5f);
                _directional.rotation = Quaternion.LookRotation(dir, Vector3.up);
                _directional.localScale = new Vector3(width, 1f, length);
            }
        }

        public void Dispose()
        {
            if (_root != null)
                Object.Destroy(_root.gameObject);

            if (_rangeRingMaterialInstance != null)
                Object.Destroy(_rangeRingMaterialInstance);
            if (_placementMaterialInstance != null)
                Object.Destroy(_placementMaterialInstance);
        }

        private Transform CreateMeshChild(string name, Mesh mesh, bool active, Material material)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_root, false);
            go.SetActive(active);

            var filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;

            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            return go.transform;
        }

        private static Material CreateMaterialInstance(Material source, Texture2D texture)
        {
            Material mat;
            if (source != null)
            {
                mat = new Material(source);
            }
            else
            {
                EnsureRuntimeShader();
                mat = new Material(_runtimeShader != null
                    ? _runtimeShader
                    : Shader.Find("Hidden/InternalErrorShader"));
                mat.name = "SkillIndicatorRuntime";
                mat.color = new Color(0.2f, 0.75f, 1f, 0.35f);
            }

            if (texture != null)
            {
                if (mat.HasProperty("_MainTex"))
                    mat.mainTexture = texture;
                if (mat.HasProperty("_BaseMap"))
                    mat.SetTexture("_BaseMap", texture);
            }

            return mat;
        }

        private static Texture2D GetDefaultRangeTexture()
        {
            if (_defaultRangeTexture == null)
                _defaultRangeTexture = Resources.Load<Texture2D>(DefaultRangeTextureResource);

            // Fallback if the dedicated range texture is missing.
            return _defaultRangeTexture != null ? _defaultRangeTexture : GetDefaultPlacementTexture();
        }

        private static Texture2D GetDefaultPlacementTexture()
        {
            if (_defaultPlacementTexture == null)
                _defaultPlacementTexture = Resources.Load<Texture2D>(DefaultPlacementTextureResource);

            return _defaultPlacementTexture;
        }

        private static void EnsureRuntimeShader()
        {
            if (_runtimeShader != null)
                return;

            _runtimeShader = Shader.Find("Sprites/Default");
            if (_runtimeShader == null)
                _runtimeShader = Shader.Find("Unlit/Transparent");
            if (_runtimeShader == null)
                _runtimeShader = Shader.Find("Universal Render Pipeline/Unlit");
        }
    }
}

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
        private static Texture2D _defaultConeTexture;
        private static Shader _runtimeShader;

        private readonly UnitController _caster;
        private readonly NetworkedSkillInstance _skillInstance;
        private readonly SkillIndicatorData _visualSource;
        private readonly SkillEffectTarget _snapToTarget;
        private readonly bool _isPreviewSession;
        private readonly Transform _root;
        private readonly Transform _rangeRing;
        private readonly Transform _circle;
        private readonly Transform _directional;
        private readonly Transform _cone;
        private readonly MeshFilter _coneFilter;
        private readonly Material _rangeRingMaterialInstance;
        private readonly Material _placementMaterialInstance;
        private float _builtConeAngle = -1f;

        public SkillIndicatorDisplayParams Display { get; private set; }
        private Vector3 _aimPoint;
        private UnitController _followTarget;
        private Vector2 _moveInput;
        private bool _hasMoveInput;
        private Vector3? _lockedDirection;

        /// <summary>
        /// After Shift+confirm, freeze follow and apply cast lock without rebuilding meshes
        /// (avoids preview→cast flicker).
        /// </summary>
        public void ApplyCastConfirm(
            SkillIndicatorDisplayParams display,
            Vector3 aimPoint,
            UnitController followTarget = null)
        {
            Display = display;
            _followTarget = followTarget;
            _aimPoint = aimPoint;
            _lockedDirection = null;

            if (display.aimFollowMode == SkillIndicatorData.AimFollowMode.LockOnConfirm
                && (display.shape == SkillIndicatorData.IndicatorShape.Directional
                    || display.shape == SkillIndicatorData.IndicatorShape.Cone)
                && _caster != null)
            {
                _lockedDirection = ResolveActiveDirection(_aimPoint);
            }

            SetVisible(true);
            Tick();
        }

        private const string DefaultRangeTextureResource =
            "SkillIndicators/rangeskillindicator";
        private const string DefaultPlacementTextureResource =
            "SkillIndicators/aoeskillindicator_nobackground";
        private const string DefaultConeTextureResource =
            "SkillIndicators/coneskillindicator";

        public static SkillIndicatorSessionView Create(
            UnitController caster,
            SkillIndicatorDisplayParams display,
            Vector3 aimPoint,
            SkillIndicatorData visualSource = null,
            UnitController followTarget = null,
            NetworkedSkillInstance skillInstance = null,
            bool isPreviewSession = false)
        {
            return new SkillIndicatorSessionView(
                caster,
                display,
                aimPoint,
                visualSource,
                followTarget,
                skillInstance,
                isPreviewSession);
        }

        private SkillIndicatorSessionView(
            UnitController caster,
            SkillIndicatorDisplayParams display,
            Vector3 aimPoint,
            SkillIndicatorData visualSource,
            UnitController followTarget,
            NetworkedSkillInstance skillInstance,
            bool isPreviewSession)
        {
            _caster = caster;
            Display = display;
            _aimPoint = aimPoint;
            _skillInstance = skillInstance;
            _isPreviewSession = isPreviewSession;

            visualSource ??= SkillIndicatorVisualCatalog.Get(display.indicatorAssetName);
            if (visualSource != null)
                SkillIndicatorVisualCatalog.Register(visualSource);

            _visualSource = visualSource;
            _snapToTarget = visualSource != null ? visualSource.snapToTarget : null;
            _followTarget = followTarget;

            if (_followTarget == null && _snapToTarget != null)
            {
                _followTarget = ResolveSnapTarget(_aimPoint);
            }

            var rootGo = new GameObject("SkillIndicatorSession");
            _root = rootGo.transform;

            Texture2D rangeTex = visualSource != null && visualSource.rangeRingTexture != null
                ? visualSource.rangeRingTexture
                : GetDefaultRangeTexture();
            Texture2D placementTex = visualSource != null && visualSource.placementTexture != null
                ? visualSource.placementTexture
                : GetDefaultPlacementTexture(display.shape);

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

            float coneAngle = Mathf.Clamp(display.effectAngle > 0f ? display.effectAngle : 90f, 1f, 360f);
            _cone = CreateMeshChild(
                "Cone",
                MeshFactory.BuildCone(1f, coneAngle),
                display.shape == SkillIndicatorData.IndicatorShape.Cone,
                _placementMaterialInstance);
            _coneFilter = _cone != null ? _cone.GetComponent<MeshFilter>() : null;
            _builtConeAngle = coneAngle;

            if (display.aimFollowMode == SkillIndicatorData.AimFollowMode.LockOnConfirm
                && (display.shape == SkillIndicatorData.IndicatorShape.Directional
                    || display.shape == SkillIndicatorData.IndicatorShape.Cone)
                && _caster != null)
            {
                _lockedDirection = ResolveActiveDirection(_aimPoint);
            }

            Tick();
        }

        public void SetAimPoint(Vector3 aimPoint)
        {
            _aimPoint = aimPoint;

            if (Display.aimFollowMode == SkillIndicatorData.AimFollowMode.FollowWhileActive
                && Display.castRange > 0f
                && _caster != null
                && _snapToTarget == null)
            {
                _aimPoint = SkillAimUtil.ClampAimPoint(
                    _caster.transform.position,
                    _aimPoint,
                    Display.castRange);
            }

            if (Display.aimFollowMode == SkillIndicatorData.AimFollowMode.FollowWhileActive
                && _snapToTarget != null
                && _visualSource != null)
            {
                _followTarget = ResolveSnapTarget(_aimPoint);
            }
        }

        private UnitController ResolveSnapTarget(Vector3 aimPoint)
        {
            bool forceSelfTarget = SkillTargetingInput.IsLeftAltPressedForSelfTarget();
            var preferred = ShadowInfection.Targeting.PlayerTargetLookup.CurrentOrNull();
            if (_isPreviewSession)
            {
                return SkillIndicatorTargetSnap.ResolvePreview(
                    _visualSource,
                    _caster,
                    _skillInstance,
                    aimPoint,
                    forceSelfTarget,
                    preferred);
            }

            return SkillIndicatorTargetSnap.Resolve(
                _visualSource,
                _caster,
                _skillInstance,
                aimPoint,
                forceSelfTarget,
                preferred);
        }

        public void SetFollowTarget(UnitController target)
        {
            _followTarget = target;
        }

        public void SetMoveInput(Vector2 moveInput)
        {
            _moveInput = moveInput;
            _hasMoveInput = true;
        }

        public void SetVisible(bool visible)
        {
            if (_root != null)
                _root.gameObject.SetActive(visible);
        }

        public void Tick()
        {
            if (_root == null || _caster == null || !_root.gameObject.activeSelf)
                return;

            // Drop destroyed units. Drop dead units only when the snap target filter
            // does not allow Dead (Resurrection keeps corpses as valid snap targets).
            if (_followTarget != null && _followTarget.Equals(null))
            {
                _followTarget = null;
            }
            else if (_followTarget != null
                && _followTarget.IsDead
                && (_snapToTarget == null
                    || (_snapToTarget.lifeMask & SkillEffectTarget.LifeMask.Dead) == 0))
            {
                _followTarget = null;
            }

            Vector3 casterPos = _caster.transform.position;
            casterPos.y += 0.05f;

            Vector3 aim = _aimPoint;
            aim.y = casterPos.y;

            if (Display.aimFollowMode == SkillIndicatorData.AimFollowMode.FollowWhileActive
                && Display.castRange > 0f
                && _snapToTarget == null)
            {
                aim = SkillAimUtil.ClampAimPoint(casterPos, aim, Display.castRange);
            }

            Vector3 placementPos = SkillAimUtil.ResolveCirclePlacement(
                _caster,
                aim,
                Display.placement,
                _followTarget);

            if (_rangeRing != null && _rangeRing.gameObject.activeSelf)
            {
                _rangeRing.position = casterPos;
                _rangeRing.rotation = Quaternion.identity;
                float radius = Mathf.Max(0.01f, Display.castRange);
                _rangeRing.localScale = new Vector3(radius, 1f, radius);
            }

            if (_circle != null && _circle.gameObject.activeSelf)
            {
                _circle.position = placementPos;
                _circle.rotation = Quaternion.identity;
                float radius = Mathf.Max(0.05f, Display.effectRadius > 0f ? Display.effectRadius : 0.75f);
                _circle.localScale = new Vector3(radius, 1f, radius);
            }

            Vector3 directionalOrigin = SkillAimUtil.ResolveDirectionalOrigin(_caster);

            if (_directional != null && _directional.gameObject.activeSelf)
            {
                Vector3 dir = ResolveActiveDirection(aim);
                float length = Mathf.Max(0.01f, Display.effectRange > 0f ? Display.effectRange : 1f);
                float width = Mathf.Max(0.01f, Display.effectWidth > 0f ? Display.effectWidth : 1f);
                _directional.position = directionalOrigin + dir * (length * 0.5f);
                _directional.rotation = Quaternion.LookRotation(dir, Vector3.up);
                _directional.localScale = new Vector3(width, 1f, length);
            }

            if (_cone != null && _cone.gameObject.activeSelf)
            {
                EnsureConeMesh(Display.effectAngle);
                Vector3 dir = ResolveActiveDirection(aim);
                float range = Mathf.Max(0.05f, Display.effectRange > 0f ? Display.effectRange : 1f);
                _cone.position = directionalOrigin;
                _cone.rotation = Quaternion.LookRotation(dir, Vector3.up);
                _cone.localScale = new Vector3(range, 1f, range);
            }
        }

        private Vector3 ResolveActiveDirection(Vector3 aimPoint)
        {
            if (Display.aimFollowMode == SkillIndicatorData.AimFollowMode.LockOnConfirm
                && _lockedDirection.HasValue)
            {
                return _lockedDirection.Value;
            }

            if (_caster == null)
                return Vector3.forward;

            Vector2 moveInput = _hasMoveInput
                ? _moveInput
                : new Vector2(_caster.horizontalInput, _caster.verticalInput);

            return SkillAimUtil.ResolveDirection(
                _caster,
                aimPoint,
                moveInput,
                Display.directionSource);
        }

        public void Dispose()
        {
            if (_coneFilter != null && _coneFilter.sharedMesh != null)
                Object.Destroy(_coneFilter.sharedMesh);

            if (_root != null)
                Object.Destroy(_root.gameObject);

            if (_rangeRingMaterialInstance != null)
                Object.Destroy(_rangeRingMaterialInstance);
            if (_placementMaterialInstance != null)
                Object.Destroy(_placementMaterialInstance);
        }

        private void EnsureConeMesh(float angleDegrees)
        {
            float angle = Mathf.Clamp(angleDegrees > 0f ? angleDegrees : 90f, 1f, 360f);
            if (_coneFilter == null || Mathf.Abs(angle - _builtConeAngle) < 0.01f)
                return;

            Mesh old = _coneFilter.sharedMesh;
            _coneFilter.sharedMesh = MeshFactory.BuildCone(1f, angle);
            _builtConeAngle = angle;

            if (old != null)
                Object.Destroy(old);
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

            return _defaultRangeTexture != null
                ? _defaultRangeTexture
                : GetDefaultPlacementTexture(SkillIndicatorData.IndicatorShape.Circle);
        }

        private static Texture2D GetDefaultPlacementTexture(SkillIndicatorData.IndicatorShape shape)
        {
            if (shape == SkillIndicatorData.IndicatorShape.Cone)
            {
                if (_defaultConeTexture == null)
                    _defaultConeTexture = Resources.Load<Texture2D>(DefaultConeTextureResource);

                if (_defaultConeTexture != null)
                    return _defaultConeTexture;
            }

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

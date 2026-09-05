using ShadowInfection.Animations;
using ShadowInfection.Items;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class UnitAnimationController : MonoBehaviour
{
    private const string AttackLayerName = "Attack";
    private const string CastLayerName = "Cast";
    private const string AttackTriggerName = "Attack";
    private const string CastTriggerName = "Cast";
    private const string CastEndTriggerName = "CastEnd";
    private const string IsCastingParamName = "IsCasting";
    private const string StanceParamName = "Stance";
    private const string StanceBlendParamName = "StanceBlend";
    private const string LayerIdleStateName = "None";

    public float maxSpeed = 5f;
    UnitController unitController;
    UnitActionState actionState;
    Animator animator;
    bool wasCasting;
    AnimatorOverrideController runtimeOverride;
    RuntimeAnimatorController boundSource;
    AnimationSetData boundAnimSet;
    AnimationClip attackMainPlaceholder;
    AnimationClip attackOffPlaceholder;

    void Awake() {
        animator = GetComponent<Animator>();
        animator.applyRootMotion = false;
        unitController = GetComponentInParent<UnitController>();
        if (unitController == null)
        {
            Debug.LogWarning("UnitAnimationController: Missing UnitController reference.", this);
            enabled = false;
            return;
        }
        animator.fireEvents = false;
        unitController.OnAttackStart += HandleOnAttackStartChange;
        unitController.OnHealthChange += HandleOnHealthChange;
        unitController.OnTakeDamage += HandleOnTakeDamage;
        unitController.OnWeaponChange += HandleOnWeaponChange;
        unitController.OnActionInterrupted += HandleOnActionInterrupted;

        actionState = unitController.unitActionState != null
            ? unitController.unitActionState
            : unitController.GetComponent<UnitActionState>();
        if (actionState != null)
            actionState.OnActionStateChanged += HandleOnActionStateChanged;

        BindUnitAnimator();
    }

    void Start()
    {
        BindUnitAnimator();
    }

    void OnDestroy()
    {
        if (unitController != null) {
            unitController.OnAttackStart -= HandleOnAttackStartChange;
            unitController.OnHealthChange -= HandleOnHealthChange;
            unitController.OnTakeDamage -= HandleOnTakeDamage;
            unitController.OnWeaponChange -= HandleOnWeaponChange;
            unitController.OnActionInterrupted -= HandleOnActionInterrupted;
        }
        if (actionState != null)
            actionState.OnActionStateChanged -= HandleOnActionStateChanged;
    }

    void Update()
    {
        HandleMovementAnimation();
    }

    private void HandleMovementAnimation () {
        float horizontal = unitController.horizontalInput;
        float vertical = unitController.verticalInput;
        Vector3 movement = new Vector3(horizontal, 0f, vertical);

        float percentMoveSpeed = Mathf.Clamp01(unitController.moveSpeed / maxSpeed);

        Vector3 movmentLerped = Vector3.Lerp(Vector3.zero, movement.normalized, percentMoveSpeed);

        float velocityZ = Vector3.Dot(movmentLerped, transform.forward);
        float velocityX = Vector3.Dot(movmentLerped, transform.right);

        animator.SetFloat("VelocityZ", velocityZ, 0.1f, Time.deltaTime);
        animator.SetFloat("VelocityX", velocityX, 0.1f, Time.deltaTime);
    }

    private void HandleOnAttackStartChange((UnitController unitController, int attackIndex) obj)
    {
        var offHand = obj.attackIndex == 1;
        var swinging = unitController.GetWeaponForAttackIndex(obj.attackIndex);
        if (swinging != null)
            SetAttackTime(swinging.attackTime);

        if (HasParam("Health"))
            animator.SetInteger("Health", unitController.health);

        ApplyAttackClip(swinging, offHand);

        if (HasParam("AttackVersion"))
            animator.SetInteger("AttackVersion", offHand ? 1 : 0);
        if (HasParam(AttackTriggerName))
        {
            animator.ResetTrigger(AttackTriggerName);
            animator.SetTrigger(AttackTriggerName);
        }
    }

    private void ApplyAttackClip(WeaponData swinging, bool offHand)
    {
        if (runtimeOverride == null)
            return;

        var placeholder = offHand ? attackOffPlaceholder : attackMainPlaceholder;
        if (placeholder == null)
            return;

        var weaponType = swinging != null ? swinging.weaponType : WeaponType.Unarmed;
        var clip = boundAnimSet != null
            ? boundAnimSet.PickAttackClip(weaponType, offHand)
            : null;
        if (clip == null && boundAnimSet != null && offHand)
            clip = boundAnimSet.PickAttackClip(weaponType, false);
        if (clip == null)
            clip = placeholder;

        runtimeOverride[placeholder] = clip;
    }

    private void HandleOnActionInterrupted(
        (UnitController target, UnitActionState.ActionStateData interruptedAction) data)
    {
        if (data.interruptedAction.type == UnitActionState.ActionType.Attacking)
            InterruptLayer(AttackLayerName, AttackTriggerName);

        if (data.interruptedAction.type == UnitActionState.ActionType.Casting
            || data.interruptedAction.type == UnitActionState.ActionType.Channeling)
            InterruptCastAnimation();
    }

    private void HandleOnActionStateChanged(UnitActionState actionState)
    {
        bool casting = IsCastingOrChanneling(actionState);
        if (casting == wasCasting)
            return;

        if (casting)
            BeginCastAnimation();
        else
            EndCastAnimation();

        wasCasting = casting;
    }

    private static bool IsCastingOrChanneling(UnitActionState actionState)
    {
        if (actionState == null)
            return false;

        return actionState.IsPerforming(UnitActionState.ActionType.Casting)
            || actionState.IsPerforming(UnitActionState.ActionType.Channeling);
    }

    private void BeginCastAnimation()
    {
        if (animator == null || !animator.isActiveAndEnabled)
            return;

        if (HasParam(CastEndTriggerName))
            animator.ResetTrigger(CastEndTriggerName);
        if (HasParam(IsCastingParamName))
            animator.SetBool(IsCastingParamName, true);
        if (HasParam(CastTriggerName))
        {
            animator.ResetTrigger(CastTriggerName);
            animator.SetTrigger(CastTriggerName);
        }
    }

    private void EndCastAnimation()
    {
        if (animator == null || !animator.isActiveAndEnabled)
            return;

        if (HasParam(CastTriggerName))
            animator.ResetTrigger(CastTriggerName);
        if (HasParam(IsCastingParamName))
            animator.SetBool(IsCastingParamName, false);
        if (HasParam(CastEndTriggerName))
        {
            animator.ResetTrigger(CastEndTriggerName);
            animator.SetTrigger(CastEndTriggerName);
        }
    }

    private void InterruptCastAnimation()
    {
        if (HasParam(CastTriggerName))
            animator.ResetTrigger(CastTriggerName);
        if (HasParam(CastEndTriggerName))
            animator.ResetTrigger(CastEndTriggerName);
        if (HasParam(IsCastingParamName))
            animator.SetBool(IsCastingParamName, false);

        InterruptLayer(CastLayerName, null);
        wasCasting = false;
    }

    private void InterruptLayer(string layerName, string triggerToReset)
    {
        if (animator == null || !animator.isActiveAndEnabled)
            return;

        if (!string.IsNullOrEmpty(triggerToReset) && HasParam(triggerToReset))
            animator.ResetTrigger(triggerToReset);

        int layer = animator.GetLayerIndex(layerName);
        if (layer < 0)
            return;

        animator.Play(LayerIdleStateName, layer, 0f);
        animator.Update(0f);
    }

    private void HandleOnHealthChange((int current, int max) health)
    {
        animator.SetInteger("Health", health.current);

        if (health.current <= 0) {
            animator.SetFloat("DeadSpeedMultiplier", 1f + Random.Range(-0.3f, 0.2f));
        }
    }

    private void HandleOnTakeDamage((UnitController unitController, UnitController attacker) obj)
    {
        animator.SetTrigger("Hitted");
    }

    private void SetAttackTime(float attackTime)
    {
        var baseAnimationDuration = 0.8f;
        float animationSpeed = baseAnimationDuration / attackTime;
        animator.SetFloat("AttackTime", animationSpeed / 2f);
    }

    private void HandleOnWeaponChange(UnitController unitController)
    {
        ApplyStance();
        if (unitController.currentWeapon != null)
            SetAttackTime(unitController.currentWeapon.attackTime);
    }

    private void BindUnitAnimator()
    {
        if (animator == null || unitController == null)
            return;

        boundAnimSet = unitController.modelData != null
            ? unitController.modelData.defaultAnimationSet
            : null;
        var source = boundAnimSet != null && boundAnimSet.animatorController != null
            ? boundAnimSet.animatorController
            : animator.runtimeAnimatorController;

        if (source != null && (runtimeOverride == null || boundSource != source))
        {
            runtimeOverride = new AnimatorOverrideController(source);
            runtimeOverride.hideFlags = HideFlags.HideAndDontSave;
            boundSource = source;
            animator.runtimeAnimatorController = runtimeOverride;
        }

        attackMainPlaceholder = null;
        attackOffPlaceholder = null;
        if (boundAnimSet != null)
            boundAnimSet.TryGetAttackPlaceholders(out attackMainPlaceholder, out attackOffPlaceholder);

        animator.applyRootMotion = false;
        animator.SetInteger("Health", unitController.health);
        ApplyStance();

        if (unitController.currentWeapon != null)
            SetAttackTime(unitController.currentWeapon.attackTime);

        wasCasting = IsCastingOrChanneling(actionState);
        if (wasCasting)
            BeginCastAnimation();
    }

    private void ApplyStance()
    {
        if (animator == null || unitController == null)
            return;

        var stance = (int)ResolveStance();
        if (HasParam(StanceParamName))
            animator.SetInteger(StanceParamName, stance);
        if (HasParam(StanceBlendParamName))
            animator.SetFloat(StanceBlendParamName, stance);
    }

    private AnimationStance ResolveStance()
    {
        return ItemRules.ResolveAnimationStance(
            unitController.currentWeapon,
            unitController.currentOffHandWeapon != null
                ? unitController.currentOffHandWeapon
                : unitController.offHandItemWeapon);
    }

    private bool HasParam(string name)
    {
        if (animator == null || string.IsNullOrEmpty(name))
            return false;

        var parameters = animator.parameters;
        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].name == name)
                return true;
        }

        return false;
    }
}

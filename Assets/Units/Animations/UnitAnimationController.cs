using UnityEngine;

public class UnitAnimationController : MonoBehaviour
{
    public float maxSpeed = 5f;
    UnitController unitController;
    Animator animator;

    void Awake() {
        unitController = GetComponentInParent<UnitController>();
        animator = GetComponent<Animator>();
        animator.fireEvents = false;
        unitController.OnAttackStart += HandleOnAttackStartChange;
        unitController.OnHealthChange += HandleOnHealthChange;
        unitController.OnTakeDamage += HandleOnTakeDamage;
        unitController.OnWeaponChange += HandleOnWeaponChange;

        SelectAnimator(unitController);
    }

    void OnDestroy()
    {
        if (unitController != null) {
            unitController.OnAttackStart -= HandleOnAttackStartChange;
            unitController.OnHealthChange -= HandleOnHealthChange;
            unitController.OnTakeDamage -= HandleOnTakeDamage;
            unitController.OnWeaponChange -= HandleOnWeaponChange;
        }    
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

    private int attackCounter = 0;

    private void HandleOnAttackStartChange(UnitController unitController)
    {
        if (unitController.currentWeapon != null)
        {
            SetAttackTime(unitController.currentWeapon.attackTime);
        }
        animator.SetInteger("AttackVersion", attackCounter % 2);
        animator.SetTrigger("Attack");
        attackCounter = (attackCounter + 1) % 4;
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
        SelectAnimator(unitController);
    }

    private void SelectAnimator(UnitController unitController) {

        if( unitController.modelData != null) {
            animator.runtimeAnimatorController = unitController.modelData.GetAnimationSetForWeapon(unitController.currentWeapon.weaponType).animatorController;
        }

        animator.SetInteger("Health", unitController.health);

        if (unitController.currentWeapon != null) {
            SetAttackTime(unitController.currentWeapon.attackTime);
        }
    }
}

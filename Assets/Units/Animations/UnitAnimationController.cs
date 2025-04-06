using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

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
        if (unitController.currentWeapon != null) {
            SetAttackTime(unitController.currentWeapon.attackTime);
        }
        animator.SetInteger("Health", unitController.health);
    }

    void OnDestroy()
    {
        if (unitController != null) {
            unitController.OnAttackStart -= HandleOnAttackStartChange;
            unitController.OnHealthChange -= HandleOnHealthChange;
            unitController.OnTakeDamage -= HandleOnTakeDamage;
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

    private void HandleOnAttackStartChange(UnitController unitController)
    {
        if (unitController.currentWeapon != null) {
            SetAttackTime(unitController.currentWeapon.attackTime);
        }
        animator.SetInteger("AttackVersion", Random.Range(0, 2));
        animator.SetTrigger("Attack");
        
    }

    private void HandleOnHealthChange((int current, int max) health)
    {
        animator.SetInteger("Health", health.current);
    }

    private void HandleOnTakeDamage(UnitController unitController)
    {
        animator.SetTrigger("Hitted");
    }

    private void SetAttackTime(float attackTime)
    {
        var baseAnimationDuration = 0.8f;
        float animationSpeed = baseAnimationDuration / attackTime;
        animator.SetFloat("AttackTime", animationSpeed / 2f);
    }
}

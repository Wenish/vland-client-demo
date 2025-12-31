using System.Collections.Generic;
using UnityEngine;

namespace NPCBehaviour
{
    /// <summary>
    /// Attack state where NPC uses skills on target.
    /// Uses SkillSelector to choose which skill to use.
    /// </summary>
    [CreateAssetMenu(fileName = "AttackState", menuName = "Game/NPC Behaviour/States/Attack")]
    public class AttackState : BehaviourState
    {
        [Header("Attack Behaviour")]
        [Tooltip("Transitions from this state")]
        public List<BehaviourTransition> transitions = new();

        [Header("Skill Usage")]
        [Tooltip("Strategy for selecting which skill to use")]
        public SkillSelector skillSelector;

        [Tooltip("Minimum time between skill attempts (seconds)")]
        public float skillCooldown = 1f;

        [Header("Facing")]
        [Tooltip("Should the NPC face its target while attacking?")]
        public bool faceTarget = true;

        public override void OnEnter(BehaviourContext context)
        {
            context.LastSkillUseTime = 0f;
            context.RefreshAvailableSkills();
        }

        public override bool OnUpdate(BehaviourContext context, float deltaTime)
        {
            context.TimeInState += deltaTime;

            // No target = exit state
            if (context.CurrentTarget == null || context.CurrentTarget.IsDead)
            {
                return false;
            }

            // Face target if enabled
            if (faceTarget)
            {
                FaceTarget(context);
            }

            // Try to use a skill
            if (Time.time - context.LastSkillUseTime >= skillCooldown)
            {
                TryUseSkill(context);
            }
            
            TryUseAutoAttack(context);

            return true;
        }

        public override void OnExit(BehaviourContext context)
        {
            // Nothing specific to clean up
        }

        public override BehaviourTransition EvaluateTransitions(BehaviourContext context)
        {
            foreach (var transition in transitions)
            {
                if (transition != null && transition.CanTransition(context))
                {
                    return transition;
                }
            }

            return null;
        }

        private void FaceTarget(BehaviourContext context)
        {
            if (context.CurrentTarget == null) return;

            Vector3 direction = context.CurrentTarget.transform.position - context.Position;
            direction.y = 0f;

            if (direction.sqrMagnitude > 0.01f)
            {
                float angle = -Mathf.Atan2(direction.z, direction.x) * Mathf.Rad2Deg - 90f + 180f;
                context.Unit.angle = angle;
            }
        }

        private void TryUseSkill(BehaviourContext context)
        {
            if (skillSelector == null)
            {
                Debug.LogWarning($"AttackState '{name}' has no skill selector assigned!");
                return;
            }

            // Refresh available skills
            context.RefreshAvailableSkills();
            var offCooldownSkills = context.GetOffCooldownSkills();

            if (offCooldownSkills.Count == 0) return;

            // Select a skill
            var selectedSkill = skillSelector.SelectSkill(context, offCooldownSkills);

            if (selectedSkill != null)
            {
                // Set aim point towards target
                Vector3? aimPoint = context.CurrentTarget != null 
                    ? context.CurrentTarget.transform.position 
                    : (Vector3?)null;

                // Trigger the skill
                selectedSkill.Cast(aimPoint);
                context.LastSkillUseTime = Time.time;
            }
        }

        private void TryUseAutoAttack(BehaviourContext context)
        {
            context.Unit.Attack();
            context.LastSkillUseTime = Time.time;
        }
    }
}

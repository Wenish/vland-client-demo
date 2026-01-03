using System.Collections.Generic;
using UnityEngine;

namespace NPCBehaviour
{
    /// <summary>
    /// Attack state where NPC uses skills on target.
    /// Uses SkillSelector to choose which skill to use.
    /// </summary>
    [CreateAssetMenu(fileName = "StateAttack", menuName = "Game/NPC Behaviour/States/Attack")]
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

        [Tooltip("Minimum time between auto-attacks (seconds)")]
        public float autoAttackCooldown = 0.5f;

        [Header("Facing")]
        [Tooltip("Should the NPC face its target while attacking?")]
        public bool faceTarget = true;

        [Tooltip("Re-evaluate threat target periodically")]
        public bool updateThreatTarget = false;

        [Tooltip("How often to update threat target (in seconds)")]
        public float threatUpdateInterval = 1f;

        public override void OnEnter(BehaviourContext context)
        {
            context.LastSkillUseTime = 0f;
            context.LastAttackTime = 0f;
            context.RefreshAvailableSkills();
            context.SetStateData("lastThreatUpdate", 0f);
        }

        public override bool OnUpdate(BehaviourContext context, float deltaTime)
        {
            context.TimeInState += deltaTime;

            // Re-evaluate threat target if enabled
            if (updateThreatTarget && context.HasThreatSystem)
            {
                float lastUpdate = context.GetStateData("lastThreatUpdate", 0f);
                if (Time.time - lastUpdate >= threatUpdateInterval)
                {
                    var threatTarget = context.GetHighestThreatTarget();
                    if (threatTarget != null && !threatTarget.IsDead)
                    {
                        context.CurrentTarget = threatTarget;
                    }
                    context.SetStateData("lastThreatUpdate", Time.time);
                }
            }

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

            // Try to use a skill, or auto-attack as fallback
            bool skillUsed = false;
            if (Time.time - context.LastSkillUseTime >= skillCooldown)
            {
                skillUsed = TryUseSkill(context);
            }
            
            // Use auto-attack if no skill was used
            if (!skillUsed)
            {
                TryUseAutoAttack(context);
            }

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

        private bool TryUseSkill(BehaviourContext context)
        {
            if (skillSelector == null)
            {
                Debug.LogWarning($"AttackState '{name}' has no skill selector assigned!");
                return false;
            }

            // Refresh available skills
            context.RefreshAvailableSkills();
            var offCooldownSkills = context.GetOffCooldownSkills();

            if (offCooldownSkills.Count == 0) return false;

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
                return true;
            }
            
            return false;
        }

        private void TryUseAutoAttack(BehaviourContext context)
        {
            // Only auto-attack if cooldown has elapsed
            if (Time.time - context.LastAttackTime < autoAttackCooldown)
                return;
            
            context.Unit.Attack();
            context.LastAttackTime = Time.time;
        }
    }
}

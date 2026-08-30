namespace ShadowInfection.Input
{
    public static class PlayerActionSkillMap
    {
        public static readonly PlayerActionId[] SkillActions =
        {
            PlayerActionId.Skill1,
            PlayerActionId.Skill2,
            PlayerActionId.Skill3,
            PlayerActionId.Ultimate
        };

        public static bool TryGetSlot(PlayerActionId id, out SkillSlotType slot, out int index)
        {
            switch (id)
            {
                case PlayerActionId.Skill1:
                    slot = SkillSlotType.Normal;
                    index = 0;
                    return true;
                case PlayerActionId.Skill2:
                    slot = SkillSlotType.Normal;
                    index = 1;
                    return true;
                case PlayerActionId.Skill3:
                    slot = SkillSlotType.Normal;
                    index = 2;
                    return true;
                case PlayerActionId.Ultimate:
                    slot = SkillSlotType.Ultimate;
                    index = 0;
                    return true;
                default:
                    slot = default;
                    index = -1;
                    return false;
            }
        }
    }
}

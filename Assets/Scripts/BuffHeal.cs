public class BuffHeal : Buff
{
    public int HealAmount;

    public BuffHeal(float duration, float tickInterval, int healAmount)
    {
        BuffId = "BuffHeal";
        Duration = duration;
        TickInterval = tickInterval;
        HealAmount = healAmount;
        IsPeriodic = true;
    }

    public override void OnTick(UnitMediator mediator)
    {
        base.OnTick(mediator);
        mediator.UnitController.Heal(HealAmount);
    }
}
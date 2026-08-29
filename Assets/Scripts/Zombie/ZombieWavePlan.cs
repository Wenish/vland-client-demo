using System.Collections.Generic;

namespace ShadowInfection.Zombie
{
    public sealed class ZombieWavePlan
    {
        public Queue<string> SpawnQueue;
        public float HealthMultiplier;
        public float DamageMultiplier;
        public bool IsRecurringSpecial;
    }
}

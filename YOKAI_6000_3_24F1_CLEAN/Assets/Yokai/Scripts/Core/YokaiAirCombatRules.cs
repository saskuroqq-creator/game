namespace Yokai
{
    public static class YokaiAirCombatRules
    {
        public const int MaxAirHits = 3;
        public static bool CanContinueCombo(int completedHits)
        {
            return completedHits >= 0 && completedHits < MaxAirHits;
        }
        public static bool CanLaunch(YokaiEnemyArchetype archetype, bool postureBroken)
        {
            if (archetype == YokaiEnemyArchetype.Heavy || archetype == YokaiEnemyArchetype.Boss) return false;
            return archetype != YokaiEnemyArchetype.Elite || postureBroken;
        }
    }
}

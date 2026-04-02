namespace Gridlock.Combat
{
    public struct DamageInfo
    {
        public float Amount;
        public DamageType Type;

        public DamageInfo(float amount, DamageType type = DamageType.Direct)
        {
            Amount = amount;
            Type = type;
        }
    }

    public enum DamageType
    {
        Direct,
        Projectile,
        DamageOverTime
    }
}

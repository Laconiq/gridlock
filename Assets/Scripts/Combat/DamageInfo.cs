namespace AIWE.Combat
{
    public struct DamageInfo
    {
        public float Amount;
        public ulong SourceId;
        public DamageType Type;

        public DamageInfo(float amount, ulong sourceId, DamageType type = DamageType.Direct)
        {
            Amount = amount;
            SourceId = sourceId;
            Type = type;
        }
    }

    public enum DamageType
    {
        Direct,
        Projectile,
        Hitscan,
        DamageOverTime
    }
}

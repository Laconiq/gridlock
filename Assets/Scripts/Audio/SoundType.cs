namespace Gridlock.Audio
{
    public enum SoundType
    {
        // SFX — Combat
        TowerFire,
        ProjectileImpact,
        EnemyHit,
        EnemyDeath,
        ObjectiveHit,

        // SFX — Tower
        TowerPlace,
        TowerPlaceInvalid,
        TowerHover,

        // SFX — Waves
        WaveStart,
        WaveComplete,
        GameOver,

        // SFX — Loot
        LootDrop,
        LootCollect,

        // UI — Mod Slots
        NodeGrab,
        NodeDrop,
        NodeRemove,
        PortConnect,
        PortDisconnect,
        EditorOpen,
        EditorClose,
        InventoryOpen,
        InventoryClose,

        // UI — Drag & Drop
        UIDragStart,
        UIDragHover,
        UIDropFail,

        // UI — General
        UIClick,
        UIHover,
        UIAnnounce,
        UIDropdownOpen,
        UIDropdownSelect
    }
}

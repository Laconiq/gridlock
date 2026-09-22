namespace Gridlock.Audio
{
    public enum SoundType
    {
        // Combat
        TowerFire,
        ProjectileImpact,
        EnemyHit,
        EnemyDeath,
        ObjectiveHit,

        // Tower
        TowerPlace,
        TowerPlaceInvalid,
        TowerHover,

        // Waves
        WaveStart,
        WaveComplete,
        GameOver,

        // Loot
        LootDrop,
        LootCollect,

        // Mod Slots
        NodeGrab,
        NodeDrop,
        NodeRemove,
        PortConnect,
        PortDisconnect,
        EditorOpen,
        EditorClose,
        InventoryOpen,
        InventoryClose,

        // Drag & Drop
        UIDragStart,
        UIDragHover,
        UIDropFail,

        // General UI
        UIClick,
        UIHover,
        UIAnnounce,
        UIDropdownOpen,
        UIDropdownSelect
    }
}

using UnityEngine;

namespace Gridlock.Towers
{
    public class TowerChassis : MonoBehaviour
    {
        [SerializeField] private float baseRange = 10f;
        [SerializeField] private float baseDamage = 5f;
        [SerializeField] private float fireRate = 2f;
        [SerializeField] private int slotCount = 5;
        [SerializeField] private Transform firePoint;

        public Transform FirePoint => firePoint;
        public float BaseRange => baseRange;
        public float BaseDamage => baseDamage;
        public float FireRate => fireRate;
        public int SlotCount => slotCount;
    }
}

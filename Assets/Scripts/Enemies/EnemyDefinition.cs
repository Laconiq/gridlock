using Gridlock.Visual;
using UnityEngine;

namespace Gridlock.Enemies
{
    [CreateAssetMenu(menuName = "Gridlock/Enemy Definition")]
    public class EnemyDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string enemyId;
        public Color color = Color.red;
        public ShapeType shape = ShapeType.Triangle;

        [Header("Stats")]
        public float maxHP = 100f;
        public float moveSpeed = 3f;
        public float scale = 1f;

        [Header("Melee Attack")]
        public float attackDamage = 10f;
        public float attackRange = 1.5f;
        public float attackCooldown = 1f;

        [Header("Objective")]
        public float objectiveDamage = 10f;

        [Header("AI")]
        public float detectionRadius = 12f;
        public float leashRadius = 15f;
    }
}

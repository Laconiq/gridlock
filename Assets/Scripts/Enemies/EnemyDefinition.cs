using UnityEngine;

namespace Gridlock.Enemies
{
    [CreateAssetMenu(menuName = "Gridlock/Enemy Definition")]
    public class EnemyDefinition : ScriptableObject
    {
        [Header("Visuals")]
        public Mesh mesh;
        public Material material;
        public Color color = Color.red;
        public Vector3 scale = Vector3.one;

        [Header("Stats")]
        public float maxHP = 100f;
        public float moveSpeed = 3f;

        [Header("Objective")]
        public float objectiveDamage = 10f;
    }
}

using UnityEngine;

namespace AIWE.Enemies
{
    [CreateAssetMenu(menuName = "AIWE/Enemy Definition")]
    public class EnemyDefinition : ScriptableObject
    {
        public string enemyId;
        public float maxHP = 100f;
        public float moveSpeed = 3f;
        public float scale = 1f;
        public Color color = Color.red;
    }
}

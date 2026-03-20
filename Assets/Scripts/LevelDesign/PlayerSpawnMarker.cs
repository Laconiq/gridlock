using LDtkUnity;
using UnityEngine;

namespace AIWE.LevelDesign
{
    public class PlayerSpawnMarker : MonoBehaviour, ILDtkImportedFields
    {
        [SerializeField] private int playerIndex;
        public int PlayerIndex => playerIndex;
        public void OnLDtkImportFields(LDtkFields fields) { playerIndex = fields.GetInt("player_index"); }
    }
}

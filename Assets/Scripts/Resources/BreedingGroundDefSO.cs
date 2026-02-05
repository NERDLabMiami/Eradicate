using UnityEngine;

namespace Eradicate
{
    [CreateAssetMenu(menuName = "Eradicate/Breeding Ground", fileName = "Ground_")]
    public class BreedingGroundDefSO : ScriptableObject
    {
        public BreedingGroundType type;

        [Header("Front Visuals")]
        public Sprite groundImage;                   // e.g., kiddie pool photo/illustration
        public string displayName = "Kiddie Pool";
        public int startingEggs = 3;
    }
}
using UnityEngine;

namespace Eradicate
{
    [CreateAssetMenu(menuName = "Eradicate/Human", fileName = "Human_")]
    public class HumanDefSO : ScriptableObject
    {
        public HumanColor color;

        [Header("Front Visuals")]
        public Color cardColor = Color.white;        // background tint
        public Sprite humanIcon;                     // white/mono icon (optional)

    }
}
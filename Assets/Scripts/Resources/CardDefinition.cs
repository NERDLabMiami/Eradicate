// CardDefinition.cs
// Drop-in ScriptableObject card definition for Eradicate!
//
// This version is designed to be *authorable in the Unity Inspector* without needing
// [SerializeReference] drawers or custom editors.
//
// How it stays "clean":
// - The card has ONE discriminant: EffectType
// - Only the relevant payload field is shown (via [ShowIf] logic using Unity's built-in inspector limitations)
//   Since Unity does not natively support conditional field visibility without a custom editor,
//   we still *store* the minimal fields, but we:
//     1) Group them clearly
//     2) Add OnValidate() to auto-clear irrelevant fields so cards can't accidentally carry junk.
//
// If you later want truly one-payload-per-card with perfect inspector UX, use subclasses
// (BiteCardDef, BreedCardDef, etc.). For now, this is a robust prototype-friendly middle ground.

using UnityEngine;

namespace Eradicate
{
    public enum CardEffectType
    {
        Mosquito_Bite,
        Mosquito_Breed,
        Human_Protect,
        Human_Clear,
        Event
    }

    public enum HumanColor { Orange, Blue, Purple, Green }
    public enum BreedingGroundType { Tarp, Wheelbarrow, TrashCan, KiddiePool, Tire }

    public enum EventEffectType
    {
        CommunityCleanup,
        Drought_KiddiePool,
        Drought_Tarp,
        Habitat,
        Rain,
        Warming
    }

    [CreateAssetMenu(menuName = "Eradicate/Action or Event", fileName = "Card_")]
    public class CardDefinition : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Unique id for debugging / save-load / analytics.")]
        public string cardId;

        [Tooltip("The single, authoritative meaning of this card.")]
        public CardEffectType effectType;

        [Header("Presentation")]
        public string title;
        [TextArea(2, 6)] public string rulesText;
        [TextArea(1, 4)] public string flavorText;
        public Sprite artwork;

        // --------------------------------------------------------------------
        // Payload (only ONE of these should be meaningful, based on effectType)
        // --------------------------------------------------------------------

        [Header("Payload: Human Target (Bite / Protect)")]
        [Tooltip("Used by Mosquito_Bite and Human_Protect.")]
        public HumanColor humanColor;

        [Header("Payload: Breeding Ground Target (Breed / Clear)")]
        [Tooltip("Used by Mosquito_Breed and Human_Clear.")]
        public BreedingGroundType breedingGround;

        [Header("Payload: Event")]
        [Tooltip("Used by Event.")]
        public EventEffectType eventEffect;

        // Optional: tiny helper for code clarity
        public bool UsesHumanColor =>
            effectType == CardEffectType.Mosquito_Bite ||
            effectType == CardEffectType.Human_Protect;

        public bool UsesBreedingGround =>
            effectType == CardEffectType.Mosquito_Breed ||
            effectType == CardEffectType.Human_Clear;

        public bool UsesEvent =>
            effectType == CardEffectType.Event;

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Auto-clear irrelevant payloads so authored cards stay conceptually clean.
            switch (effectType)
            {
                case CardEffectType.Mosquito_Bite:
                case CardEffectType.Human_Protect:
                    breedingGround = default;
                    eventEffect = default;
                    break;

                case CardEffectType.Mosquito_Breed:
                case CardEffectType.Human_Clear:
                    humanColor = default;
                    eventEffect = default;
                    break;

                case CardEffectType.Event:
                    humanColor = default;
                    breedingGround = default;
                    break;
            }

            // Provide a default title if empty (nice for quick authoring).
            if (string.IsNullOrWhiteSpace(title))
                title = effectType.ToString();
        }
#endif
    }
}

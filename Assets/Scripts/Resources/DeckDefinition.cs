using System.Collections.Generic;
using UnityEngine;

namespace Eradicate
{
    [CreateAssetMenu(menuName = "Eradicate/Deck Definition", fileName = "Deck_")]
    public class DeckDefinition : ScriptableObject
    {
        [Tooltip("Cards included in this deck. Add duplicates by adding the same CardDefinition multiple times.")]
        public List<CardDefinition> cards = new();

        public int Count => cards != null ? cards.Count : 0;
    }
}
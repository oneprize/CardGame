using UnityEngine;
using CardGame.Drawing;

namespace CardGame.Cards
{
    public enum CardType
    {
        Attack,
        Defense,
        AttackAll,
        Weaken,
        Enfeeble,
        Strength,
        Draw,
        Poison,         // apply effectValue Poison to the selected target
        PoisonAura,     // apply effectValue Poison to ALL enemies at the start of every player turn, for the rest of the battle
        PoisonBounce,   // apply effectValue Poison, hitCount times, each to a random alive enemy
        PoisonMultiply, // double the selected target's current Poison stacks
        PoisonExplode,  // apply effectValue Poison to the target; when it dies, it deals its max HP as damage to all other enemies
        Retain,         // at end of turn, keep effectValue cards in hand instead of discarding them, for the rest of the battle
        Thorns,         // gain effectValue Thorns: reflect that much damage back at any enemy that attacks you, for 3 turns
        Barricade,      // Block no longer resets at the start of your turn, for the rest of the battle
        BlockMultiply,  // double your current Block
        DefendAndWeaken // gain effectValue Block and apply Weaken to the selected target
    }

    [CreateAssetMenu(menuName = "CardGame/Card", fileName = "NewCard")]
    public class CardData : ScriptableObject
    {
        public string id;
        public string displayName;
        [TextArea] public string description;
        public int cost = 1;
        public CardType cardType;
        public int effectValue;

        // Attack-only: number of separate hits (each dealing effectValue) and whether it skips Block.
        // Also reused by PoisonBounce for its "N times" count.
        public int hitCount = 1;
        public bool ignoresBlock;

        // If true, this card is removed from the deck for the rest of the battle after being played
        // instead of going to the discard pile.
        public bool exhausts;

        public GuideShapeType guideShape;
        public float timeLimit = 3f;

        // Short "[cost] stat" text shown on card buttons - shared by hand/reward/shop/deck-view UI.
        public string GetStatsText()
        {
            switch (cardType)
            {
                case CardType.Attack:
                    string attackText = hitCount > 1 ? $"공격 {effectValue} x{hitCount}" : $"공격 {effectValue}";
                    if (ignoresBlock) attackText += " (방어 무시)";
                    return attackText;
                case CardType.Defense:
                    return $"방어 {effectValue}";
                case CardType.AttackAll:
                    return $"전체공격 {effectValue}";
                case CardType.Weaken:
                    return "약화 부여";
                case CardType.Enfeeble:
                    return "무력화 부여";
                case CardType.Strength:
                    return $"힘 +{effectValue}";
                case CardType.Draw:
                    return $"카드 {effectValue}장 뽑기";
                case CardType.Poison:
                    return $"중독 {effectValue} 부여";
                case CardType.PoisonAura:
                    return $"매턴 전체 중독 {effectValue}";
                case CardType.PoisonBounce:
                    return $"무작위 중독 {effectValue} x{hitCount}";
                case CardType.PoisonMultiply:
                    return "중독 2배";
                case CardType.PoisonExplode:
                    return $"중독 {effectValue} + 시체 폭발";
                case CardType.Retain:
                    return $"카드 {effectValue}장 유지";
                case CardType.Thorns:
                    return $"가시 {effectValue}";
                case CardType.Barricade:
                    return "방어 유지";
                case CardType.BlockMultiply:
                    return "방어 2배";
                case CardType.DefendAndWeaken:
                    return $"방어 {effectValue} + 약화 부여";
                default:
                    return cardType.ToString();
            }
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace CardGame.Combat
{
    // What an enemy does on one of its own turns. Attack is the only action for every enemy that
    // doesn't set a turnPattern - this keeps all existing (patternless) enemies unchanged.
    public enum EnemyTurnAction
    {
        Attack,
        ShieldNegate,  // no attack this turn; the next hit this enemy takes (any source) is nullified
        Summon,        // no attack this turn; fills empty enemy slots with summonEnemyData
        Defend,        // gain defendAmount Block AND add defendAmount to this enemy's charge total
        AttackCharge,  // attack the player for this enemy's whole accumulated charge, then reset it to 0
        Split          // halve the highest-HP living instance of this enemy and clone it into an empty slot
    }

    [CreateAssetMenu(menuName = "CardGame/Enemy", fileName = "NewEnemy")]
    public class EnemyData : ScriptableObject
    {
        public string displayName;
        public int maxHP = 40;
        public int attackDamage = 7;
        public int goldReward = 5;

        // Optional - left null until real art exists for this monster. EnemySlotView falls back to
        // whatever sprite is already on the slot when this is unset, so monsters without art yet
        // don't regress to a blank box.
        public Sprite portrait;

        // Optional repeating action cycle for this enemy's own turns. Empty/null means "just attack
        // every turn" (the default for every enemy that doesn't need a special pattern).
        public List<EnemyTurnAction> turnPattern;

        // Used by the Summon action.
        public EnemyData summonEnemyData;
        public int summonCount = 2;

        // Used by the Defend action - both the Block gained and the amount added to this enemy's
        // charge total (spent by a later AttackCharge step in the same pattern).
        public int defendAmount = 10;
    }
}

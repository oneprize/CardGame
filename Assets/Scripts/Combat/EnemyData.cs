using UnityEngine;

namespace CardGame.Combat
{
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
    }
}

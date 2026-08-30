using System;
using UnityEngine;

namespace CardGame.Combat
{
    public class CombatantHealth : MonoBehaviour
    {
        public int MaxHP { get; private set; }
        public int CurrentHP { get; private set; }
        public int Block { get; private set; }
        public bool IsDead => CurrentHP <= 0;

        public event Action OnChanged;

        public void Initialize(int maxHP)
        {
            MaxHP = maxHP;
            CurrentHP = maxHP;
            Block = 0;
            OnChanged?.Invoke();
        }

        public void AddBlock(int amount)
        {
            Block += amount;
            OnChanged?.Invoke();
        }

        public void ClearBlock()
        {
            Block = 0;
            OnChanged?.Invoke();
        }

        public void Heal(int amount)
        {
            CurrentHP = Mathf.Min(MaxHP, CurrentHP + amount);
            OnChanged?.Invoke();
        }

        public void MultiplyBlock(int factor)
        {
            Block *= Mathf.Max(0, factor);
            OnChanged?.Invoke();
        }

        // Block absorbs damage first, like most deckbuilders; excess spills over to HP.
        // ignoreBlock lets armor-piercing effects (e.g. Precise Stab) skip that absorption entirely.
        public void TakeDamage(int amount, bool ignoreBlock = false)
        {
            int absorbed = ignoreBlock ? 0 : Mathf.Min(Block, amount);
            Block -= absorbed;
            int remaining = amount - absorbed;
            CurrentHP = Mathf.Max(0, CurrentHP - remaining);
            OnChanged?.Invoke();
        }
    }
}

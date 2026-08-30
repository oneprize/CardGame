using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace CardGame.Combat
{
    // One enemy "seat" in combat - up to 3 active at once. Owns its own CombatantHealth. The slot's
    // own Image is the monster artwork; there's no stat text baked onto it anymore - clicking the
    // slot tells CombatController to target it and show its stats in the shared status readout.
    [RequireComponent(typeof(CombatantHealth))]
    public class EnemySlotView : MonoBehaviour
    {
        private const float HitFlashDuration = 0.15f;
        private const float AttackPunchDuration = 0.15f;
        private const float AttackPunchScale = 1.2f;
        private static readonly Color HitFlashColor = new Color(1f, 0.25f, 0.25f);

        [SerializeField] private Image background;
        [SerializeField] private Button selectButton;

        private static readonly Color NormalColor = Color.white;
        private static readonly Color SelectedColor = new Color(1f, 0.85f, 0.55f);

        public CombatantHealth Health { get; private set; }
        public EnemyData Data { get; private set; }

        // Weaken: this enemy takes 50% more damage. Enfeeble: this enemy deals 30% less damage.
        // Both tick down by one at the end of the enemy's own turn.
        public int WeakenTurns { get; private set; }
        public int EnfeebleTurns { get; private set; }
        public bool HasWeaken => WeakenTurns > 0;
        public bool HasEnfeeble => EnfeebleTurns > 0;

        // Poison: stacks additively, deals damage equal to its stack count at the start of this
        // enemy's own turn (ignoring Block), then decreases by 1. Doesn't expire on a turn timer.
        public int PoisonStacks { get; private set; }

        // Corpse Explosion: when this enemy dies, it deals its max HP as damage to every other
        // currently-alive enemy.
        public bool HasCorpseExplosion { get; private set; }

        public event Action OnClicked;

        private bool _isSelected;
        private Vector3 _baseScale;
        private Coroutine _flashCoroutine;
        private Coroutine _punchCoroutine;

        private void Awake()
        {
            Health = GetComponent<CombatantHealth>();
            selectButton.onClick.AddListener(() => OnClicked?.Invoke());
            // Source art comes in whatever aspect ratio the artist drew it at; scale to fit the
            // slot instead of stretching to fill it.
            background.preserveAspect = true;
            _baseScale = transform.localScale;
        }

        public void Show(EnemyData data)
        {
            Data = data;
            Health.Initialize(data.maxHP);
            if (data.portrait != null)
                background.sprite = data.portrait;

            if (_flashCoroutine != null) { StopCoroutine(_flashCoroutine); _flashCoroutine = null; }
            if (_punchCoroutine != null) { StopCoroutine(_punchCoroutine); _punchCoroutine = null; }
            transform.localScale = _baseScale;
            _isSelected = false;
            background.color = NormalColor;

            WeakenTurns = 0;
            EnfeebleTurns = 0;
            PoisonStacks = 0;
            HasCorpseExplosion = false;
            gameObject.SetActive(true);
        }

        // Called once this slot's HP hits 0 - removes it from view for the rest of the battle. The
        // slot GameObject itself is never destroyed since it's reused by the next stage's Show().
        public void Hide()
        {
            Data = null;
            gameObject.SetActive(false);
        }

        public void PlayHitFlash()
        {
            if (!gameObject.activeInHierarchy) return;
            if (_flashCoroutine != null) StopCoroutine(_flashCoroutine);
            _flashCoroutine = StartCoroutine(HitFlashRoutine());
        }

        private IEnumerator HitFlashRoutine()
        {
            background.color = HitFlashColor;
            yield return new WaitForSeconds(HitFlashDuration);
            background.color = _isSelected ? SelectedColor : NormalColor;
            _flashCoroutine = null;
        }

        public void PlayAttackPunch()
        {
            if (!gameObject.activeInHierarchy) return;
            if (_punchCoroutine != null) StopCoroutine(_punchCoroutine);
            _punchCoroutine = StartCoroutine(AttackPunchRoutine());
        }

        private IEnumerator AttackPunchRoutine()
        {
            transform.localScale = _baseScale * AttackPunchScale;
            yield return new WaitForSeconds(AttackPunchDuration);
            transform.localScale = _baseScale;
            _punchCoroutine = null;
        }

        public void ApplyWeaken(int turns)
        {
            WeakenTurns = Mathf.Max(WeakenTurns, turns);
        }

        public void ApplyEnfeeble(int turns)
        {
            EnfeebleTurns = Mathf.Max(EnfeebleTurns, turns);
        }

        public void TickStatusEffects()
        {
            if (WeakenTurns > 0) WeakenTurns--;
            if (EnfeebleTurns > 0) EnfeebleTurns--;
        }

        public void ApplyPoison(int amount)
        {
            PoisonStacks += Mathf.Max(0, amount);
        }

        public void MultiplyPoison(int factor)
        {
            PoisonStacks *= factor;
        }

        public void TickPoison()
        {
            if (PoisonStacks > 0) PoisonStacks--;
        }

        public void ApplyCorpseExplosion()
        {
            HasCorpseExplosion = true;
        }

        public string GetStatusText()
        {
            if (Data == null) return string.Empty;
            string intent = Health.IsDead ? "처치됨" : $"다음 공격: {Data.attackDamage}";
            string status = "";
            if (HasWeaken) status += $"  [약화 {WeakenTurns}턴]";
            if (HasEnfeeble) status += $"  [무력화 {EnfeebleTurns}턴]";
            if (PoisonStacks > 0) status += $"  [중독 {PoisonStacks}]";
            if (HasCorpseExplosion) status += "  [시체 폭발]";
            return $"{Data.displayName}  HP {Health.CurrentHP}/{Health.MaxHP}\n{intent}{status}";
        }

        public void SetSelected(bool selected)
        {
            _isSelected = selected;
            background.color = selected ? SelectedColor : NormalColor;
        }
    }
}

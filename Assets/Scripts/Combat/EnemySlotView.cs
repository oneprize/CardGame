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

        // Position in Data.turnPattern for this enemy's next own turn (wraps once it reaches the end).
        public int TurnPatternIndex { get; private set; }

        // ShieldNegate stacks: the next N times this enemy would take damage (any source, any
        // amount), each is nullified instead and consumes one stack.
        public int DamageNegationStacks { get; private set; }

        // Set on enemies filled in mid-battle via Summon so they don't act the turn they arrive.
        public bool SkipThisTurn { get; set; }

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
            TurnPatternIndex = 0;
            DamageNegationStacks = 0;
            SkipThisTurn = false;
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

        // Looks at the action this enemy will take on its next own turn without consuming it -
        // used both to resolve the turn and to preview the intent text.
        public EnemyTurnAction PeekNextPatternAction()
        {
            if (Data == null || Data.turnPattern == null || Data.turnPattern.Count == 0)
                return EnemyTurnAction.Attack;
            return Data.turnPattern[TurnPatternIndex % Data.turnPattern.Count];
        }

        public EnemyTurnAction ConsumeNextPatternAction()
        {
            EnemyTurnAction action = PeekNextPatternAction();
            TurnPatternIndex++;
            return action;
        }

        public void AddDamageNegation(int amount)
        {
            DamageNegationStacks += Mathf.Max(0, amount);
        }

        // Called by the damage funnel before applying any damage. Returns true (and consumes one
        // stack) if this hit should be nullified instead.
        public bool ConsumeDamageNegation()
        {
            if (DamageNegationStacks <= 0) return false;
            DamageNegationStacks--;
            return true;
        }

        public string GetStatusText()
        {
            if (Data == null) return string.Empty;
            string intent;
            if (Health.IsDead)
            {
                intent = "처치됨";
            }
            else
            {
                switch (PeekNextPatternAction())
                {
                    case EnemyTurnAction.ShieldNegate:
                        intent = "다음 행동: 피해 무효화 준비";
                        break;
                    case EnemyTurnAction.Summon:
                        intent = "다음 행동: 지원군 소환";
                        break;
                    case EnemyTurnAction.Defend:
                        intent = $"다음 행동: 방어 태세 (+{Data.defendAmount})";
                        break;
                    case EnemyTurnAction.AttackCharge:
                        intent = $"다음 공격: 축적된 방어력 {Health.Block}";
                        break;
                    case EnemyTurnAction.Split:
                        intent = "다음 행동: 분열";
                        break;
                    default:
                        intent = $"다음 공격: {Data.attackDamage}";
                        break;
                }
            }
            string status = "";
            if (HasWeaken) status += $"  [약화 {WeakenTurns}턴]";
            if (HasEnfeeble) status += $"  [무력화 {EnfeebleTurns}턴]";
            if (PoisonStacks > 0) status += $"  [중독 {PoisonStacks}]";
            if (HasCorpseExplosion) status += "  [시체 폭발]";
            if (DamageNegationStacks > 0) status += $"  [무효화 {DamageNegationStacks}]";
            return $"{Data.displayName}  HP {Health.CurrentHP}/{Health.MaxHP}\n{intent}{status}";
        }

        public void SetSelected(bool selected)
        {
            _isSelected = selected;
            background.color = selected ? SelectedColor : NormalColor;
        }
    }
}

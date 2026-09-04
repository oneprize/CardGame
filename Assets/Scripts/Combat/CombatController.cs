using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using CardGame.Cards;
using CardGame.Drawing;
using CardGame.UI;

namespace CardGame.Combat
{
    public class CombatController : MonoBehaviour
    {
        private const float SuccessThreshold = 80f;

        // All buffs/debuffs (Weaken, Enfeeble, Strength) last exactly this many of the affected
        // side's own turns before expiring.
        private const int StatusEffectDuration = 3;
        private const float WeakenDamageMultiplier = 0.5f;
        private const float EnfeebleDamageMultiplier = 0.3f;

        // Pause between each enemy's individual attack during the enemy turn, so they visibly act
        // one at a time instead of all their damage landing in the same frame.
        private const float EnemyAttackDelay = 0.5f;

        [Header("Player")]
        [SerializeField] private CombatantHealth playerHealth;
        [SerializeField] private int playerMaxHP = 50;

        [Header("Enemies (exactly 3 slots - unused ones are hidden per stage)")]
        [SerializeField] private EnemySlotView[] enemySlots;

        [Header("Deck")]
        [SerializeField] private int startingEnergy = 3;
        [SerializeField] private int handSize = 5;

        [Header("Drawing Challenge")]
        [SerializeField] private DrawChallengeController drawChallenge;

        [Header("UI")]
        [SerializeField] private HandView handView;
        [SerializeField] private Text stageLabelText;
        [SerializeField] private Text monsterInfoText;
        [SerializeField] private Text playerHPText;
        [SerializeField] private Text buffDebuffText;
        [SerializeField] private Text energyText;
        [SerializeField] private Text deckCountText;
        [SerializeField] private Text logText;
        [SerializeField] private Text resultText;
        [SerializeField] private Button endTurnButton;

        // Fired once per battle when it ends: true = player won, false = player died.
        public event Action<bool> OnCombatEnded;

        // Fired once per enemy kill with that enemy's gold reward, for the shop/bonfire run loop.
        public event Action<int> OnGoldEarned;

        public int PlayerCurrentHP => playerHealth.CurrentHP;
        public int PlayerMaxHP => playerHealth.MaxHP;

        private DeckRuntime _deck;
        private int _currentEnergy;
        private int _selectedEnemyIndex = -1;
        private bool _combatOver;
        private bool _awaitingChallenge;

        // Strength: bonus damage added to every attack instance. Ticks down like the enemy debuffs.
        private int _playerStrength;
        private int _playerStrengthTurns;

        // Noxious Fumes: Poison applied to all enemies at the start of every player turn for the
        // rest of the battle. Retain: cards kept in hand instead of discarded at end of turn, also
        // for the rest of the battle once played. Both accumulate if played more than once.
        private int _noxiousFumesPoisonPerTurn;
        private int _retainCount;

        // Thorns: reflects damage at any enemy that attacks the player. Ticks down like Strength.
        // Barricade: once played, Block stops resetting at the start of the player's turn.
        private int _playerThorns;
        private int _playerThornsTurns;
        private bool _blockPersists;

        // Lifesteal: while active, heals this % of any damage the player's attacks deal. Lasts only
        // until the start of the player's next turn (see StartPlayerTurn).
        private int _playerLifestealPercent;

        private void Awake()
        {
            endTurnButton.onClick.AddListener(EndPlayerTurn);
            for (int i = 0; i < enemySlots.Length; i++)
            {
                int index = i;
                enemySlots[i].OnClicked += () => SelectTarget(index);
            }
        }

        // Called once when a run starts, before the first StartBattle.
        public void BeginRun()
        {
            playerHealth.Initialize(playerMaxHP);
        }

        // Resets battle-local state and starts a fresh fight for `stage` using the run's current
        // deck. Safe to call repeatedly - this is what lets one scene host a whole run of battles.
        public void StartBattle(List<CardData> deck, StageData stage)
        {
            for (int i = 0; i < enemySlots.Length; i++)
            {
                if (i < stage.enemies.Count)
                    enemySlots[i].Show(stage.enemies[i]);
                else
                    enemySlots[i].Hide();
            }

            _selectedEnemyIndex = FindFirstAliveIndex();
            playerHealth.ClearBlock();
            _playerStrength = 0;
            _playerStrengthTurns = 0;
            _noxiousFumesPoisonPerTurn = 0;
            _retainCount = 0;
            _playerThorns = 0;
            _playerThornsTurns = 0;
            _blockPersists = false;
            _playerLifestealPercent = 0;

            _deck = new DeckRuntime(deck);
            _combatOver = false;
            _awaitingChallenge = false;
            if (resultText != null) resultText.text = string.Empty;
            if (stageLabelText != null) stageLabelText.text = stage.label;

            StartPlayerTurn();
        }

        private void StartPlayerTurn()
        {
            if (_combatOver) return;

            if (!_blockPersists)
                playerHealth.ClearBlock();
            _currentEnergy = startingEnergy;
            _playerLifestealPercent = 0;

            if (_noxiousFumesPoisonPerTurn > 0)
            {
                foreach (EnemySlotView slot in enemySlots)
                    if (slot.Data != null && !slot.Health.IsDead)
                        slot.ApplyPoison(_noxiousFumesPoisonPerTurn);
            }

            _deck.DrawCards(handSize);
            Log("당신의 턴 시작");
            RefreshUI();
        }

        private void SelectTarget(int index)
        {
            if (_awaitingChallenge || _combatOver) return;

            EnemySlotView slot = enemySlots[index];
            if (slot.Data == null || slot.Health.IsDead) return;

            _selectedEnemyIndex = index;
            RefreshEnemyUI();
        }

        private int FindFirstAliveIndex()
        {
            for (int i = 0; i < enemySlots.Length; i++)
                if (enemySlots[i].Data != null && !enemySlots[i].Health.IsDead)
                    return i;
            return -1;
        }

        private void OnCardClicked(CardData card)
        {
            if (_awaitingChallenge || _combatOver) return;
            if (_currentEnergy < card.cost)
            {
                Log("에너지가 부족합니다");
                return;
            }

            _currentEnergy -= card.cost;
            _awaitingChallenge = true;
            RefreshUI();

            drawChallenge.BeginChallenge(card.guideShape, card.timeLimit, SuccessThreshold, (success, score) =>
            {
                _awaitingChallenge = false;
                _deck.PlayCard(card);

                if (success)
                {
                    ApplyCardEffect(card);
                    Log($"{card.displayName} 성공! (싱크율 {score:0.0}%)");

                    if (_selectedEnemyIndex >= 0 && enemySlots[_selectedEnemyIndex].Health.IsDead)
                        _selectedEnemyIndex = FindFirstAliveIndex();
                }
                else
                {
                    Log($"{card.displayName} 실패... (싱크율 {score:0.0}%)");
                }

                CheckCombatEnd();
                RefreshUI();
            }, card.displayName);
        }

        private void ApplyCardEffect(CardData card)
        {
            switch (card.cardType)
            {
                case CardType.Attack:
                    if (_selectedEnemyIndex >= 0)
                    {
                        EnemySlotView target = enemySlots[_selectedEnemyIndex];
                        int hits = Mathf.Max(1, card.hitCount);
                        for (int i = 0; i < hits && !target.Health.IsDead; i++)
                            DealDamage(target, card);
                    }
                    break;
                case CardType.AttackAll:
                    foreach (EnemySlotView slot in enemySlots)
                    {
                        if (slot.Data != null && !slot.Health.IsDead)
                            DealDamage(slot, card);
                    }
                    break;
                case CardType.Defense:
                    playerHealth.AddBlock(card.effectValue);
                    break;
                case CardType.Weaken:
                    if (_selectedEnemyIndex >= 0)
                        enemySlots[_selectedEnemyIndex].ApplyWeaken(StatusEffectDuration);
                    break;
                case CardType.Enfeeble:
                    if (_selectedEnemyIndex >= 0)
                        enemySlots[_selectedEnemyIndex].ApplyEnfeeble(StatusEffectDuration);
                    break;
                case CardType.Strength:
                    _playerStrength += card.effectValue;
                    _playerStrengthTurns = Mathf.Max(_playerStrengthTurns, StatusEffectDuration);
                    break;
                case CardType.Draw:
                    _deck.DrawCards(card.effectValue);
                    break;
                case CardType.Poison:
                    if (_selectedEnemyIndex >= 0)
                        enemySlots[_selectedEnemyIndex].ApplyPoison(card.effectValue);
                    break;
                case CardType.PoisonAura:
                    _noxiousFumesPoisonPerTurn += card.effectValue;
                    break;
                case CardType.PoisonBounce:
                    ApplyPoisonBounce(card);
                    break;
                case CardType.PoisonMultiply:
                    if (_selectedEnemyIndex >= 0)
                        enemySlots[_selectedEnemyIndex].MultiplyPoison(2);
                    break;
                case CardType.PoisonExplode:
                    if (_selectedEnemyIndex >= 0)
                    {
                        EnemySlotView target = enemySlots[_selectedEnemyIndex];
                        target.ApplyPoison(card.effectValue);
                        target.ApplyCorpseExplosion();
                    }
                    break;
                case CardType.Retain:
                    _retainCount += card.effectValue;
                    break;
                case CardType.Thorns:
                    _playerThorns += card.effectValue;
                    _playerThornsTurns = Mathf.Max(_playerThornsTurns, StatusEffectDuration);
                    break;
                case CardType.Barricade:
                    _blockPersists = true;
                    break;
                case CardType.BlockMultiply:
                    playerHealth.MultiplyBlock(2);
                    break;
                case CardType.DefendAndWeaken:
                    playerHealth.AddBlock(card.effectValue);
                    if (_selectedEnemyIndex >= 0)
                        enemySlots[_selectedEnemyIndex].ApplyWeaken(StatusEffectDuration);
                    break;
                case CardType.DefendAndAttack:
                    playerHealth.AddBlock(card.effectValue);
                    if (_selectedEnemyIndex >= 0)
                        DealDamage(enemySlots[_selectedEnemyIndex], card);
                    break;
                case CardType.DefendAndEnfeeble:
                    playerHealth.AddBlock(card.effectValue);
                    if (_selectedEnemyIndex >= 0)
                        enemySlots[_selectedEnemyIndex].ApplyEnfeeble(StatusEffectDuration);
                    break;
                case CardType.DefendPerEnemy:
                    playerHealth.AddBlock(card.effectValue * CountAliveEnemies());
                    break;
                case CardType.Lifesteal:
                    _playerLifestealPercent = card.effectValue;
                    break;
            }
        }

        // Applies one hit of `card` to `target`: Strength adds flat bonus damage, Weaken multiplies
        // the total (rounded down), and ignoresBlock skips the target's Block absorption.
        private void DealDamage(EnemySlotView target, CardData card)
        {
            int damage = card.effectValue + _playerStrength;
            if (target.HasWeaken)
                damage += Mathf.FloorToInt(damage * WeakenDamageMultiplier);

            int actualDamage = DamageEnemy(target, damage, card.ignoresBlock);

            if (_playerLifestealPercent > 0 && actualDamage > 0)
            {
                int healed = Mathf.FloorToInt(actualDamage * _playerLifestealPercent / 100f);
                if (healed > 0) playerHealth.Heal(healed);
            }
        }

        // Applies a random-target Poison hit `card.hitCount` times (Bouncing Flask); each hit can
        // land on the same enemy again since it's picked fresh from the currently-alive set.
        private void ApplyPoisonBounce(CardData card)
        {
            var aliveIndices = new List<int>();
            for (int i = 0; i < enemySlots.Length; i++)
                if (enemySlots[i].Data != null && !enemySlots[i].Health.IsDead)
                    aliveIndices.Add(i);
            if (aliveIndices.Count == 0) return;

            int hits = Mathf.Max(1, card.hitCount);
            for (int i = 0; i < hits; i++)
            {
                int randomIndex = aliveIndices[UnityEngine.Random.Range(0, aliveIndices.Count)];
                enemySlots[randomIndex].ApplyPoison(card.effectValue);
            }
        }

        // Single funnel for all enemy-facing damage (cards and Poison ticks alike) so gold-on-kill,
        // the hit flash, and Corpse Explosion all trigger the same way no matter what caused it.
        // Returns how much HP the target actually lost (post-Block), e.g. for Lifesteal - 0 if the
        // target was already dead or the hit was negated.
        private int DamageEnemy(EnemySlotView target, int amount, bool ignoreBlock)
        {
            if (target.Health.IsDead) return 0;

            if (target.ConsumeDamageNegation())
            {
                Log($"{target.Data.displayName}의 피해가 무효화되었습니다!");
                return 0;
            }

            EnemyData data = target.Data;
            int hpBefore = target.Health.CurrentHP;
            target.Health.TakeDamage(amount, ignoreBlock);
            int actualDamage = hpBefore - target.Health.CurrentHP;
            target.PlayHitFlash();

            if (target.Health.IsDead)
            {
                OnGoldEarned?.Invoke(data.goldReward);
                if (target.HasCorpseExplosion)
                    TriggerCorpseExplosion(target);
                target.Hide();
            }

            return actualDamage;
        }

        // Corpse Explosion: the dead enemy's max HP is dealt to every other still-alive enemy. Can
        // chain if one of those also has Corpse Explosion and dies from the blast.
        private void TriggerCorpseExplosion(EnemySlotView deadEnemy)
        {
            int explosionDamage = deadEnemy.Health.MaxHP;
            foreach (EnemySlotView slot in enemySlots)
            {
                if (slot == deadEnemy || slot.Data == null || slot.Health.IsDead) continue;
                DamageEnemy(slot, explosionDamage, false);
            }
        }

        // Used by the shop/bonfire screens between battles - playerHealth persists for the whole run.
        public void HealPlayer(int amount)
        {
            playerHealth.Heal(amount);
        }

        private void EndPlayerTurn()
        {
            if (_awaitingChallenge || _combatOver) return;

            _deck.DiscardHand(_retainCount);
            Log("적의 턴");
            StartCoroutine(EnemyTurnRoutine());
        }

        // Enemies act one at a time (with a short pause between each) instead of all their damage
        // landing in the same frame, so the attack-punch animation and HP drops are each visible.
        private IEnumerator EnemyTurnRoutine()
        {
            // Pre-scan: if any living enemy's next own action is Split (and there's room for the
            // resulting clone), the whole group's plain Attack actions are suspended this round -
            // only the one Split resolves. See ResolveSplit/HasEmptyEnemySlot.
            bool anySplitPending = false;
            foreach (var scanSlot in enemySlots)
            {
                if (scanSlot.Data == null || scanSlot.Health.IsDead || scanSlot.SkipThisTurn) continue;
                if (scanSlot.PeekNextPatternAction() == EnemyTurnAction.Split && HasEmptyEnemySlot())
                    anySplitPending = true;
            }
            bool splitResolvedThisRound = false;

            foreach (var slot in enemySlots)
            {
                if (slot.Data == null || slot.Health.IsDead) continue;

                if (slot.SkipThisTurn)
                {
                    slot.SkipThisTurn = false;
                    continue;
                }

                if (slot.PoisonStacks > 0)
                {
                    DamageEnemy(slot, slot.PoisonStacks, true);
                    slot.TickPoison();
                    if (slot.Health.IsDead) continue;
                }

                EnemyTurnAction peeked = slot.PeekNextPatternAction();
                bool resolvesAsSplit = peeked == EnemyTurnAction.Split && HasEmptyEnemySlot();

                if (resolvesAsSplit)
                {
                    slot.ConsumeNextPatternAction();
                    if (!splitResolvedThisRound)
                    {
                        ResolveSplit(slot.Data);
                        splitResolvedThisRound = true;
                        Log($"{slot.Data.displayName}이(가) 분열합니다!");
                    }
                }
                else if (peeked == EnemyTurnAction.Attack || peeked == EnemyTurnAction.Split)
                {
                    // peeked == Split here only when the population cap left no room to clone into -
                    // it falls back to a plain attack instead of doing nothing.
                    if (anySplitPending)
                    {
                        Log($"{slot.Data.displayName}이(가) 다른 개체의 분열로 인해 이번 턴 공격을 멈춥니다");
                    }
                    else
                    {
                        slot.ConsumeNextPatternAction();
                        int damage = slot.Data.attackDamage;
                        if (slot.HasEnfeeble)
                            damage -= Mathf.FloorToInt(damage * EnfeebleDamageMultiplier);

                        playerHealth.TakeDamage(damage);
                        slot.PlayAttackPunch();
                        Log($"{slot.Data.displayName}이(가) {damage} 데미지를 입혔습니다");

                        if (_playerThorns > 0)
                            DamageEnemy(slot, _playerThorns, false);
                    }
                }
                else
                {
                    EnemyTurnAction action = slot.ConsumeNextPatternAction();
                    switch (action)
                    {
                        case EnemyTurnAction.ShieldNegate:
                            slot.AddDamageNegation(1);
                            Log($"{slot.Data.displayName}이(가) 다음 피해를 무효화할 준비를 합니다");
                            break;
                        case EnemyTurnAction.Summon:
                            SummonEnemies(slot.Data.summonEnemyData, slot.Data.summonCount);
                            Log($"{slot.Data.displayName}이(가) 지원군을 소환했습니다!");
                            break;
                        case EnemyTurnAction.Defend:
                            slot.Health.AddBlock(slot.Data.defendAmount);
                            Log($"{slot.Data.displayName}이(가) 방어 태세를 취합니다 (+{slot.Data.defendAmount})");
                            break;
                        case EnemyTurnAction.AttackCharge:
                            // Deals whatever Block this enemy currently has left (player attacks against
                            // it in the meantime reduce this), then clears that Block to 0.
                            int chargedDamage = slot.Health.Block;
                            slot.Health.ClearBlock();
                            if (slot.HasEnfeeble)
                                chargedDamage -= Mathf.FloorToInt(chargedDamage * EnfeebleDamageMultiplier);

                            playerHealth.TakeDamage(chargedDamage);
                            slot.PlayAttackPunch();
                            Log($"{slot.Data.displayName}이(가) 축적된 힘으로 {chargedDamage} 데미지를 입혔습니다!");

                            if (_playerThorns > 0)
                                DamageEnemy(slot, _playerThorns, false);
                            break;
                    }
                }

                slot.TickStatusEffects();
                RefreshPlayerStatusUI();

                yield return new WaitForSeconds(EnemyAttackDelay);
            }

            if (_playerStrengthTurns > 0 && --_playerStrengthTurns == 0)
                _playerStrength = 0;
            if (_playerThornsTurns > 0 && --_playerThornsTurns == 0)
                _playerThorns = 0;

            CheckCombatEnd();
            RefreshUI();

            if (!_combatOver)
            {
                if (_selectedEnemyIndex < 0 || enemySlots[_selectedEnemyIndex].Health.IsDead)
                    _selectedEnemyIndex = FindFirstAliveIndex();
                StartPlayerTurn();
            }
        }

        private bool AllEnemiesDead()
        {
            foreach (var slot in enemySlots)
                if (slot.Data != null && !slot.Health.IsDead) return false;
            return true;
        }

        private int CountAliveEnemies()
        {
            int count = 0;
            foreach (var slot in enemySlots)
                if (slot.Data != null && !slot.Health.IsDead) count++;
            return count;
        }

        // Fills up to `count` empty enemy slots with fresh copies of `data` (e.g. Undead Commander's
        // Summon action). Summoned enemies don't act until their next own turn (see SkipThisTurn).
        private void SummonEnemies(EnemyData data, int count)
        {
            if (data == null) return;

            int summoned = 0;
            foreach (var slot in enemySlots)
            {
                if (summoned >= count) break;
                if (slot.Data != null && !slot.Health.IsDead) continue;

                slot.Show(data);
                slot.SkipThisTurn = true;
                summoned++;
            }
        }

        private bool HasEmptyEnemySlot()
        {
            foreach (var slot in enemySlots)
                if (slot.Data == null || slot.Health.IsDead) return true;
            return false;
        }

        // Split: finds the highest-current-HP living instance of `species` (ties broken by the
        // rightmost/highest-index slot), halves its HP, and fills the first empty slot with a fresh
        // clone carrying that same halved HP. The clone doesn't act the round it's created.
        private void ResolveSplit(EnemyData species)
        {
            EnemySlotView target = null;
            foreach (var slot in enemySlots)
            {
                if (slot.Data != species || slot.Health.IsDead) continue;
                if (target == null || slot.Health.CurrentHP >= target.Health.CurrentHP)
                    target = slot;
            }
            if (target == null) return;

            int halved = Mathf.Max(1, target.Health.CurrentHP / 2);
            target.Health.SetCurrentHP(halved);

            foreach (var slot in enemySlots)
            {
                if (slot.Data != null && !slot.Health.IsDead) continue;
                slot.Show(species);
                slot.Health.SetCurrentHP(halved);
                slot.SkipThisTurn = true;
                break;
            }
        }

        private void CheckCombatEnd()
        {
            if (AllEnemiesDead())
            {
                _combatOver = true;
                resultText.text = "승리!";
                OnCombatEnded?.Invoke(true);
            }
            else if (playerHealth.IsDead)
            {
                _combatOver = true;
                resultText.text = "패배...";
                OnCombatEnded?.Invoke(false);
            }
        }

        private void RefreshEnemyUI()
        {
            for (int i = 0; i < enemySlots.Length; i++)
                enemySlots[i].SetSelected(i == _selectedEnemyIndex);

            if (monsterInfoText != null)
            {
                monsterInfoText.text = _selectedEnemyIndex >= 0
                    ? enemySlots[_selectedEnemyIndex].GetStatusText()
                    : string.Empty;
            }
        }

        // Just the player's HP/buff-debuff readout - called after each individual enemy attack too,
        // so damage and status changes are visible one at a time rather than only at turn's end.
        private void RefreshPlayerStatusUI()
        {
            playerHPText.text = $"HP {playerHealth.CurrentHP}/{playerHealth.MaxHP} (방어 {playerHealth.Block})";

            if (buffDebuffText != null)
            {
                var parts = new List<string>();
                if (_playerStrength > 0) parts.Add($"힘 +{_playerStrength} ({_playerStrengthTurns}턴)");
                if (_playerThorns > 0) parts.Add($"가시 +{_playerThorns} ({_playerThornsTurns}턴)");
                if (_blockPersists) parts.Add("방어 유지");
                buffDebuffText.text = string.Join("   ", parts);
            }
        }

        private void RefreshUI()
        {
            RefreshPlayerStatusUI();
            RefreshEnemyUI();
            energyText.text = $"에너지 {_currentEnergy}/{startingEnergy}";
            if (deckCountText != null)
                deckCountText.text = $"덱 {_deck.DrawPile.Count} | 버림 {_deck.DiscardPile.Count}";
            handView.Render(_deck.Hand, _currentEnergy, OnCardClicked);
            endTurnButton.interactable = !_awaitingChallenge && !_combatOver;
        }

        private void Log(string message)
        {
            if (logText != null) logText.text = message;
        }
    }
}

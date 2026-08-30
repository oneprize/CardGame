using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using CardGame.Cards;
using CardGame.UI;

namespace CardGame.Combat
{
    // Drives the linear 20-stage run (two 10-stage chapters): main menu -> battle -> reward ->
    // next stage -> ... -> game over / game clear -> restart. Deck and player HP persist across
    // the whole run (only block resets each battle). Stage 5 and 10 of each chapter are configured
    // as solo boss fights via their StageData; multi-enemy stages use the weaker enemy tiers so a
    // 3-enemy fight isn't tougher overall than a single strong enemy.
    public class RunManager : MonoBehaviour
    {
        // 0-based indices into `stages` for the two special screens. The shop opens right after
        // the reward pick for each chapter's mid-boss and chapter-boss stages; the bonfire opens
        // right before each chapter's final boss stage starts.
        private static readonly int[] ShopAfterStageIndices = { 4, 9, 14 };   // after "1-5", "1-10", "2-5"
        private static readonly int[] BonfireBeforeStageIndices = { 9, 19 }; // before "1-10", "2-10"

        [SerializeField] private CombatController combatController;
        [SerializeField] private RewardController rewardController;
        [SerializeField] private ShopController shopController;
        [SerializeField] private BonfireController bonfireController;

        [Header("Screens")]
        [SerializeField] private GameObject mainMenuRoot;
        [SerializeField] private Button startButton;
        [SerializeField] private GameObject gameOverRoot;
        [SerializeField] private Button gameOverRestartButton;
        [SerializeField] private GameObject gameClearRoot;
        [SerializeField] private Button gameClearRestartButton;
        [SerializeField] private CardGame.UI.MenuPanelController pauseMenu;

        [Header("Deck")]
        [SerializeField] private List<CardData> startingDeck;
        [SerializeField] private List<CardData> cardPool;
        [SerializeField] private List<StageData> stages;
        [SerializeField] private int rewardOptionCount = 3;

        [Header("Gold")]
        [SerializeField] private Text goldText;

        private List<CardData> _deck;
        private int _stageIndex;
        private int _gold;

        public IReadOnlyList<CardData> CurrentDeck => _deck;

        // 0-based index of the stage currently in progress, -1 before the run has started.
        public int CurrentStageIndex => _stageIndex - 1;

        private void Awake()
        {
            combatController.OnCombatEnded += HandleCombatEnded;
            combatController.OnGoldEarned += AddGold;
            startButton.onClick.AddListener(BeginNewRun);
            gameOverRestartButton.onClick.AddListener(BeginNewRun);
            gameClearRestartButton.onClick.AddListener(BeginNewRun);
            if (pauseMenu != null) pauseMenu.OnRestartRequested += HandlePauseMenuRestart;
        }

        private void OnDestroy()
        {
            combatController.OnCombatEnded -= HandleCombatEnded;
            combatController.OnGoldEarned -= AddGold;
            if (pauseMenu != null) pauseMenu.OnRestartRequested -= HandlePauseMenuRestart;
        }

        private void HandlePauseMenuRestart()
        {
            pauseMenu.Close();
            BeginNewRun();
        }

        private void Start()
        {
            if (mainMenuRoot != null) mainMenuRoot.SetActive(true);
            if (gameOverRoot != null) gameOverRoot.SetActive(false);
            if (gameClearRoot != null) gameClearRoot.SetActive(false);
        }

        private void BeginNewRun()
        {
            if (mainMenuRoot != null) mainMenuRoot.SetActive(false);
            if (gameOverRoot != null) gameOverRoot.SetActive(false);
            if (gameClearRoot != null) gameClearRoot.SetActive(false);

            _deck = new List<CardData>(startingDeck);
            _stageIndex = 0;
            _gold = 0;
            RefreshGoldText();
            combatController.BeginRun();
            StartNextStage();
        }

        private void AddGold(int amount)
        {
            _gold += amount;
            RefreshGoldText();
        }

        private void SpendGold(int amount)
        {
            _gold -= amount;
            RefreshGoldText();
        }

        private void RefreshGoldText()
        {
            if (goldText != null) goldText.text = $"골드 {_gold}";
        }

        private void StartNextStage()
        {
            if (_stageIndex >= stages.Count)
            {
                if (gameClearRoot != null) gameClearRoot.SetActive(true);
                return;
            }

            StageData stage = stages[_stageIndex];
            combatController.StartBattle(_deck, stage);
            _stageIndex++;
        }

        private void HandleCombatEnded(bool playerWon)
        {
            if (!playerWon)
            {
                if (gameOverRoot != null) gameOverRoot.SetActive(true);
                return;
            }

            if (_stageIndex >= stages.Count)
            {
                if (gameClearRoot != null) gameClearRoot.SetActive(true);
                return;
            }

            int clearedStageIndex = _stageIndex - 1;
            rewardController.ShowReward(cardPool, rewardOptionCount, chosenCard =>
            {
                if (chosenCard != null)
                    _deck.Add(chosenCard);

                AfterReward(clearedStageIndex);
            });
        }

        // Routes to the shop/bonfire between-stage screens when the stage just cleared or the
        // stage about to start calls for one; otherwise goes straight to the next battle.
        private void AfterReward(int clearedStageIndex)
        {
            if (System.Array.IndexOf(ShopAfterStageIndices, clearedStageIndex) >= 0)
            {
                shopController.Open(cardPool, () => _gold, SpendGold, boughtCard => _deck.Add(boughtCard), StartNextStage);
            }
            else if (System.Array.IndexOf(BonfireBeforeStageIndices, _stageIndex) >= 0)
            {
                bonfireController.Open(StartNextStage);
            }
            else
            {
                StartNextStage();
            }
        }
    }
}

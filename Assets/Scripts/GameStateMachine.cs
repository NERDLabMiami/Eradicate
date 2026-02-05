using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Eradicate
{
    public enum GamePhase
    {
        SelectActions,
        ResolvingActions,
        RoundEnd,
        GameOver
    }

    [Serializable]
    public class HumanState
    {
        public HumanColor color;
        public int blood;

        [NonSerialized] public bool protectedThisRound;

        public bool IsActive => blood > 0;
    }

    [Serializable]
    public class BreedingGroundState
    {
        public BreedingGroundType type;
        public int eggs;

        public bool IsActive => eggs > 0; // 0 means eradicated
    }

    public class GameStateMachine : MonoBehaviour
    {
        [Header("Defs In Play (Option B)")]
        [SerializeField] private List<HumanDefSO> humansInPlay = new();
        [SerializeField] private List<BreedingGroundDefSO> groundsInPlay = new();

        [Header("Board Spawning")]
        [SerializeField] private Transform humansContainer;
        [SerializeField] private Transform breedingGroundsContainer;

        [SerializeField] private HumanCardView humanCardPrefab;
        [SerializeField] private BreedingGroundCardView breedingGroundCardPrefab;

        [Header("Token Prefabs (UI)")]
        [SerializeField] private GameObject bloodTokenPrefab;
        [SerializeField] private GameObject eggTokenPrefab;
      
        [Header("Mosquito (Prototype)")]
        [SerializeField] private bool enableMosquitoPhase = true;
        [SerializeField] private int bitesPerActiveBreedingGround = 1;
        [SerializeField] private int breedsPerActiveBreedingGround = 1;

        private int _bittenPool = 0; // available tokens for breeding this round

        [Header("Fallback Defaults")]
        [SerializeField] private int fallbackStartingBloodPerHuman = 3;

        [Header("UI")]
        [SerializeField] private Button confirmButton;
        [SerializeField] private TextMeshProUGUI actionsLabel; // optional

        public GamePhase Phase { get; private set; } = GamePhase.SelectActions;

        // Authoritative state (only what’s in play)
        private readonly Dictionary<HumanColor, HumanState> _humans = new();
        private readonly Dictionary<BreedingGroundType, BreedingGroundState> _grounds = new();
        // scratch lists to avoid allocs
        private readonly List<HumanColor> _tmpHumans = new();
        private readonly List<BreedingGroundType> _tmpGrounds = new();
        private readonly HashSet<BreedingGroundType> _clearedThisRound = new();

        // Per-round action budget
        public int ActionsAvailable { get; private set; }
        public int ActionsTaken { get; private set; }

        // “Intents” recorded during selection; applied on confirm
        private readonly HashSet<HumanColor> _protectIntents = new();
        private readonly HashSet<BreedingGroundType> _clearIntents = new();

        // Spawn tracking
        private readonly List<GameObject> _spawnedBoardObjects = new();

        public event Action OnStateChanged;

        private void Awake()
        {
            if (confirmButton != null)
                confirmButton.onClick.AddListener(OnConfirmPressed);
        }

        private void Start()
        {
            StartNewGame();
        }

        public void StartNewGame()
        {
            BuildInitialStateFromDefs();
            SpawnBoardFromDefs();
            BeginSelectActionsPhase();
            Broadcast();
        }

        private void BuildInitialStateFromDefs()
        {
            _humans.Clear();
            for (int i = 0; i < humansInPlay.Count; i++)
            {
                var def = humansInPlay[i];
                if (def == null) continue;

                _humans[def.color] = new HumanState
                {
                    color = def.color,
                    blood = fallbackStartingBloodPerHuman
                };
            }

            _grounds.Clear();
            for (int i = 0; i < groundsInPlay.Count; i++)
            {
                var def = groundsInPlay[i];
                if (def == null) continue;

                _grounds[def.type] = new BreedingGroundState
                {
                    type = def.type,
                    eggs = Mathf.Max(0, def.startingEggs)
                };
            }
        }

        private void SpawnBoardFromDefs()
        {
            // Clear old board
            for (int i = 0; i < _spawnedBoardObjects.Count; i++)
                if (_spawnedBoardObjects[i] != null) Destroy(_spawnedBoardObjects[i]);
            _spawnedBoardObjects.Clear();

            // Humans
            if (humanCardPrefab != null && humansContainer != null)
            {
                for (int i = 0; i < humansInPlay.Count; i++)
                {
                    var def = humansInPlay[i];
                    if (def == null) continue;

                    var view = Instantiate(humanCardPrefab, humansContainer);
                    view.Init(this, def, bloodTokenPrefab);
                    _spawnedBoardObjects.Add(view.gameObject);
                }
            }

            // Breeding grounds
            if (breedingGroundCardPrefab != null && breedingGroundsContainer != null)
            {
                for (int i = 0; i < groundsInPlay.Count; i++)
                {
                    var def = groundsInPlay[i];
                    if (def == null) continue;

                    var view = Instantiate(breedingGroundCardPrefab, breedingGroundsContainer);
                    view.Init(this, def, eggTokenPrefab);
                    _spawnedBoardObjects.Add(view.gameObject);
                }
            }
        }

        private void BeginSelectActionsPhase()
        {
            _clearedThisRound.Clear();
            Phase = GamePhase.SelectActions;

            foreach (var h in _humans.Values)
                h.protectedThisRound = false;

            _protectIntents.Clear();
            _clearIntents.Clear();

            ActionsTaken = 0;
            ActionsAvailable = CountActiveHumans();

            RefreshConfirmInteractivity();
            UpdateActionsLabel();
        }

        // ----------------------------
        // INPUT API (called by card views)
        // ----------------------------

        public bool TryToggleHuman(HumanColor color)
        {
            if (Phase != GamePhase.SelectActions) return false;

            if (!_humans.TryGetValue(color, out var h) || !h.IsActive)
                return false;

            // Toggle OFF
            if (_protectIntents.Remove(color))
            {
                ActionsTaken = Mathf.Max(0, ActionsTaken - 1);
                Broadcast();
                return true;
            }

            // Toggle ON (only if budget remains)
            if (ActionsTaken >= ActionsAvailable) return false;

            _protectIntents.Add(color);
            ActionsTaken++;

            Broadcast();
            return true;
        }

        public bool TryToggleBreedingGround(BreedingGroundType type)
        {
            if (Phase != GamePhase.SelectActions) return false;

            if (!_grounds.TryGetValue(type, out var bg) || !bg.IsActive)
                return false;

            // Toggle OFF
            if (_clearIntents.Remove(type))
            {
                ActionsTaken = Mathf.Max(0, ActionsTaken - 1);
                Broadcast();
                return true;
            }

            // Toggle ON (only if budget remains)
            if (ActionsTaken >= ActionsAvailable) return false;

            _clearIntents.Add(type);
            ActionsTaken++;

            Broadcast();
            return true;
        }

        // ----------------------------
        // CONFIRM / RESOLUTION
        // ----------------------------

        private void OnConfirmPressed()
        {
            if (Phase != GamePhase.SelectActions) return;
            if (ActionsTaken != ActionsAvailable) return;

            Phase = GamePhase.ResolvingActions;

            // Apply protection intents
            foreach (var c in _protectIntents)
            {
                if (_humans.TryGetValue(c, out var h) && h.IsActive)
                    h.protectedThisRound = true;
            }
            _clearedThisRound.Clear();
            // Apply clear intents (ONE egg per selected ground)
            foreach (var g in _clearIntents)
            {
                if (_grounds.TryGetValue(g, out var bg) && bg.IsActive)
                {
                    bg.eggs = Mathf.Max(0, bg.eggs - 1);
                    _clearedThisRound.Add(g);
                }
            }

            // --- MOSQUITO PHASE (prototype) ---
            if (enableMosquitoPhase)
                ResolveMosquitoPhase();

            // Win/Lose checks
            bool allGroundsEradicated = true;
            foreach (var bg in _grounds.Values)
            {
                if (bg.IsActive) { allGroundsEradicated = false; break; }
            }

            bool anyHumanActive = false;
            foreach (var h in _humans.Values)
            {
                if (h.IsActive) { anyHumanActive = true; break; }
            }

            if (allGroundsEradicated || !anyHumanActive)
            {
                Phase = GamePhase.GameOver;
            }
            else
            {
                BeginSelectActionsPhase();
            }

            Broadcast();
        }
        private void ResolveMosquitoPhase()
        {
            _bittenPool = 0;

            int activeGrounds = CountActiveGrounds();
            if (activeGrounds <= 0) return;

            int mosquitoActions = activeGrounds; // 1 per ground, like the analog game

            for (int i = 0; i < mosquitoActions; i++)
            {
                // If we have no bite tokens available, breeding is impossible, so force Bite.
                bool canBreed = _bittenPool > 0;

                // Tune this probability later to match your deck composition.
                // Start at 50/50: half bite, half breed (breed only if canBreed).
                bool doBreed = canBreed && UnityEngine.Random.value < 0.5f;

                if (doBreed)
                {
                    var target = PickRandomBreedTarget(_clearedThisRound);
                    if (target == null) continue;

                    _grounds[target.Value].eggs += 1;
                    _bittenPool--;
                }
                else
                {
                    var targetColor = PickRandomActiveHuman();
                    if (targetColor == null) continue;

                    var h = _humans[targetColor.Value];

                    if (h.protectedThisRound) continue;

                    h.blood = Mathf.Max(0, h.blood - 1);
                    _bittenPool++;
                }
            }
        }

        private int CountActiveGrounds()
        {
            int count = 0;
            foreach (var bg in _grounds.Values)
                if (bg.IsActive) count++;
            return count;
        }

        private HumanColor? PickRandomActiveHuman()
        {
            // collect active humans
            _tmpHumans.Clear();
            foreach (var kvp in _humans)
                if (kvp.Value.IsActive) _tmpHumans.Add(kvp.Key);

            if (_tmpHumans.Count == 0) return null;
            return _tmpHumans[UnityEngine.Random.Range(0, _tmpHumans.Count)];
        }

        private BreedingGroundType? PickRandomBreedTarget(HashSet<BreedingGroundType> exclude)
        {
            _tmpGrounds.Clear();
            foreach (var kvp in _grounds)
            {
                if (!kvp.Value.IsActive) continue;
                if (exclude != null && exclude.Contains(kvp.Key)) continue;
                _tmpGrounds.Add(kvp.Key);
            }

            if (_tmpGrounds.Count == 0) return null;
            return _tmpGrounds[UnityEngine.Random.Range(0, _tmpGrounds.Count)];
        }


        // ----------------------------
        // HELPERS
        // ----------------------------

        private int CountActiveHumans()
        {
            int count = 0;
            foreach (var h in _humans.Values)
                if (h.IsActive) count++;
            return count;
        }

        private void RefreshConfirmInteractivity()
        {
            if (confirmButton == null) return;

            bool canConfirm =
                Phase == GamePhase.SelectActions &&
                ActionsAvailable > 0 &&
                ActionsTaken == ActionsAvailable;

            confirmButton.interactable = canConfirm;
        }

        private void UpdateActionsLabel()
        {
            if (actionsLabel == null) return;
            actionsLabel.text = $"Actions: {ActionsTaken}/{ActionsAvailable}";
        }

        private void Broadcast()
        {
            RefreshConfirmInteractivity();
            UpdateActionsLabel();
            OnStateChanged?.Invoke();
        }

        // ----------------------------
        // READ-ONLY ACCESSORS (Views)
        // ----------------------------

        public HumanState GetHuman(HumanColor c)
        {
            _humans.TryGetValue(c, out var h);
            return h;
        }

        public BreedingGroundState GetGround(BreedingGroundType g)
        {
            _grounds.TryGetValue(g, out var bg);
            return bg;
        }

        public bool HasProtectIntent(HumanColor c) => _protectIntents.Contains(c);
        public bool HasClearIntent(BreedingGroundType g) => _clearIntents.Contains(g);
    }
}

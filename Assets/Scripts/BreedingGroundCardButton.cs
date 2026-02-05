using UnityEngine;
using UnityEngine.UI;

namespace Eradicate
{
    [RequireComponent(typeof(Button))]
    public class BreedingGroundCardButton : MonoBehaviour
    {
        [SerializeField] private BreedingGroundType groundType;
        [SerializeField] private GameStateMachine gameStateMachine;

        [Header("Visual")]
        [Tooltip("If set, this graphic will be tinted. If null, uses the Button's ColorBlock.")]
        [SerializeField] private Graphic tintTarget;

        [Tooltip("Tint when this breeding ground is selected for Clear this round.")]
        [SerializeField] private Color selectedTint = new Color(0.6f, 0.9f, 0.6f, 1f);

        [Tooltip("Tint when not selected.")]
        [SerializeField] private Color normalTint = Color.white;

        private Button _button;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _button.onClick.AddListener(OnClicked);
        }

        private void OnEnable()
        {
            if (gameStateMachine != null)
                gameStateMachine.OnStateChanged += RefreshVisuals;

            RefreshVisuals();
        }

        private void OnDisable()
        {
            if (gameStateMachine != null)
                gameStateMachine.OnStateChanged -= RefreshVisuals;
        }

        private void OnClicked()
        {
            if (gameStateMachine == null) return;

            // GSM decides if the toggle is accepted; we always re-render from GSM truth.
            gameStateMachine.TryToggleBreedingGround(groundType);
            RefreshVisuals();
        }

        private void RefreshVisuals()
        {
            if (gameStateMachine == null) return;

            var bg = gameStateMachine.GetGround(groundType);

            // Not in play (or GSM not initialized yet)
            if (bg == null)
            {
                _button.interactable = false;
                ApplyTint(new Color(1f, 1f, 1f, 0.35f)); // dim
                return;
            }

            bool selected = gameStateMachine.HasClearIntent(groundType);
            bool groundActive = bg.IsActive;

            bool canInteract =
                gameStateMachine.Phase == GamePhase.SelectActions &&
                groundActive &&
                (selected || gameStateMachine.ActionsTaken < gameStateMachine.ActionsAvailable);

            _button.interactable = canInteract;
            ApplyTint(selected ? selectedTint : normalTint);
        }

        private void ApplyTint(Color c)
        {
            if (tintTarget != null)
            {
                tintTarget.color = c;
                return;
            }

            var cb = _button.colors;
            cb.normalColor = c;
            cb.selectedColor = c;
            cb.highlightedColor = c;
            _button.colors = cb;
        }
    }
}

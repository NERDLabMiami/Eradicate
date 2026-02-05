using UnityEngine;
using UnityEngine.UI;

namespace Eradicate
{
    [RequireComponent(typeof(Button))]
    public class HumanCardButton : MonoBehaviour
    {
        [SerializeField] private HumanColor color;
        [SerializeField] private GameStateMachine gameStateMachine;

        [Header("Visual")]
        [Tooltip("If set, this image will be tinted. If null, uses the Button's ColorBlock.")]
        [SerializeField] private Graphic tintTarget;

        [Tooltip("Tint when this human is selected for Protect this round.")]
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

            // IMPORTANT: we don't assume the click "worked".
            // GSM decides, then we refresh visuals based on GSM truth.
            gameStateMachine.TryToggleHuman(color);
            RefreshVisuals();
        }

        private void RefreshVisuals()
        {
            if (gameStateMachine == null) return;

            // 1) Selection truth comes ONLY from GSM
            bool selected = gameStateMachine.HasProtectIntent(color);

            // 2) Optional: disable button outside selection phase / if human inactive / if no budget left
            var hs = gameStateMachine.GetHuman(color);
            bool canInteract =
                gameStateMachine.Phase == GamePhase.SelectActions &&
                hs != null && hs.IsActive &&
                // allow toggling OFF even if budget is full:
                (selected || gameStateMachine.ActionsTaken < gameStateMachine.ActionsAvailable);

            _button.interactable = canInteract;

            // 3) Apply tint
            ApplyTint(selected ? selectedTint : normalTint);
        }

        private void ApplyTint(Color c)
        {
            if (tintTarget != null)
            {
                tintTarget.color = c;
                return;
            }

            // Fallback: use Button's ColorBlock (tints the whole button target graphic)
            var cb = _button.colors;
            cb.normalColor = c;
            cb.selectedColor = c;
            cb.highlightedColor = c;
            _button.colors = cb;
        }
    }
}

using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Eradicate
{
    [RequireComponent(typeof(Button))]
    public class HumanCardView : MonoBehaviour
    {
        [Header("Wiring")]
        [SerializeField] private Image cardBackground;   // parent Image (tinted to human color)
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private Image iconImage;
        [SerializeField] private TokenStackView bloodStack;

        [Header("Colors")]
        [Tooltip("If you want the active background to come from the HumanDefSO, leave this unused.")]
        [SerializeField] private Color eliminatedBackground = Color.black;

        [Tooltip("Optional: subtle tint when selected for Protect.")]
        [SerializeField] private Color selectedTintMultiplier = new Color(0.9f, 1.1f, 0.9f, 1f);

        private Button _button;
        private GameStateMachine _gsm;
        private HumanDefSO _def;

        private string _originalTitle;
        private Color _activeBackground;

        private void Awake()
        {
            _button = GetComponent<Button>();
        }

        public void Init(GameStateMachine gsm, HumanDefSO def, GameObject bloodTokenPrefab)
        {
            _gsm = gsm;
            _def = def;

            // Title can just be the color name, or you can add a displayName field to HumanDefSO later.
            _originalTitle = def != null ? def.color.ToString() : "Human";

            if (titleText != null)
                titleText.text = _originalTitle;

            // Active background comes from the def (recommended)
            _activeBackground = (def != null) ? def.cardColor : Color.white;
            if (cardBackground != null)
                cardBackground.color = _activeBackground;

            if (iconImage != null)
                iconImage.sprite = def != null ? def.humanIcon : null;

            if (bloodStack != null)
                bloodStack.SetTokenPrefab(bloodTokenPrefab);

            _button.onClick.RemoveAllListeners();
            _button.onClick.AddListener(() =>
            {
                if (_gsm != null && _def != null)
                    _gsm.TryToggleHuman(_def.color);
            });

            if (_gsm != null)
                _gsm.OnStateChanged += Refresh;

            Refresh();
        }

        private void OnDestroy()
        {
            if (_gsm != null)
                _gsm.OnStateChanged -= Refresh;
        }

        private void Refresh()
        {
            if (_gsm == null || _def == null) return;

            var hs = _gsm.GetHuman(_def.color);
            if (hs == null)
            {
                SetEliminatedVisuals(true);
                _button.interactable = false;
                return;
            }

            // Tokens-only
            if (bloodStack != null)
                bloodStack.SetCount(hs.blood);

            bool eliminated = !hs.IsActive;
            bool selected = _gsm.HasProtectIntent(_def.color);

            SetEliminatedVisuals(eliminated);

            if (!eliminated)
            {
                // allow toggling off even when budget is full
                bool canInteract =
                    _gsm.Phase == GamePhase.SelectActions &&
                    (selected || _gsm.ActionsTaken < _gsm.ActionsAvailable);

                _button.interactable = canInteract;

                // Selection tint (optional)
                if (cardBackground != null)
                {
                    if (selected)
                        cardBackground.color = Multiply(_activeBackground, selectedTintMultiplier);
                    else
                        cardBackground.color = _activeBackground;
                }
            }
            else
            {
                _button.interactable = false;
            }
        }

        private void SetEliminatedVisuals(bool eliminated)
        {
            if (cardBackground != null)
                cardBackground.color = eliminated ? eliminatedBackground : _activeBackground;

            if (titleText != null)
                titleText.text = eliminated ? "ELIMINATED" : _originalTitle;

            if (iconImage != null)
                iconImage.enabled = !eliminated;

            if (bloodStack != null)
                bloodStack.gameObject.SetActive(!eliminated);
        }

        private static Color Multiply(Color a, Color b)
        {
            return new Color(a.r * b.r, a.g * b.g, a.b * b.b, a.a * b.a);
        }
    }
}

using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Eradicate
{
    [RequireComponent(typeof(Button))]
    public class BreedingGroundCardView : MonoBehaviour
    {
        [Header("Wiring")]
        [SerializeField] private Image cardBackground;   // parent Image (white by default)
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private Image groundImage;
        [SerializeField] private TokenStackView eggStack;

        [Header("Colors")]
        [SerializeField] private Color activeBackground = Color.white;
        [SerializeField] private Color eradicatedBackground = Color.black;
        [SerializeField] private Color selectedTint = new Color(0.9f, 1f, 0.9f, 1f);

        private Button _button;
        private GameStateMachine _gsm;
        private BreedingGroundDefSO _def;

        private string _originalTitle;

        private void Awake()
        {
            _button = GetComponent<Button>();
        }

        public void Init(GameStateMachine gsm, BreedingGroundDefSO def, GameObject eggTokenPrefab)
        {
            _gsm = gsm;
            _def = def;

            _originalTitle = def.displayName;

            if (titleText != null)
                titleText.text = _originalTitle;

            if (groundImage != null)
                groundImage.sprite = def.groundImage;

            if (eggStack != null)
                eggStack.SetTokenPrefab(eggTokenPrefab);

            _button.onClick.RemoveAllListeners();
            _button.onClick.AddListener(() => _gsm.TryToggleBreedingGround(def.type));

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

            var bg = _gsm.GetGround(_def.type);
            if (bg == null)
            {
                SetEradicatedVisuals(true);
                _button.interactable = false;
                return;
            }

            bool eradicated = !bg.IsActive;
            bool selected = _gsm.HasClearIntent(_def.type);

            // Tokens-only count
            if (eggStack != null)
                eggStack.SetCount(bg.eggs);

            SetEradicatedVisuals(eradicated);

            if (!eradicated)
            {
                bool canInteract =
                    _gsm.Phase == GamePhase.SelectActions &&
                    (selected || _gsm.ActionsTaken < _gsm.ActionsAvailable);

                _button.interactable = canInteract;

                if (cardBackground != null)
                    cardBackground.color = selected ? selectedTint : activeBackground;
            }
            else
            {
                _button.interactable = false;
            }
        }

        private void SetEradicatedVisuals(bool eradicated)
        {
            if (cardBackground != null)
                cardBackground.color = eradicated ? eradicatedBackground : activeBackground;

            if (titleText != null)
                titleText.text = eradicated ? "ERADICATED" : _originalTitle;

            if (groundImage != null)
                groundImage.enabled = !eradicated;

            if (eggStack != null)
                eggStack.gameObject.SetActive(!eradicated);
        }
    }
}

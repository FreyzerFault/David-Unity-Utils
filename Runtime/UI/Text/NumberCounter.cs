using UnityEngine;

namespace DavidUtils.UI.Text
{
    public class NumberCounter : MonoBehaviour
    {
        [SerializeField] private string spritesPath = "Sprites/Symbols/Numbers/";

        [SerializeField] private float numberSpacing = .08f;

        private SpriteRenderer[] _numberRenderers;
        private Sprite[] _sprites;

        private int _num;
        public int Number
        {
            get => _num;
            set
            {
                _num = value;
                UpdateNumSprites();
            }
        }

        private void Awake()
        {
            _numberRenderers = GetComponentsInChildren<SpriteRenderer>();
            _sprites = new Sprite[10];
            for (var i = 0; i < 10; i++) _sprites[i] = Resources.Load<Sprite>(spritesPath + i);
        }

        private Sprite GetSpriteByDigit(int digit) => _sprites[digit];

        // Update the Number Sprites to show the num digits
        private void UpdateNumSprites()
        {
            var numS = Number.ToString();
            var numWithPadding = Number.ToString("000");

            // Set the sprites for each digit
            for (var i = 0; i < _numberRenderers.Length; i++)
                _numberRenderers[i].sprite = GetSpriteByDigit(numWithPadding[i]);

            // Disable leading 0s and enable the rest
            DisableLeadingDigits(numWithPadding);

            switch (numS.Length)
            {
                case 1:
                    _numberRenderers[2].transform.localPosition = Vector3.zero;
                    break;
                case 2:
                    _numberRenderers[1].transform.localPosition = Vector3.left * numberSpacing;
                    _numberRenderers[2].transform.localPosition = Vector3.right * numberSpacing;
                    break;
                case 3:
                    _numberRenderers[0].transform.localPosition =
                        Vector3.left * (numberSpacing * 2);
                    _numberRenderers[1].transform.localPosition = Vector3.zero;
                    _numberRenderers[2].transform.localPosition =
                        Vector3.right * (numberSpacing * 2);
                    break;
            }
        }

        // Disable leading 0s and enable the rest
        private void DisableLeadingDigits(string numS)
        {
            for (var i = 0; i < _numberRenderers.Length; i++)
                if (numS[i] == 0)
                {
                    _numberRenderers[i].enabled = false;
                }
                else
                {
                    for (var j = i + 1; j < _numberRenderers.Length; j++) _numberRenderers[i].enabled = true;
                    break;
                }
        }
    }
}
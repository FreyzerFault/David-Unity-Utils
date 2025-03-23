using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace DavidUtils.UI.Sliders
{
    public class DynamicBar : MonoBehaviour
    {
        public Gradient gradient = new();

        private Slider _slider;
        private Image _bar;
        private UnityEngine.UI.Text _text;

        public bool useGradient;
        public bool inverted;

        public float MaxValue
        {
            get => _slider.maxValue;
            set => _slider.maxValue = value;
        }

        public float Value
        {
            get => inverted ? _slider.maxValue - _slider.value : _slider.value;
            set
            {
                if (inverted)
                    _slider.value = MaxValue - value;
                else
                    _slider.value = value;

                // Cambia la barra de color con el gradiente
                if (useGradient) _bar.color = gradient.Evaluate(_slider.normalizedValue);

                // Cambia el texto
                if (_text)
                {
                    _text.text = value.ToString(CultureInfo.CurrentCulture);
                    _text.color = Color.Lerp(Color.red, Color.black, _slider.normalizedValue);
                }
            }
        }

        private void Awake()
        {
            _slider = GetComponent<Slider>();
            _bar = GetComponentInChildren<Image>();
            _text = GetComponentInChildren<UnityEngine.UI.Text>();

            if (inverted) _slider.value = 0;
        }
    }
}

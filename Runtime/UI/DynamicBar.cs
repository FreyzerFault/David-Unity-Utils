using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace DavidUtils.UI
{
	public class DynamicBar : MonoBehaviour
	{
		public Gradient gradient = new Gradient();
	
		private Slider slider;
		private Image bar;
		private Text text;

		public bool useGradient = false;
		public bool inverted = false;

		public float MaxValue
		{
			get => slider.maxValue; 
			set => slider.maxValue = value;
		}
	
		public float Value
		{
			get => inverted ? slider.maxValue - slider.value : slider.value;
			set
			{
				if (inverted)
				{
					slider.value = MaxValue - value;				
				}
				else
					slider.value = value;

				// Cambia la barra de color con el gradiente
				if (useGradient)
					bar.color = gradient.Evaluate(slider.normalizedValue);
		
				// Cambia el texto
				if (text)
				{
					text.text = value.ToString(CultureInfo.CurrentCulture);
					text.color = Color.Lerp(Color.red, Color.black, slider.normalizedValue);
				}
			}
		}

		void Awake()
		{
			slider = GetComponent<Slider>();
			bar = GetComponentInChildren<Image>();
			text = GetComponentInChildren<Text>();

			if (inverted)
				slider.value = 0;
		}
	}
}

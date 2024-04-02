using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace DavidUtils.UI
{
	public class DynamicIcon : MonoBehaviour
	{
		private Image _image;

		private void Awake()
		{
			_image = GetComponent<Image>();
		}

		protected IEnumerator Shake()
		{
			// Rotacion random bajo un maximo
			float rotation = ((Random.value - 0.5f) * 2) * 20;
			float scaling = Random.value / 2 + 1;

			transform.Rotate(Vector3.forward, rotation);
			transform.localScale *= scaling;

			yield return null;

			transform.localScale *= 1 / scaling;
			transform.Rotate(Vector3.forward, -rotation);
		}

		protected IEnumerator Beat()
		{
			const float scale = 1.2f;
			transform.localScale *= scale;
		
			yield return new WaitForSeconds(0.5f);

			transform.localScale /= scale;
		}

		protected void SwitchIcon(Sprite newSprite) => _image.sprite = newSprite;
	}
}

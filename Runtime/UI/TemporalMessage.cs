using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DavidUtils.UI
{
	public class TemporalMessage : MonoBehaviour
	{
		public Text text;
		private Image _background;

		// Start is called before the first frame update
		void Start()
		{
			_background = GetComponent<Image>();
		
			text.enabled = false;
		
			if (_background)
				_background.enabled = false;
		}

		public void ShowMessage(string msg, float seconds, bool resetGame = false)
		{
			text.enabled = true;
			_background.enabled = true;
			StartCoroutine(ShowMessageCoroutine(msg, seconds, resetGame));
		}

		private IEnumerator ShowMessageCoroutine(string msg, float seconds, bool resetGame = false)
		{
			text.text = msg;
			text.enabled = true;
			if (_background)
				_background.enabled = true;
			
			yield return new WaitForSeconds(seconds);

			text.enabled = false;
			_background.enabled = false;

			if (resetGame) SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
		}

	
	}
}

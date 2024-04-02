using UnityEngine;
using UnityEngine.SceneManagement;

namespace DavidUtils.Scenes
{
	public class SceneController : SingletonPersistent<SceneController>
	{
		public static void ReloadScene() => 
			LoadScene(SceneManager.GetActiveScene().buildIndex);

		public static void LoadScene(int i) => 
			SceneManager.LoadScene(i);

		public static void LoadScene(string sceneName) => 
			SceneManager.LoadScene(sceneName);

		public static void LoadByURL(string url)
		{
			Application.OpenURL(url);
		}
	}
}

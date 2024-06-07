using UnityEngine;

namespace DavidUtils
{
	// Only 1 Instance of object of type T can exist
	// If second one is created, it will destroy itself
	// Only the 1st one prevails
	// Awake MUST GO BEFORE ANY non-Singleton Awake
	public class Singleton<T> : MonoBehaviour
		where T : MonoBehaviour
	{
		public static T Instance { get; private set; }

		protected virtual void Awake()
		{
			var thisInstance = gameObject.GetComponent<T>();

			if (Instance != null && Instance != thisInstance)
			{
				if (Application.isPlaying)
					Destroy(gameObject);
				else
					DestroyImmediate(gameObject);
				return;
			}

			// First Initialization
			Instance = thisInstance;
		}
	}

	// Persist across scenes
	public class SingletonPersistent<T> : Singleton<T>
		where T : MonoBehaviour
	{
		protected override void Awake()
		{
			base.Awake();
			DontDestroyOnLoad(transform.parent == null ? gameObject : transform.root.gameObject);
		}
	}


	// ExecuteAlways runs it on both Edit and Play mode
	[ExecuteAlways]
	public class SingletonExecuteAlways<T> : Singleton<T>
		where T : MonoBehaviour
	{
	}
}

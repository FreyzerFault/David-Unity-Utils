using UnityEngine;

namespace DavidUtils
{
    public class Singleton<T> : MonoBehaviour
        where T : MonoBehaviour
    {
        public static T Instance { get; private set; }

        protected virtual void Awake()
        {
            if (Instance != null)
            {
                if (Application.isPlaying)
                    Destroy(gameObject);
                else
                    DestroyImmediate(gameObject);
                return;
            }

            Instance = gameObject.GetComponent<T>();
        }
    }

    public class SingletonPersistent<T> : Singleton<T>
        where T : MonoBehaviour
    {
        protected override void Awake()
        {
            base.Awake();
            DontDestroyOnLoad(gameObject);
        }
    }
}
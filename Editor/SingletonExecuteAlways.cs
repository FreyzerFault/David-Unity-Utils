using UnityEngine;

namespace Editor
{
    [ExecuteAlways]
    public class SingletonExecuteAlways<T> : Singleton<T>
        where T : MonoBehaviour
    {
    }
}
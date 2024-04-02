using UnityEngine;

namespace DavidUtils.Editor
{
    [ExecuteAlways]
    public class SingletonExecuteAlways<T> : Singleton<T>
        where T : MonoBehaviour
    {
    }
}
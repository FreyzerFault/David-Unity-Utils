using System;
using UnityEngine;

namespace DavidUtils.ScriptableObjectsUtils
{
    [ExecuteAlways]
    public abstract class AutoUpdatableSo : ScriptableObject
    {
        public Action ValuesUpdated;

        // protected virtual void Awake() => ValuesUpdated = null;

        public virtual void NotifyUpdate() => ValuesUpdated?.Invoke();
    }
}

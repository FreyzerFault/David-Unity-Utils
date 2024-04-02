using System;
using UnityEngine;

namespace DavidUtils.ScriptableObjectsUtils
{
    [ExecuteAlways]
    public abstract class AutoUpdatableSo : ScriptableObject
    {
        public Action ValuesUpdated;

        private void Awake() => ValuesUpdated = null;

        public virtual void NotifyUpdate() => ValuesUpdated?.Invoke();
    }
}

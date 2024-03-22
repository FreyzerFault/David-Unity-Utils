using System;
using UnityEngine;

namespace ScriptableObjectsUtils
{
    [ExecuteAlways]
    public abstract class AutoUpdatableSo : ScriptableObject
    {
        public bool autoUpdate = true;

        public Action OnValuesUpdated;

        private void Awake() => OnValuesUpdated = null;

#if UNITY_EDITOR
        public void OnValidate() => ValidationUtility.SafeOnValidate(OnUpdateValues);
#endif

        public virtual void OnUpdateValues()
        {
            if (autoUpdate) NotifyUpdate();
        }

        public void NotifyUpdate() => OnValuesUpdated?.Invoke();
    }
}
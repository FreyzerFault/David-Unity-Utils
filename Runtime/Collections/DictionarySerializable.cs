using System;
using System.Collections.Generic;
using System.Linq;
using DavidUtils.DevTools.CustomAttributes;
using DavidUtils.ExtensionMethods;
using UnityEngine;

namespace DavidUtils.Collections
{
    [Serializable]
    public class KeyValuePairSerializable<TKey, TValue>: ArrayElementTitleAttribute.IArrayElementTitle
    {
        [HideInInspector]
        public TKey key;
        public TValue value;
        
        public string Name => key.ToString();

        public KeyValuePairSerializable(TKey key, TValue value = default) { this.key = key; this.value = value; }
        public KeyValuePairSerializable(KeyValuePair<TKey, TValue> pair) { key = pair.Key; value = pair.Value; }
    }
    
    [Serializable]
    public class DictionarySerializable<TKey, TValue> : ISerializationCallbackReceiver
    {
        // TODO Comprobar que cada elemento tiene la key como titulo
        [ArrayElementTitle("key")]
        [SerializeField] private KeyValuePairSerializable<TKey, TValue>[] array = Array.Empty<KeyValuePairSerializable<TKey, TValue>>();
        protected Dictionary<TKey, TValue> dictionary = new();
        
        public TValue this[TKey key]
        {
            get => dictionary[key];
            set
            {
                // TODO comprobar si, al ser KeyValuePairSerializable una clase, el array
                // con los valores de los pairs se actualiza automáticamente
                
                dictionary[key] = value;
                
                // var pair = new KeyValuePairSerializable<TKey, TValue>(key, value);
                // if (_dictionary.TryAdd(key, value))
                // {
                //     array.Add(pair);
                // }
                // else
                // {
                //     _dictionary[key] = value;
                //     int index = array.FindIndex(pair => EqualityComparer<TKey>.Default.Equals(pair.key, key));
                //     array[index] = pair;
                // }
            }
        }


        #region SERIALIZATION

        public void OnBeforeSerialize() { }

        public void OnAfterDeserialize()
        {
            if (dictionary != null) dictionary.Clear();
            else dictionary = new Dictionary<TKey, TValue>();
            SincronizeDictionary();
        }
        
        private void SincronizeList() =>
            array = dictionary.Select(pair => new KeyValuePairSerializable<TKey, TValue>(pair)).ToArray();
        private void SincronizeDictionary() =>
            dictionary = new Dictionary<TKey, TValue>(array.Select(pair => new KeyValuePair<TKey, TValue>(pair.key, pair.value)));

        #endregion
        
        public TKey[] Keys => dictionary.Keys.ToArray();
        public TValue[] Values
        {
            get => dictionary.Values.ToArray();
            set
            {
                if (value.Length != dictionary.Count)
                    throw new ArgumentException("Values array must have the same length as the dictionary");
                dictionary.Keys.ForEach((key, i) => this[key] = value[i]);
            }
        }

        public DictionarySerializable() {}
        
        public DictionarySerializable(TKey[] keyList, TValue[] values = null)
        {
            array = keyList.Select((key, i) =>
                    new KeyValuePairSerializable<TKey, TValue>(
                        key,
                        values == null
                            ? default
                            : i < values.Length
                                ? values[i]
                                : default))
                .ToArray();
            SincronizeDictionary();
        }

        public DictionarySerializable(KeyValuePairSerializable<TKey, TValue>[] elements)
        {
            array = elements.ToArray();
            SincronizeDictionary();
        }

        // Transform Dictionary => List (Serializable Dictionary)
        public DictionarySerializable(Dictionary<TKey, TValue> dictionary)
        {
            this.dictionary = dictionary;
            SincronizeList();
        }
    }
    
}

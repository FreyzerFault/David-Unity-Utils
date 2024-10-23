using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DavidUtils.DevTools.CustomAttributes;
using DavidUtils.ExtensionMethods;
using UnityEngine;
using UnityEngine.Serialization;

namespace DavidUtils.Collections
{
    [Serializable]
    public class KeyValuePairSerializable<TKey, TValue>: ArrayElementTitleAttribute.IArrayElementTitle
    {
        public TKey Key { get; }
        public TValue Value { get; }

        public string Name => Key.ToString();
        
        public KeyValuePairSerializable(TKey key, TValue value = default) { Key = key; Value = value; }
        public KeyValuePairSerializable(KeyValuePair<TKey, TValue> pair) { Key = pair.Key; Value = pair.Value; }
    }
    
    [Serializable]
    public class DictionarySerializable<TKey, TValue> : UnityEngine.Object, IEnumerable<KeyValuePairSerializable<TKey, TValue>>, ISerializationCallbackReceiver
    {
        // TODO Comprobar que cada elemento tiene la key como titulo
        [FormerlySerializedAs("array")]
        [ArrayElementTitle("key")]
        [SerializeField] private List<KeyValuePairSerializable<TKey, TValue>> list = new();
        protected Dictionary<TKey, TValue> dictionary = new();
        
        public TValue this[TKey key]
        {
            get => dictionary[key];
            set
            {
                Debug.Log($"Setting value {key} : {value}");
                
                var pair = new KeyValuePairSerializable<TKey, TValue>(key, value);
                if (dictionary.TryAdd(key, value))
                {
                    list.Add(pair);
                }
                else
                {
                    dictionary[key] = value;
                    int index = list.FindIndex(p => 
                        EqualityComparer<TKey>.Default.Equals(p.Key, key));
                    if (index != -1)
                        list[index] = pair;
                    else
                        SincronizeList();
                }
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
            list = dictionary.Select(pair => new KeyValuePairSerializable<TKey, TValue>(pair)).ToList();
        private void SincronizeDictionary() =>
            dictionary = new Dictionary<TKey, TValue>(list.Select(pair => new KeyValuePair<TKey, TValue>(pair.Key, pair.Value)));

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
            list = keyList.Select((key, i) =>
                    new KeyValuePairSerializable<TKey, TValue>(
                        key,
                        values == null
                            ? default
                            : i < values.Length
                                ? values[i]
                                : default))
                .ToList();
            SincronizeDictionary();
        }

        public DictionarySerializable(KeyValuePairSerializable<TKey, TValue>[] elements)
        {
            list = elements.ToList();
            SincronizeDictionary();
        }

        // Transform Dictionary => List (Serializable Dictionary)
        public DictionarySerializable(Dictionary<TKey, TValue> dictionary)
        {
            this.dictionary = dictionary;
            SincronizeList();
        }

        public DictionarySerializable(DictionarySerializable<TKey, TValue> other) : this(other.Keys, other.Values) { }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public IEnumerator<KeyValuePairSerializable<TKey, TValue>> GetEnumerator()
        {
            Debug.Log($"{(list == null ? "null" : "list")}.GetEnumerator");
            return list.GetEnumerator();
        }

        public void Add(TKey key, TValue value) => this[key] = value;
    }
    
}

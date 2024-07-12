using System;
using System.Collections.Generic;
using System.Linq;
using DavidUtils.ExtensionMethods;

namespace DavidUtils.Collections
{
    [Serializable]
    public class DictionarySerializable<TKey, TValue>
    {
        public List<KeyValuePairSerializable> pairElements;
        
        public TKey[] Keys => pairElements.Select(pair => pair.key).ToArray();
        public TValue[] Values
        {
            get => pairElements.Select(pair => pair.value).ToArray();
            set => pairElements.ForEach((p,i) => p.value = value[i]);
        }

        // Empty Dict
        public DictionarySerializable() => pairElements = new List<KeyValuePairSerializable>();
        
        public DictionarySerializable(TKey[] keyList, TValue[] values = null) =>
            pairElements = keyList.Select((key, i) =>
                    new KeyValuePairSerializable(
                        key,
                        values == null 
                            ? default 
                            : i < values.Length ? values[i] : default))
                .ToList();

        public DictionarySerializable(KeyValuePairSerializable[] elements) => pairElements = elements.ToList();

        // Transform Dictionary => List (Serializable Dictionary)
        public DictionarySerializable(Dictionary<TKey, TValue> dictionary) =>
            pairElements = dictionary.Select(
                    pair => new KeyValuePairSerializable(pair.Key, pair.Value)
                )
                .ToList();

        public TValue GetValue(TKey key)
        {
            var element = pairElements.Find(
                pair => EqualityComparer<TKey>.Default.Equals(pair.key, key)
            );

            if (element == null) throw new KeyNotFoundException("Key not found: " + key);

            return element.value;
        }

        public void SetValue(TKey key, TValue value)
        {
            var element = pairElements.Find(pair => EqualityComparer<TKey>.Default.Equals(pair.key, key));
            if (element == null)
                pairElements.Add(new KeyValuePairSerializable(key, value));
            else
                element.value = value;
        }

        [Serializable]
        public class KeyValuePairSerializable
        {
            public TKey key;
            public TValue value;

            public KeyValuePairSerializable(TKey key, TValue value = default) { this.key = key; this.value = value; }
        }
    }
}

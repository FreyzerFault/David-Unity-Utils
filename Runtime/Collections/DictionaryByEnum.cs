using System;
using System.Linq;
using DavidUtils.ExtensionMethods;

namespace DavidUtils.Collections
{
    [Serializable]
    public class DictionaryByEnum<TEnum, TValue>: DictionarySerializable<TEnum, TValue> where TEnum : Enum
    {
        public DictionaryByEnum()
        : base(typeof(TEnum).GetEnumValues<TEnum>())
        {
            if (!typeof(TEnum).IsEnum)
                throw new ArgumentException("TEnum must be an enumerated type");
            
            var enumValues = typeof(TEnum).GetEnumValues<TEnum>();
            if (enumValues.Length != enumValues.Distinct().Count())
                throw new ArgumentException("TEnum must have unique values");
        }

        public DictionaryByEnum(TValue[] values) : this()
        {
            Values = values;
            int enumCount = typeof(TEnum).GetEnumValues<TEnum>().Length;
            if (values.Length != enumCount)
                throw new ArgumentException(
                    $"Values array ({values.Length} values) must have the same length as the enum values ({enumCount})"
                    );
        }
    }
}

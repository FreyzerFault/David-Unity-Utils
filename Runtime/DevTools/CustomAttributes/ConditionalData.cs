using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DavidUtils.ExtensionMethods;

namespace DavidUtils.DevTools.CustomAttributes
{
	public class ConditionalData: IEnumerable<(string Field, bool Inverse, string[] CompareAgainst)>
	{
		public bool IsSet => fieldToCheck.NotNullOrEmpty() || fieldsToCheckMultiple.NotNullOrEmpty()
		                                                    || predicateMethod.NotNullOrEmpty();
		public readonly string fieldToCheck;
		public readonly bool inverse;
		public readonly string[] compareValues;

		public readonly string[] fieldsToCheckMultiple;
		public readonly bool[] inverseMultiple;
		public readonly string[] compareValuesMultiple;
		
		public MethodInfo cachedMethodInfo;
		public bool initializedMethodInfo;

		public readonly string predicateMethod;

		public ConditionalData(string fieldToCheck, bool inverse = false, params object[] compareValues)
			=> (this.fieldToCheck, this.inverse, this.compareValues) =
				(fieldToCheck, inverse, compareValues.Select(c => c.ToString().ToUpper()).ToArray());

		public ConditionalData(string[] fieldToCheck, bool[] inverse = null, params object[] compare) =>
			(fieldsToCheckMultiple, inverseMultiple, compareValuesMultiple) =
			(fieldToCheck, inverse, compare.Select(c => c.ToString().ToUpper()).ToArray());

		public ConditionalData(params string[] fieldToCheck) => fieldsToCheckMultiple = fieldToCheck;

		// ReSharper disable once UnusedParameter.Local
		public ConditionalData(bool useMethod, string methodName, bool inverse = false)
			=> (predicateMethod, this.inverse) = (methodName, inverse);

		
		///     Iterate over Field Conditions
		public IEnumerator<(string Field, bool Inverse, string[] CompareAgainst)> GetEnumerator()
		{
			if (fieldToCheck.NotNullOrEmpty()) yield return (fieldToCheck, inverse, compareValues);
			if (fieldsToCheckMultiple.NotNullOrEmpty())
				for (var i = 0; i < fieldsToCheckMultiple.Length; i++)
				{
					string field = fieldsToCheckMultiple[i];
					bool withInverseValue = inverseMultiple != null && inverseMultiple.Length - 1 >= i;
					bool withCompareValue = compareValuesMultiple != null && compareValuesMultiple.Length - 1 >= i;
					bool thisInverse = withInverseValue && inverseMultiple[i];
					string[] compare = withCompareValue ? new[] { compareValuesMultiple[i] } : null;

					yield return (field, thisInverse, compare);
				}
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}

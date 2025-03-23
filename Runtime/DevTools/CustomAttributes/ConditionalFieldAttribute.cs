using System;
using UnityEngine;

namespace DavidUtils.DevTools.CustomAttributes
{
	/// <summary>
	///     Conditionally Show/Hide field in inspector, based on some other field or property value
	/// </summary>
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
	public class ConditionalFieldAttribute : PropertyAttribute
	{
		public readonly ConditionalData data;
		
		public bool IsSet => data is { IsSet: true };
		
		/// <param name="fieldToCheck">String name of field to check value</param>
		/// <param name="inverse">Inverse check result</param>
		/// <param name="compareValues">On which values field will be shown in inspector</param>
		public ConditionalFieldAttribute(string fieldToCheck, bool inverse = false, params object[] compareValues)
			=> data = new ConditionalData(fieldToCheck, inverse, compareValues);


		public ConditionalFieldAttribute(string[] fieldToCheck, bool[] inverse = null, params object[] compare)
			=> data = new ConditionalData(fieldToCheck, inverse, compare);

		public ConditionalFieldAttribute(params string[] fieldToCheck) => data = new ConditionalData(fieldToCheck);

		public ConditionalFieldAttribute(bool useMethod, string method, bool inverse = false)
			=> data = new ConditionalData(useMethod, method, inverse);
	}
}

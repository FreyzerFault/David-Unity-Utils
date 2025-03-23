using System.Reflection;
using UnityEngine;

namespace DavidUtils.DevTools.Reflection
{
	[ExecuteAlways]
	public class ReflectionDemo : MonoBehaviour
	{
		public MonoBehaviour targetObj;

		private FieldInfo[] _exposedFields;

		private void Start() => _exposedFields = ExposedFieldMethods.GetExposedFields(targetObj);
	}
}

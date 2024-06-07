using System.Reflection;
using UnityEngine;

namespace DavidUtils.DevTools.Reflection
{
	[ExecuteAlways]
	public class ReflectionDemo : MonoBehaviour
	{
		public MonoBehaviour targetObj;

		private FieldInfo[] exposedFields;

		private void Start() => exposedFields = ExposedFieldMethods.GetExposedFields(targetObj);
	}
}

using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DavidUtils
{
	public static class UnityUtils
	{
		#region OBJECT METHODS

		/// <summary>
		/// Destroy para Player y Editor
		/// </summary>
		public static void DestroySafe(MonoBehaviour mb) => DestroySafe(mb.gameObject);
		public static void DestroySafe(Component comp) => DestroySafe(comp.gameObject);
		public static void DestroySafe(GameObject obj) => Destroy(obj);
		private static Action<GameObject> Destroy => Application.isPlaying ? Object.Destroy : Object.DestroyImmediate;

		/// <summary>
		/// Crea un EMPTY OBJECT normal y corriente
		/// </summary>
		public static GameObject InstantiateEmptyObject(
			Transform parent,
			string name = "New Object",
			Vector3? localPos = null,
			Quaternion? localRot = null,
			Vector3? localScale = null
		)
		{
			var obj = new GameObject(name)
			{
				transform =
				{
					parent = parent,
					localPosition = localPos ?? Vector3.zero,
					localRotation = localRot ?? Quaternion.identity,
					localScale = localScale ?? Vector3.one
				}
			};
			return obj;
		}
		
		#endregion
	}
}

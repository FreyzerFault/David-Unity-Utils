﻿using System;
using System.Collections.Generic;
using DavidUtils.ExtensionMethods;
using UnityEngine;
using UnityEngine.Rendering;

namespace DavidUtils
{
	public static class UnityUtils
	{
		#region OBJECT METHODS

		/// <summary>
		///     Destroy para Player y Editor
		/// </summary>
		public static void DestroySafe(MonoBehaviour mb) => DestroySafe(mb?.gameObject);

		public static void DestroySafe(Component comp) => DestroySafe(comp?.gameObject);

		public static void DestroySafe(GameObject obj)
		{
			if (obj != null) Destroy(obj);
			else Debug.LogWarning("[UnityUtils.DestroySafe()]: Trying to destroy a null object".Colored("orange"));
		}

		public static void DestroySafe(IEnumerable<GameObject> objs) => objs?.ForEach(DestroySafe);
		public static void DestroySafe(IEnumerable<Component> objs) => objs?.ForEach(DestroySafe);
		public static void DestroySafe(IEnumerable<MonoBehaviour> objs) => objs?.ForEach(DestroySafe);
		
		private static Action<GameObject> Destroy => 
			Application.isPlaying 
				? UnityEngine.Object.Destroy 
				: UnityEngine.Object.DestroyImmediate;

		/// <summary>
		///     Crea un EMPTY OBJECT normal y corriente
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
		
		/// <summary>
		/// Instancia un GameObject con un componente de tipo T 
		/// </summary>
		public static T InstantiateObject<T>(
			Transform parent,
			string name = "New Object",
			Vector3? localPos = null,
			Quaternion? localRot = null,
			Vector3? localScale = null
		) where T : Component
		{
			GameObject obj = new(name)
			{
				transform =
				{
					parent = parent,
					localPosition = localPos ?? Vector3.zero,
					localRotation = localRot ?? Quaternion.identity,
					localScale = localScale ?? Vector3.one
				}
			};
			
			return obj.AddComponent<T>();
		}

		#endregion


		#region LIGHTING

		public static void ToggleShadows(this GameObject go, bool castShadows = true)
		{
			foreach (Renderer renderer in go.GetComponentsInChildren<Renderer>(true))
				renderer.shadowCastingMode = castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
		}

		#endregion
	}
}

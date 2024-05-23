using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DavidUtils
{
	public static class UnityUtils
	{
		#region OBJECT METHODS

		private static Action<GameObject> Destroy => Application.isPlaying ? Object.Destroy : Object.DestroyImmediate;
		public static void DestroySafe(MonoBehaviour mb) => Destroy(mb.gameObject);

		#endregion
	}
}

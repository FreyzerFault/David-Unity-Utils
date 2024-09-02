using DavidUtils.ExtensionMethods;
using UnityEditor;
using UnityEngine;

namespace DavidUtils.MouseInputs
{
	public static class MouseInputUtils
	{
		public static Vector3 MousePosition => Input.mousePosition;
		public static Vector3 MouseWorldPosition => Camera.main!.ScreenToWorldPoint(MousePosition);

		// Checkea límites de la Ventana (Screen).
		// Note: No funciona bien en el editor si activamos Gizmos en varias ventanas (Scene, Game)
		// Se ejecuta una vez por cada ventana donde tengamos Gizmos
		public static bool MouseInScreen()
		{
			Vector3 p = MousePosition;
			return p.x >= 0 && p.x <= Screen.width && p.y >= 0 && p.y <= Screen.height;
		}

		/// <summary>
		///     Checkea si el Mouse apunta a un área 2D desde una vista CENITAL (es decir, un Quad en el plano XZ)
		/// </summary>
		public static bool MouseInArea_CenitalView(Vector3 originPos, Vector2 size, out Vector2 normalizedPos)
		{
			normalizedPos = Vector2.zero;
			if (Camera.main == null) return false;

			Vector3 worldMousePos = Camera.main.ScreenToWorldPoint(MousePosition).WithY(0);

			normalizedPos = (worldMousePos - originPos).ToV2xz() / size;

			return normalizedPos.IsIn01();
		}

		/// <summary>
		///     Checkea si el Mouse apunta a un área 2D desde una vista FRONTAL (es decir, un Quad en el plano XY)
		/// </summary>
		public static bool MouseInArea_FrontView(Vector3 originPos, Vector2 size, out Vector2 normalizedPos)
		{
			normalizedPos = Vector2.zero;
			if (Camera.main == null) return false;

			Vector3 mousePos = MousePosition;
			Vector3 worldMousePos = Camera.main.ScreenToWorldPoint(mousePos).WithZ(0);

			normalizedPos = (worldMousePos - originPos).ToV2xy() / size;

			return normalizedPos.IsIn01();
		}

		#region DEBUG

#if UNITY_EDITOR

		#region MOUSE POSITION IN SCENE

		// Mouse Position when Mouse is in Scene Window
		public static Vector3 mouseWorldPosition_InScene = Vector3.zero;

		public static Vector2 MouseWorldPosition_InScene_XY =>
			mouseWorldPosition_InScene.ToV2xy();
		public static Vector2 MouseWorldPosition_InScene_XZ =>
			mouseWorldPosition_InScene.ToV2xz();

		// !=======================================================================================
		// ! USAR ESTO EN ONGUI() de cualquier MonoBehaviour que vaya a estar ACTIVO SIEMPRE
		// ! para actualizar la posicion del Mouse en la Escena si vas a usarla
		// !=======================================================================================
		public static void UpdateMousePositionInScene() =>
			mouseWorldPosition_InScene = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition).origin;

		#endregion


		#region GIZMOS

		public static void DrawGizmos_XZ()
		{
			Gizmos.color = Color.red;
			Gizmos.DrawSphere(MouseWorldPosition.WithY(0), .1f);
		}

		public static void DrawGizmos_XY()
		{
			Gizmos.color = Color.red;
			Gizmos.DrawSphere(MouseWorldPosition.WithZ(0), 0.1f);
		}

		#endregion

#endif

		#endregion
	}
}

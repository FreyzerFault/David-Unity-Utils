using UnityEngine;

namespace DavidUtils.ExtensionMethods
{
	public static class TransformExtensionMethods
	{
		// Locks the rotation of the transform to the horizontal plane
		public static Transform LockRotationVertical(this Transform transform)
		{
			transform.rotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0);
			return transform;
		}

		public static void Billboard(
			this Transform transform,
			Transform target,
			bool verticalLock = false
		)
		{
			// transform.rotation = Quaternion.LookRotation(target.forward, target.up);
			transform.LookAt(target);
			if (verticalLock)
				transform.LockRotationVertical();
		}


		#region Set X/Y/Z

		public static void SetX(this Transform transform, float x) => transform.position = transform.position.WithX(x);
		public static void SetY(this Transform transform, float y) => transform.position = transform.position.WithY(y);
		public static void SetZ(this Transform transform, float z) => transform.position = transform.position.WithZ(z);

		public static void SetXY(this Transform transform, float x, float y) =>
			transform.position = transform.position.WithX(x).WithY(y);

		public static void SetXY(this Transform transform, Vector2 xy) =>
			transform.position = transform.position.WithX(xy.x).WithY(xy.y);

		public static void SetXZ(this Transform transform, float x, float z) =>
			transform.position = transform.position.WithX(x).WithZ(z);

		public static void SetXZ(this Transform transform, Vector2 xz) =>
			transform.position = transform.position.WithX(xz.x).WithZ(xz.y);

		public static void SetYZ(this Transform transform, float y, float z) =>
			transform.position = transform.position.WithY(y).WithZ(z);

		public static void SetYZ(this Transform transform, Vector2 yz) =>
			transform.position = transform.position.WithY(yz.x).WithZ(yz.y);

		#endregion
		
		
		#region WORLD TRANSFORMATIONS

		public static Vector3 ToWorld(this Transform transform, Vector3 localPoint) 
			=> transform.localToWorldMatrix.MultiplyPoint(localPoint);
		
		public static Vector3 ToWorld(this Transform transform, Vector2 localPoint) 
			=> transform.localToWorldMatrix.MultiplyPoint(localPoint.ToV3xy());
		
		public static Vector3 ToLocal(this Transform transform, Vector3 worldPoint) 
			=> transform.worldToLocalMatrix.MultiplyPoint(worldPoint);
		
		public static Vector3 ToLocal(this Transform transform, Vector2 worldPoint) 
			=> transform.worldToLocalMatrix.MultiplyPoint(worldPoint.ToV3xy());

		#endregion
	}
}

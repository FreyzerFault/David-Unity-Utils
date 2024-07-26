using System;
using DavidUtils.DevTools.CustomAttributes;
using DavidUtils.DevTools.GizmosAndHandles;
using DavidUtils.ExtensionMethods;
using UnityEngine;
using UnityEngine.Serialization;

namespace DavidUtils.Geometry.Bounding_Box
{
	public class BoundsComponent : MonoBehaviour
	{
		[HideInInspector]
		public bool is2D;

		[ConditionalField("is2D", true)]
		public Bounds bounds3D;

		[FormerlySerializedAs("bounds2D")]
		[ConditionalField("is2D", false)]
		public AABB_2D aabb2D = AABB_2D.NormalizedAABB;

		[ConditionalField("is2D", false)]
		public bool XZplane = true;

		public Action OnChanged;

		public Vector3 Center
		{
			get => bounds3D.center;
			set
			{
				bounds3D.center = value;
				OnChanged?.Invoke();
				Sincronize3Dto2D();
			}
		}

		public Vector3 Size
		{
			get => is2D ? aabb2D.Size.ToV3(XZplane) : bounds3D.size;
			set
			{
				bounds3D.size = value;
				OnChanged?.Invoke();
				Sincronize3Dto2D();
			}
		}

		public Vector2 Size2D
		{
			get => aabb2D.Size;
			set
			{
				aabb2D.Size = value;
				OnChanged?.Invoke();
				Sincronize2Dto3D();
			}
		}

		public Vector3 Min
		{
			get => is2D ? aabb2D.min.ToV3(XZplane) : bounds3D.min;
			set
			{
				bounds3D.min = value;
				aabb2D.min = value.ToV2(XZplane);
			}
		}
		public Vector3 Max
		{
			get => is2D ? aabb2D.max.ToV3(XZplane) : bounds3D.max;
			set
			{
				bounds3D.max = value;
				aabb2D.max = value.ToV2(XZplane);
			}
		}
		
		
		private void Awake() => SincronizeBounds();
		private void OnEnable() => SincronizeBounds();


		#region SPACE TRANSFORMATION

		public Matrix4x4 BoundsToLocalMatrix =>
			Matrix4x4.Scale(is2D ? aabb2D.Size.Inverse() : bounds3D.size.Inverse()) *
			Matrix4x4.Rotate(is2D && XZplane ? AABB_2D.RotationToXYplane : Quaternion.identity) *
			Matrix4x4.Translate(-Min);

		public Matrix4x4 LocalToBoundsMatrix => Matrix4x4.TRS(
			Min,
			is2D && XZplane ? AABB_2D.RotationToXZplane : Quaternion.identity,
			is2D ? aabb2D.Size.ToV3().WithZ(1) : bounds3D.size
		);
		public Matrix4x4 WorldToLocalMatrix => BoundsToLocalMatrix * transform.worldToLocalMatrix;
		public Matrix4x4 LocalToWorldMatrix => transform.localToWorldMatrix * LocalToBoundsMatrix;
		
		// Using XZ coords for 2D positions
		public Matrix4x4 LocalToWorldMatrix_WithXZrotation =>
			LocalToWorldMatrix * Matrix4x4.Rotate(!is2D && XZplane ? AABB_2D.RotationToXZplane : Quaternion.identity);
		public Matrix4x4 WorldToLocalMatrix_WithXZrotation =>
			Matrix4x4.Rotate(!is2D && XZplane ? AABB_2D.RotationToXYplane : Quaternion.identity) * WorldToLocalMatrix;
		
		public Vector3 ToWorld(Vector2 pos) => LocalToWorldMatrix_WithXZrotation.MultiplyPoint3x4(pos);
		public Vector3 ToWorldVector(Vector2 worldVector) => LocalToWorldMatrix_WithXZrotation.MultiplyVector(worldVector);
		public Vector3 ToLocal(Vector3 worldPos) => WorldToLocalMatrix_WithXZrotation.MultiplyPoint3x4(worldPos);
		public Vector3 ToLocalVector(Vector3 worldVector) => WorldToLocalMatrix_WithXZrotation.MultiplyVector(worldVector);

		
		// Convierte un vector en el espacio de mundo a (0,1)
		public Vector2 VectorToLocalPositive(Vector3 vector) => 
			Vector2.Max(ToLocalVector(vector).Abs(), Vector2.one * 0.001f);
		public Vector2 VectorToLocalPositive(Vector2 vector) => VectorToLocalPositive(vector.ToV3xz());
		public Vector2 MeasureToLocalPositive(float value) => VectorToLocalPositive(Vector3.one * value);
		
		#endregion


		#region 2D - 3D SINCRONIZATION

		public void SincronizeBounds()
		{
			if (is2D) Sincronize2Dto3D();
			else Sincronize3Dto2D();
		}

		private void Sincronize3Dto2D() => aabb2D = new AABB_2D(bounds3D, XZplane);
		private void Sincronize2Dto3D()
		{
			Vector3 center = new Vector3(
				aabb2D.Center.x,
				XZplane ? bounds3D.center.y : aabb2D.Center.y,
				XZplane ? aabb2D.Center.y : bounds3D.center.z
			);
			Vector3 size = new Vector3( 
				aabb2D.Size.x,
				XZplane ? bounds3D.size.y : aabb2D.Size.y,
				XZplane ? aabb2D.Size.y : bounds3D.size.z
			);
			bounds3D = new Bounds(center, size);
		}

		#endregion

		
		#region DEBUG

#if UNITY_EDITOR

		private void OnDrawGizmos()
		{
			SincronizeBounds();
			if (is2D)
				GizmosExtensions.DrawQuadWire(LocalToWorldMatrix, 5, Color.green);
			else
				GizmosExtensions.DrawCubeWire(LocalToWorldMatrix, 5, Color.green);
		}

#endif

		#endregion

		
		#region TRANSFORMATION to BOUNDS SPACE

		public void TransformToBounds_Local(Component obj) => 
			obj?.transform.ApplyLocalMatrix(
				LocalToBoundsMatrix * Matrix4x4.Rotate(!is2D && XZplane ? AABB_2D.RotationToXZplane : Quaternion.identity)
			);
		public void TransformToBounds_World(Component obj) => obj?.transform.ApplyWorldMatrix(LocalToWorldMatrix);

		// Ajusta el tamaño del AABB al terreno
		public void AdjustToTerrain(Terrain terrain)
		{
			bounds3D = terrain.terrainData.bounds;
			is2D = false;
			Sincronize3Dto2D();
			OnChanged?.Invoke();
		}

		#endregion
	}
}

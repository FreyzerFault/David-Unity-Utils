using System;
using DavidUtils.ExtensionMethods;
using DavidUtils.Rendering.Extensions;
using UnityEngine;

namespace DavidUtils.Rendering
{
	[Serializable]
	public class Points2DRenderer : DynamicRenderer<Vector2[]>
	{
		protected MeshRenderer[] spheresMr = Array.Empty<MeshRenderer>();

		protected override string DefaultChildName => "Point";

		[Range(0.1f, 1)]
		public float sphereScale = .5f;

		public override void Instantiate(Vector2[] polygons, string childName = null)
		{
			if (polygons.Length == 0) return;

			if (spheresMr.Length != 0) Clear();

			if (colors.Length != polygons.Length)
				SetRainbowColors(polygons.Length);


			spheresMr = new MeshRenderer[polygons.Length];
			for (var i = 0; i < polygons.Length; i++)
			{
				Vector2 point = polygons[i];

				MeshRendererExtensions.InstantiateSphere(
					out MeshRenderer mr,
					out MeshFilter mf,
					transform,
					$"{childName ?? DefaultChildName} {i}",
					colors[i],
					Material
				);

				spheresMr[i] = mr;

				// Actualiza posicion y escala
				Transform sphereTransform = mr.transform;
				sphereTransform.SetLocalPositionAndRotation(point, Quaternion.identity);

				// Compensa el Scale Global para verse siempre del mismo tamaño
				sphereTransform.SetGlobalScale(Vector3.one * sphereScale);

				// MATERIAL
				mr.sharedMaterial = Material;
			}
		}

		public override void UpdateGeometry(Vector2[] regions)
		{
			// Faltan o sobran MeshRenderers para los puntos dados
			if (spheresMr.Length != regions.Length)
			{
				Clear();
				Instantiate(regions);
				return;
			}

			// Actualiza la posición de las semillas
			for (var i = 0; i < spheresMr.Length; i++)
				spheresMr[i].transform.localPosition = regions[i];
		}

		public override void Clear()
		{
			base.Clear();

			if (spheresMr == null) return;
			foreach (MeshRenderer meshRenderer in spheresMr)
				UnityUtils.DestroySafe(meshRenderer);

			spheresMr = Array.Empty<MeshRenderer>();
		}

		public void MovePoint(int index, Vector2 newPos) =>
			spheresMr[index].transform.localPosition = newPos;

		public void ProjectOnTerrain(Terrain terrain)
		{
			foreach (MeshRenderer sphere in spheresMr)
			{
				Vector3 pos = sphere.transform.position;
				pos.y = terrain.SampleHeight(pos);
				sphere.transform.position = pos;
			}
		}

		#region DEBUG

#if UNITY_EDITOR

		public bool drawGizmos;

		private void OnDrawGizmos()
		{
			if (!drawGizmos) return;

			Gizmos.color = colors?.Length > 0 ? colors[0] : Color.grey;
			for (var i = 0; i < spheresMr.Length; i++)
			{
				if (colors?.Length > 0)
					Gizmos.color = colors[i];
				Gizmos.DrawSphere(
					transform.localToWorldMatrix.MultiplyPoint3x4(spheresMr[i].transform.position),
					sphereScale
				);
			}
		}

#endif

		#endregion
	}
}

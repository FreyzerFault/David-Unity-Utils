using System;
using System.Collections.Generic;
using System.Linq;
using DavidUtils.ExtensionMethods;
using DavidUtils.Rendering.Extensions;
using UnityEngine;

namespace DavidUtils.Rendering
{
	[Serializable]
	public class Points2DRenderer : DynamicRenderer<IEnumerable<Vector2>>
	{
		protected List<MeshRenderer> spheresMr = new();

		protected override string DefaultChildName => "Point";

		[Range(0.1f, 1)]
		public float sphereScale = .5f;
		private Vector3 SphereScale => Vector3.one * sphereScale;

		public override void Instantiate(IEnumerable<Vector2> points, string childName = null)
		{
			if (points.IsNullOrEmpty()) return;

			if (spheresMr.Count != 0) Clear();

			SetRainbowColors(points.Count());

			spheresMr = points.Select((p, i) => InstantiateSphere(i, p, childName)).ToList();
		}

		public override void UpdateGeometry(IEnumerable<Vector2> points)
		{
			// Faltan o sobran MeshRenderers para los puntos dados
			if (points.Count() != colors.Length)
				SetRainbowColors(points.Count());

			// Actualiza las posiciones
			points.ForEach(
				(p, i) =>
				{
					if (i >= spheresMr.Count)
						spheresMr.Add(InstantiateSphere(i, p));
					else
						spheresMr[i].transform.localPosition = p;
				}
			);
		}

		private MeshRenderer InstantiateSphere(int i, Vector2 p, string childName = null)
		{
			MeshRendererExtensions.InstantiateSphere(
				out MeshRenderer mr,
				out MeshFilter mf,
				transform,
				$"{childName ?? DefaultChildName} {i}",
				colors[i],
				Material
			);

			// Actualiza posicion y escala
			Transform sphereTransform = mr.transform;
			sphereTransform.localPosition = p;

			// Compensa el Scale Global para verse siempre del mismo tamaño
			sphereTransform.SetGlobalScale(SphereScale);

			// MATERIAL
			mr.sharedMaterial = Material;

			return mr;
		}


		public override void Clear()
		{
			base.Clear();

			if (spheresMr == null) return;
			foreach (MeshRenderer meshRenderer in spheresMr)
				UnityUtils.DestroySafe(meshRenderer);

			spheresMr.Clear();
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
			for (var i = 0; i < spheresMr.Count; i++)
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

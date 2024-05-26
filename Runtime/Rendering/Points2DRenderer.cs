using System;
using DavidUtils.ExtensionMethods;
using DavidUtils.Rendering.Extensions;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DavidUtils.Rendering
{
	[Serializable]
	public class Points2DRenderer : DynamicRenderer<Vector2[]>
	{
		protected MeshRenderer[] spheresMr = Array.Empty<MeshRenderer>();

		protected override string DefaultName => "Points 2D Renderer";
		protected override string DefaultChildName => "Point";
		
		[Range(0.1f, 1)]
		public float sphereScale = .3f;
		
		// protected override Material Material => Resources.Load<Material>("Materials/Geometry Unlit");

		public override void Instantiate(Vector2[] points, string childName = null)
		{
			if (points.Length == 0) return;

			if (spheresMr.Length != 0) Clear();

			if (colors.Length != points.Length)
				SetRainbowColors(points.Length);


			spheresMr = new MeshRenderer[points.Length];
			for (var i = 0; i < points.Length; i++)
			{
				Vector2 point = points[i];

				MeshRendererExtensions.InstantiateSphere(
					out MeshRenderer mr,
					out MeshFilter mf,
					renderObj.transform,
					$"{childName ?? DefaultChildName} {i}",
					colors[i],
					Material
				);

				spheresMr[i] = mr;

				// Actualiza posicion y escala
				Transform sphereTransform = mr.transform;
				sphereTransform.SetLocalPositionAndRotation(
					point.ToV3xz(),
					Quaternion.identity
				);
				// Compensa el Scale Global para verse siempre del mismo tamaño
				sphereTransform.SetGlobalScale(Vector3.one * sphereScale / sphereTransform.lossyScale.x);

				// MATERIAL
				mr.sharedMaterial = Material;
			}

			UpdateVisibility();
		}

		public override void Update(Vector2[] points)
		{
			UpdateVisibility();
			
			if (!active) return;

			// Faltan o sobran MeshRenderers para los puntos dados
			if (spheresMr.Length != points.Length)
			{
				Clear();
				Instantiate(points);
				return;
			}

			// Actualiza la posición de las semillas
			for (var i = 0; i < spheresMr.Length; i++)
				spheresMr[i].transform.localPosition = points[i].ToV3xz();
		}

		public override void Clear()
		{
			if (spheresMr == null) return;
			foreach (MeshRenderer meshRenderer in spheresMr)
				Object.Destroy(meshRenderer);

			spheresMr = Array.Empty<MeshRenderer>();
		}

		public void MovePoint(int index, Vector2 newPos) =>
			spheresMr[index].transform.localPosition = newPos.ToV3xz();

		public void ProjectOnTerrain(Terrain terrain)
		{
			foreach (MeshRenderer sphere in spheresMr)
			{
				Vector3 pos = sphere.transform.position;
				pos.y = terrain.SampleHeight(pos);
				sphere.transform.position = pos;
			}
		}
	}
}

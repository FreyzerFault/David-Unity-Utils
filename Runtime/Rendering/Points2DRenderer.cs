using System;
using DavidUtils.ExtensionMethods;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DavidUtils.Rendering
{
	[Serializable]
	public class Points2DRenderer : DynamicRenderer<Vector2[]>
	{
		public MeshRenderer[] spheresMr = Array.Empty<MeshRenderer>();

		public float sphereScale = .3f;
		private float heightOffset = .2f;

		protected override string DefaultName => "Points 2D Renderer";

		// protected override Material Material => Resources.Load<Material>("Materials/Geometry Unlit");

		public override void Instantiate(Vector2[] seeds)
		{
			if (seeds.Length == 0) return;

			if (spheresMr.Length != 0) Clear();

			if (colors.Length != seeds.Length)
				SetRainbowColors(seeds.Length);


			spheresMr = new MeshRenderer[seeds.Length];
			for (var i = 0; i < seeds.Length; i++)
			{
				Vector2 seed = seeds[i];

				MeshRendererExtensions.InstantiateSphere(
					out MeshRenderer mr,
					out MeshFilter mf,
					renderObj.transform,
					$"Seed {i}",
					colors[i],
					Material
				);

				spheresMr[i] = mr;

				// Actualiza posicion y escala
				Transform sphereTransform = mr.transform;
				sphereTransform.SetLocalPositionAndRotation(
					seed.ToV3xz().WithY(heightOffset),
					Quaternion.identity
				);
				// Compensa el Scale Global para verse siempre del mismo tamaño
				sphereTransform.SetGlobalScale(Vector3.one * sphereScale / sphereTransform.lossyScale.x);

				// MATERIAL
				mr.sharedMaterial = Material;
			}

			UpdateVisibility();
		}

		public override void Update(Vector2[] seeds)
		{
			if (!active) return;

			// Faltan o sobran MeshRenderers para las seeds dadas
			if (spheresMr.Length != seeds.Length)
			{
				Clear();
				Instantiate(seeds);
				return;
			}

			// Actualiza la posición de las semillas
			for (var i = 0; i < spheresMr.Length; i++)
				spheresMr[i].transform.localPosition = seeds[i].ToV3xz().WithY(heightOffset);

			UpdateVisibility();
		}

		public override void Clear()
		{
			if (spheresMr == null) return;
			foreach (MeshRenderer meshRenderer in spheresMr)
				Object.Destroy(meshRenderer);

			spheresMr = Array.Empty<MeshRenderer>();
		}

		public void MoveSeed(int index, Vector2 newPos) =>
			spheresMr[index].transform.localPosition = newPos.ToV3xz().WithY(heightOffset);

		public void ProjectOnTerrain(Terrain terrain)
		{
			foreach (MeshRenderer sphere in spheresMr)
			{
				Vector3 pos = sphere.transform.position;
				pos.y = terrain.SampleHeight(pos) + heightOffset;
				sphere.transform.position = pos;
			}
		}
	}
}

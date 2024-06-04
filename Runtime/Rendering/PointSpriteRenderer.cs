using System;
using System.Linq;
using DavidUtils.ExtensionMethods;
using DavidUtils.Sprites;
using UnityEngine;

namespace DavidUtils.Rendering
{
	[Serializable]
	public class PointSpriteRenderer : DynamicRenderer<Vector2[]>
	{
		protected override string DefaultChildName => "Sprite";

		public Texture2D spriteTexture;
		public SpriteRenderer[] spriteRenderers = Array.Empty<SpriteRenderer>();

		public float spriteScale = 1;

		/// <summary>
		///     Instancia SpriteRenderers en los puntos dados
		/// </summary>
		public override void Instantiate(Vector2[] polygons, string childName = null)
		{
			if (polygons.Length == 0) return;

			spriteRenderers ??= Array.Empty<SpriteRenderer>();
			// Concat to SpriteRenderers
			spriteRenderers = spriteRenderers
				.Concat(
					new SpriteRenderer[polygons.Length]
						.FillBy(
							i => InstantiateSprite(polygons[i], $"{childName ?? DefaultChildName} {i}")
						)
				)
				.ToArray();
		}

		/// <summary>
		///     Update All SpriteRenderers' positions
		/// </summary>
		public override void UpdateGeometry(Vector2[] triangles)
		{
			// Faltan o sobran MeshRenderers para las seeds dadas
			if (spriteRenderers.Length != triangles.Length)
			{
				Clear();
				Instantiate(triangles);
				return;
			}

			// Actualiza la posición de las semillas
			for (var i = 0; i < spriteRenderers.Length; i++)
				spriteRenderers[i].transform.localPosition = triangles[i];
		}

		/// <summary>
		///     Actualiza la posicion de un Sprite Renderer
		/// </summary>
		public void UpdateSpriteRenderer(int i, Vector2 point) =>
			spriteRenderers[i].transform.localPosition = point;


		public override void Clear()
		{
			base.Clear();

			if (spriteRenderers == null) return;
			foreach (SpriteRenderer sr in spriteRenderers)
				UnityUtils.DestroySafe(sr);

			spriteRenderers = Array.Empty<SpriteRenderer>();
		}

		private SpriteRenderer InstantiateSprite(Vector2 point, string spriteName = null)
		{
			SpriteRenderer sr = SpriteGenerator.InstantiateSpriteRenderer(
				spriteTexture,
				transform,
				$"{spriteName}"
			);

			// Actualiza posicion y escala
			Transform spriteTransform = sr.transform;
			spriteTransform.localPosition = point;

			// Compensa el Scale Global para verse siempre del mismo tamaño
			spriteTransform.SetGlobalScale(Vector3.one * spriteScale);

			return sr;
		}

		public void ProjectOnTerrain(Terrain terrain)
		{
			foreach (SpriteRenderer sr in spriteRenderers)
			{
				Vector3 pos = sr.transform.position;
				pos.y = terrain.SampleHeight(pos) + .1f;
				sr.transform.position = pos;
			}
		}
	}
}

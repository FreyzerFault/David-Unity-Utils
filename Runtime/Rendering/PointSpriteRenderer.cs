using System;
using System.Collections.Generic;
using System.Linq;
using DavidUtils.ExtensionMethods;
using DavidUtils.Sprites;
using UnityEngine;

namespace DavidUtils.Rendering
{
	[Serializable]
	public class PointSpriteRenderer : Points2DRenderer
	{
		protected override string DefaultChildName => "Sprite";

		public Texture2D spriteTexture;
		public SpriteRenderer[] spriteRenderers = Array.Empty<SpriteRenderer>();

		public float spriteScale = 1;

		/// <summary>
		///     Instancia SpriteRenderers en los puntos dados
		/// </summary>
		public override void Instantiate(IEnumerable<Vector2> points, string childName = null) => spriteRenderers =
			points.Select(
					(p, i) => InstantiateSprite(p, $"{childName ?? DefaultChildName} {i}")
				)
				.ToArray();

		/// <summary>
		///     Update All SpriteRenderers' positions
		/// </summary>
		public override void UpdateGeometry(IEnumerable<Vector2> points)
		{
			// Faltan o sobran MeshRenderers para las seeds dadas
			if (points.Count() != spriteRenderers.Length)
			{
				Clear();
				Instantiate(points);
				return;
			}

			// Actualiza las posiciones
			points.ForEach((t, i) => spriteRenderers[i].transform.localPosition = t);
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

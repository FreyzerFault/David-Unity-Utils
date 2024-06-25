using System;
using System.Collections.Generic;
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
		public List<SpriteRenderer> spriteRenderers = new();

		/// <summary>
		///     Instancia SpriteRenderers en los puntos dados
		/// </summary>
		public override void Instantiate(IEnumerable<Vector2> inGeometry, string childName = null) =>
			inGeometry.ForEach((p, i) => InstantiateSprite(p, $"{childName ?? DefaultChildName} {i}"));

		/// <summary>
		///     Update All SpriteRenderers' positions
		/// </summary>
		public override void UpdateGeometry(IEnumerable<Vector2> inGeometry) =>
			// Actualiza las posiciones o añade si no existe
			inGeometry.ForEach(
				(p, i) =>
				{
					if (i >= spriteRenderers.Count) InstantiateSprite(p, $"{DefaultChildName} {i}");
					else UpdateSpriteRenderer(i, p);
				}
			);

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

			// foreach (SpriteRenderer sr in spriteRenderers) sr.gameObject.SetActive(false);

			spriteRenderers = new List<SpriteRenderer>();
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
			spriteTransform.SetGlobalScale(Scale);
			sr.drawMode = SpriteDrawMode.Sliced;
			sr.size = Vector2.one;

			spriteRenderers.Add(sr);

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

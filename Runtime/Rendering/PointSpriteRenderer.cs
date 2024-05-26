using System;
using DavidUtils.ExtensionMethods;
using DavidUtils.Sprites;
using UnityEngine;

namespace DavidUtils.Rendering
{
	[Serializable]
    public class PointSpriteRenderer : DynamicRenderer<Vector2[]>
    {
	    public Texture2D spriteTexture;
		public SpriteRenderer[] spriteRenderers = Array.Empty<SpriteRenderer>();

		public float spriteScale = .3f;
		private float heightOffset = .2f;

		protected override string DefaultName => "Sprites Renderer";
		protected override string DefaultChildName => "Sprite";

		// protected override Material Material => Resources.Load<Material>("Materials/Geometry Unlit");

		public override void Instantiate(Vector2[] points, string childName = null)
		{
			if (points.Length == 0) return;

			if (spriteRenderers.Length != 0) Clear();

			spriteRenderers = new SpriteRenderer[points.Length];
			for (var i = 0; i < points.Length; i++)
			{
				Vector2 point = points[i];
				
				spriteRenderers[i] = SpriteGenerator.InstantiateSpriteRenderer(spriteTexture, RenderParent, $"{childName} {i}");

				// Actualiza posicion y escala
				Transform spriteTransform = spriteRenderers[i].transform;
				spriteTransform.SetLocalPositionAndRotation(
					point.ToV3xz().WithY(heightOffset),
					Quaternion.identity
				);
				// Compensa el Scale Global para verse siempre del mismo tamaño
				spriteTransform.SetGlobalScale(Vector3.one * spriteScale / spriteTransform.lossyScale.x);
			}

			UpdateVisibility();
		}

		public override void Update(Vector2[] points)
		{
			if (!active) return;

			// Faltan o sobran MeshRenderers para las seeds dadas
			if (spriteRenderers.Length != points.Length)
			{
				Clear();
				Instantiate(points);
				return;
			}

			// Actualiza la posición de las semillas
			for (var i = 0; i < spriteRenderers.Length; i++)
				spriteRenderers[i].transform.localPosition = points[i].ToV3xz().WithY(heightOffset);

			UpdateVisibility();
		}

		public override void Clear()
		{
			if (spriteRenderers == null) return;
			foreach (SpriteRenderer sr in spriteRenderers)
				UnityUtils.DestroySafe(sr);

			spriteRenderers = Array.Empty<SpriteRenderer>();
		}

		public void MovePoint(int index, Vector2 newPos) =>
			spriteRenderers[index].transform.localPosition = newPos.ToV3xz().WithY(heightOffset);

		public void ProjectOnTerrain(Terrain terrain)
		{
			foreach (SpriteRenderer sr in spriteRenderers)
			{
				Vector3 pos = sr.transform.position;
				pos.y = terrain.SampleHeight(pos) + heightOffset;
				sr.transform.position = pos;
			}
		}
    }
}

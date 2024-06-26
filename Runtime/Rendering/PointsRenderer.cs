using System;
using System.Linq;
using DavidUtils.DevTools.CustomAttributes;
using DavidUtils.ExtensionMethods;
using DavidUtils.Rendering.Extensions;
using UnityEngine;

namespace DavidUtils.Rendering
{
	[Serializable]
	public class PointsRenderer : DynamicRenderer<float>
	{
		protected override string DefaultChildName => "Sphere";
		protected MeshRenderer[] MeshRenderers => renderObjs.Select(obj => obj.GetComponent<MeshRenderer>()).ToArray();


		#region RADIUS

		[Range(0.1f, 1)] [SerializeField]
		private float radius = .5f;
		public float Radius
		{
			get => radius;
			set
			{
				radius = value;
				renderObjs.ForEach(obj => obj.transform.SetGlobalScale(Scale));
			}
		}

		public Vector3 Scale => Vector3.one * (radius + (IsCirle ? thickness / 2 : 0));

		#endregion


		#region TEXTURE

		private int textureSize = 20;

		private Texture2D CircleTexture =>
			TextureUtils.GenerateCircleTexture(textureSize, Color.white, radius, thickness);

		private Texture2D PointTexture => TextureUtils.GeneratePointTexture(textureSize, Color.white);

		#endregion


		#region SPRITES

		private Sprite circleSprite;
		private Sprite CircleSprite => circleSprite ??= BuildCircleSprite();

		private Sprite BuildCircleSprite() =>
			circleSprite = Sprite.Create(
				CircleTexture,
				new Rect(0, 0, 1, 1),
				new Vector2(0.5f, 0.5f)
			);

		private Sprite pointSprite;
		private Sprite PointSprite => pointSprite ??= Sprite.Create(
			PointTexture,
			new Rect(0, 0, 1, 1),
			new Vector2(0.5f, 0.5f)
		);

		#endregion


		#region THICKNESS

		[ConditionalField("IsCircle")] [SerializeField]
		private float thickness;
		public float Thickness
		{
			get => thickness;
			set
			{
				thickness = value;
				UpdateThickness();
			}
		}

		private float ExternalRadius => radius + thickness / 2;

		private void UpdateThickness()
		{
			// Rebuild the Sprite
			BuildCircleSprite();
			renderObjs.ForEach(circle => circle.GetComponent<SpriteRenderer>().sprite = CircleSprite);
		}

		#endregion


		#region RENDER MODE

		public enum RenderMode { Sphere, Circle, Point }

		private RenderMode renderMode;
		public RenderMode Mode
		{
			get => renderMode;
			set => renderMode = value;
		}

		private bool IsCirle => renderMode == RenderMode.Circle;

		#endregion


		private void Awake() => useColor = true;


		public override GameObject InstantiateObj(Vector3 pos, int i = -1, string objName = null)
		{
			GameObject obj = Mode switch
			{
				RenderMode.Sphere => InstantiateSphere(GetColor(i), Material),
				RenderMode.Circle => InstantiateCircle(GetColor(i), thickness),
				RenderMode.Point => InstantiatePoint(GetColor(i)),
				_ => throw new ArgumentOutOfRangeException()
			};

			if (obj == null) return obj;

			obj.name = $"{objName ?? DefaultChildName} {i}";

			// Actualiza posicion y escala
			Transform sphereTransform = obj.transform;
			sphereTransform.localPosition = pos;

			// Compensa el Scale Global para verse siempre del mismo tamaño
			obj.transform.SetGlobalScale(Scale);

			return obj;
		}


		#region OBJECT CREATION

		private GameObject InstantiateSphere(Color color, Material material) =>
			MeshRendererExtensions.InstantiateSphere(
				out MeshRenderer mr,
				out MeshFilter mf,
				transform,
				color: color,
				material: material
			);

		// TODO Mover la logica de creacion del objeto a un Generador de Objetos
		// TODO Aplicar Thickness al circulo
		/// <summary>
		///     Instancia un Círculo. Si le pasas thickness sera una circunferencia
		/// </summary>
		private static GameObject InstantiateCircle(Color color, float thickness = -1)
		{
			if (thickness < 0) return InstantiatePoint(color);
			var circle = Resources.Load<GameObject>("Prefabs/Circumference");
			circle.GetComponent<SpriteRenderer>().color = color;
			return circle;
		}

		/// <summary>
		///     Instancia un Circulo como un Punto
		/// </summary>
		private static GameObject InstantiatePoint(Color color)
		{
			var circle = Resources.Load<GameObject>("Prefabs/Circle");
			circle.GetComponent<SpriteRenderer>().color = color;
			return circle;
		}

		#endregion


		#region DEBUG

#if UNITY_EDITOR

		public bool drawGizmos;

		private void OnDrawGizmos()
		{
			if (!drawGizmos) return;

			Gizmos.color = colors?.Length > 0 ? colors[0] : Color.grey;
			for (var i = 0; i < renderObjs.Count; i++)
			{
				if (colors?.Length > 0)
					Gizmos.color = colors[i];

				Vector3 pos = transform.localToWorldMatrix.MultiplyPoint3x4(renderObjs[i].transform.position);
				Gizmos.DrawSphere(pos, radius);
			}
		}

#endif

		#endregion
	}
}

using System;
using System.Linq;
using DavidUtils.DevTools.CustomAttributes;
using DavidUtils.ExtensionMethods;
using DavidUtils.Geometry.MeshExtensions;
using UnityEngine;

namespace DavidUtils.Rendering
{
	[Serializable]
	public class PointsRenderer : DynamicRenderer<Renderer>
	{
		protected override string DefaultChildName => "Point";
		protected override Material Material => Resources.Load<Material>("Materials/Geometry Unlit");


		#region COMMON PROPS
		
		public enum RenderMode { Sphere, Circle, Point }

		private RenderMode _renderMode;
		public RenderMode Mode
		{
			get => _renderMode;
			set => _renderMode = value;
		}

		private bool IsCirle => _renderMode == RenderMode.Circle;
		
		[Range(0.1f, 20)] [SerializeField]
		private float radius = .5f;
		public float Radius
		{
			get => radius;
			set
			{
				radius = value;
				UpdateRadius();
			}
		}
		private void UpdateRadius() => renderObjs.ForEach(obj => obj.transform.SetGlobalScale(Scale));

		private Vector3 RadiusToScale(float radius) => Vector3.one * (radius + (IsCirle ? thickness / 2 : 0));
		public Vector3 Scale => RadiusToScale(radius);

		public void SetRadius(int i, float pointRadius) => 
			renderObjs.ElementAt(i).transform.SetGlobalScale(Vector3.one * (pointRadius * radius));

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

		private void UpdateThickness()
		{
			// TODO
		}

		private void UpdateProperties()
		{
			renderObjs.ForEach(SetCommonProperties);
		}

		protected override void SetCommonProperties(Renderer pointRenderer)
		{
			pointRenderer.transform.SetGlobalScale(Scale);
			pointRenderer.material = Material;
		}

		#endregion


		#region INDIVIDUAL PROPS

		protected override void UpdateColor()
		{
			switch (_renderMode)
			{
				case RenderMode.Sphere:
					renderObjs.ForEach((obj, i) => obj.GetComponent<MeshFilter>().mesh.SetColor(GetColor(i)));
					break;
				case RenderMode.Circle:
				case RenderMode.Point:
					renderObjs.ForEach((obj, i) => obj.GetComponent<SpriteRenderer>().color = GetColor(i));
					break;
			}
		}
		
		
		// RADIUS
		private float[] _radiusByPoint;

		public float[] RadiusByPoint
		{
			get => _radiusByPoint;
			set
			{
				_radiusByPoint = value;
				renderObjs.ForEach((obj, i) => 
					obj.transform.SetGlobalScale(RadiusToScale(value.Length > i ? value[i] : value[0])));
			}
		}

		#endregion
		
		

		#region SPRITES

		// TODO A ver si puedo meterle de input el tamaño de textura para adaptarlo por temas de rendimiento
		public int textureSize = 20;
		private Texture2D CircleTexture => TextureUtils.GetCircle();
		private Texture2D PointTexture => TextureUtils.GetCircumference();
		
		// TODO Cambiar esto por Prefabs. Va a ser mas eficiente
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



		protected virtual void Awake() => useColor = true;

		protected void OnValidate()
		{
			renderObjs.ForEach(SetCommonProperties);
		}

		public override Renderer InstantiateObj(Vector3? localPos = null, string objName = null, int i = -1)
		{
			if (i == -1) i = renderObjs.Count;
			if (i >= colors.Length) SetRainbowColors(i+1);
			Renderer renderObj = _renderMode switch
			{
				RenderMode.Sphere => InstantiateSphere(localPos, objName, GetColor(i)),
				RenderMode.Circle => InstantiateCircle(localPos, objName, GetColor(i)),
				RenderMode.Point => InstantiatePoint(localPos, objName, GetColor(i)),
				_ => null
			};
			SetCommonProperties(renderObj);
			
			return renderObj;
		}



		#region OBJECT CREATION

		private MeshRenderer InstantiateSphere(Vector3? localPos = null, string objName = null, Color? color = null)
		{
			GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
			sphere.name = objName ?? DefaultChildName;
			sphere.transform.parent = transform;
			sphere.transform.localPosition = localPos ?? Vector3.zero;
			sphere.GetComponent<MeshFilter>().mesh.SetColor(color ?? Color.white);
			return sphere.GetComponent<MeshRenderer>();
		}

		// TODO Mover la logica de creacion del Circulo y Punto a un Generador de Objetos
		// TODO Aplicar Thickness al circulo

		/// <summary>
		///     Instancia un Círculo. Si no thickness 0 o negativo, sera un punto
		/// </summary>
		private SpriteRenderer InstantiateCircle(Vector3? localPos = null, string objName = null, Color? color = null)
		{
			if (thickness < 0) return InstantiatePoint(localPos, objName);
			SpriteRenderer sr = Resources.Load<GameObject>("Prefabs/Circumference").GetComponent<SpriteRenderer>();
			sr.name = objName ?? DefaultChildName;
			sr.transform.localPosition = localPos ?? Vector3.zero;
			sr.color = color ?? Color.white;
			
			// TODO Modificar la mask hija para modificar el grosor con thickness
			
			return sr;
		}

		/// <summary>
		///     Instancia un Circulo como un Punto
		/// </summary>
		private SpriteRenderer InstantiatePoint(Vector3? localPos = null, string objName = null, Color? color = null)
		{
			SpriteRenderer sr = Resources.Load<GameObject>("Prefabs/Circle").GetComponent<SpriteRenderer>();
			sr.name = objName ?? DefaultChildName;
			sr.transform.localPosition = localPos ?? Vector3.zero;
			sr.color = color ?? Color.white;
			return sr;
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
		
		// Dibuja lineas a todos los objetos mientras este seleccionado
		private void OnDrawGizmosSelected()
		{
			if (!drawGizmos) return;
			
			Gizmos.color = Color.red;
			renderObjs.ForEach(r => Gizmos.DrawLine(transform.position, r.transform.position));
		}

#endif

		#endregion
	}
}

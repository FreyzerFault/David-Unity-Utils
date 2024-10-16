using System;
using System.Linq;
using DavidUtils.DevTools.CustomAttributes;
using DavidUtils.ExtensionMethods;
using DavidUtils.Geometry.MeshExtensions;
using UnityEngine;
using UnityEngine.Serialization;

namespace DavidUtils.Rendering
{
	[Serializable]
	public class PointsRenderer : DynamicRenderer<Renderer>
	{
		protected override string DefaultChildName => "Point";
		protected override Material SphereMaterial => Resources.Load<Material>("Materials/Geometry Unlit");

		protected override void Awake()
		{
			base.Awake();
			
			// IGNORE Other Renderers that are not SpriteRenderers or MeshRenderers
			renderObjs.RemoveAll(obj => obj.GetComponent<MeshRenderer>() == null && obj.GetComponent<SpriteRenderer>() == null);
		}


		#region COMMON PROPS
		
		public enum RenderMode { Sphere, Circle, Point }

		private RenderMode _renderMode;
		public RenderMode Mode
		{
			get => _renderMode;
			set
			{
				_renderMode = value;
				UpdateRenderMode();
			}
		}

		public void UpdateRenderMode()
		{
			Vector3[] positions = renderObjs.Select(r => r.transform.localPosition).ToArray();
			Clear();
			AddObjs(positions);
		}
		

		public bool IsCircle => _renderMode == RenderMode.Circle;
		
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
		public void UpdateRadius() => renderObjs.ForEach(obj => obj.GetComponent<SpriteRenderer>().size = new Vector2(radius, radius));

		private Vector3 RadiusToScale(float radius) => Vector3.one * (radius + (IsCircle ? pointThickness / 2 : 0));
		public Vector3 Scale => RadiusToScale(radius);

		public void SetRadius(int i, float pointRadius) => 
			renderObjs.ElementAt(i).transform.SetGlobalScale(Vector3.one * (pointRadius * radius));

		
		public Color PointColor
		{
			get => colors[0];
			set
			{
				colors[0] = value;
				UpdateColor();
			}
		}


		[SerializeField]
		private float pointThickness;
		public float PointThickness
		{
			get => pointThickness;
			set
			{
				pointThickness = value;
				UpdateThickness();
			}
		}

		private void UpdateThickness()
		{
			// TODO
		}

		private void UpdateProperties()
		{
			renderObjs.ForEach(UpdateCommonProperties);
		}

		protected override void UpdateCommonProperties(Renderer pointRenderer)
		{
			pointRenderer.transform.SetGlobalScale(Scale);

			switch (_renderMode)
			{
				case RenderMode.Sphere:
					pointRenderer.sharedMaterial = SphereMaterial;
					break;
				case RenderMode.Circle:
				case RenderMode.Point:
					pointRenderer.transform.localRotation = Quaternion.Euler(90,0,0);
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		#endregion


		#region INDIVIDUAL PROPS

		public override void UpdateColor()
		{
			switch (_renderMode)
			{
				case RenderMode.Sphere:
					renderObjs.ForEach((obj, i) => obj.GetComponent<MeshFilter>().sharedMesh.SetColor(GetColor(i)));
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

		
		public override Renderer InstantiateObj(Vector3? localPos = null, string objName = null, int i = -1)
		{
			if (i == -1) i = renderObjs.Count;
			if (i >= colors.Length && !singleColor) SetRainbowColors(i+1);
			Renderer renderObj = _renderMode switch
			{
				RenderMode.Sphere => InstantiateSphere(localPos, objName, GetColor(i)),
				RenderMode.Circle => InstantiateCircle(localPos, objName, GetColor(i)),
				RenderMode.Point => InstantiatePoint(localPos, objName, GetColor(i)),
				_ => null
			};
			UpdateCommonProperties(renderObj);
			
			return renderObj;
		}



		#region OBJECT CREATION

		private MeshRenderer InstantiateSphere(Vector3? localPos = null, string objName = null, Color? color = null)
		{
			GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
			sphere.name = objName ?? DefaultChildName;
			sphere.transform.parent = transform;
			sphere.transform.localPosition = localPos ?? Vector3.zero;
			sphere.GetComponent<MeshFilter>().sharedMesh.SetColor(color ?? Color.white);
			return sphere.GetComponent<MeshRenderer>();
		}

		// TODO Mover la logica de creacion del Circulo y Punto a un Generador de Objetos
		// TODO Aplicar Thickness al circulo

		/// <summary>
		///     Instancia un Círculo. Si no thickness 0 o negativo, sera un punto
		/// </summary>
		private SpriteRenderer InstantiateCircle(Vector3? localPos = null, string objName = null, Color? color = null)
		{
			if (pointThickness < 0) return InstantiatePoint(localPos, objName);
			
			GameObject srObj = Instantiate(
				Resources.Load<GameObject>("Prefabs/Circumference"),
				localPos ?? Vector3.zero,
				Quaternion.identity, 
				transform);
			SpriteRenderer sr = srObj.GetComponent<SpriteRenderer>();
			srObj.name = objName ?? DefaultChildName;
			srObj.transform.localPosition = localPos ?? Vector3.zero;
			sr.color = color ?? Color.white;
			
			// TODO Modificar la mask hija para modificar el grosor con thickness
			
			return sr;
		}

		/// <summary>
		///     Instancia un Circulo como un Punto
		/// </summary>
		private SpriteRenderer InstantiatePoint(Vector3? localPos = null, string objName = null, Color? color = null)
		{
			GameObject srObj = Instantiate(
				Resources.Load<GameObject>("Prefabs/Circle"),
				localPos ?? Vector3.zero,
				Quaternion.identity, 
				transform);
			SpriteRenderer sr = srObj.GetComponent<SpriteRenderer>();
			
			srObj.name = objName ?? DefaultChildName;
			srObj.transform.localPosition = localPos ?? Vector3.zero;
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

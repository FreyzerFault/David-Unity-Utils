using System;
using System.Linq;
using DavidUtils.DevTools.CustomAttributes;
using DavidUtils.ExtensionMethods;
using DavidUtils.Geometry.MeshExtensions;
using UnityEngine;
using UnityEngine.Serialization;

namespace DavidUtils.Rendering
{
	[ExecuteAlways][Serializable]
	public class PointsRenderer : DynamicRenderer<Renderer>
	{
		protected override string DefaultChildName => "Point";
		protected override Material SphereMaterial => Resources.Load<Material>("Materials/Geometry Unlit");

		[SerializeField] private GameObject spherePrefab;
		[SerializeField] private GameObject pointPrefab;
		[SerializeField] private GameObject circlePrefab;
		
		protected GameObject Prefab => renderMode switch
		{
			RenderMode.Sphere => spherePrefab ??= Resources.Load<GameObject>("Prefabs/Geometry/Sphere"),
			RenderMode.Point => pointPrefab ??= Resources.Load<GameObject>("Prefabs/Geometry/Circle"),
			RenderMode.Circle => circlePrefab ??= Resources.Load<GameObject>("Prefabs/Geometry/Circumference"),
			_ => null
		};

		protected override void Awake()
		{
			base.Awake();
			
			spherePrefab = Resources.Load<GameObject>("Prefabs/Geometry/Sphere");
			pointPrefab = Resources.Load<GameObject>("Prefabs/Geometry/Circle");
			circlePrefab = Resources.Load<GameObject>("Prefabs/Geometry/Circumference");
			
			// IGNORE Other Renderers that are not SpriteRenderers or MeshRenderers
			renderObjs.RemoveAll(obj => obj.GetComponent<MeshRenderer>() == null && obj.GetComponent<SpriteRenderer>() == null);
		}


		#region COMMON PROPS
		
		public enum RenderMode { Sphere, Circle, Point }

		[SerializeField] private RenderMode renderMode;
		public RenderMode Mode
		{
			get => renderMode;
			set
			{
				renderMode = value;
				UpdateRenderMode();
			}
		}

		public void UpdateRenderMode()
		{
			Vector3[] positions = renderObjs.Select(r => r.transform.localPosition).ToArray();
			Clear();
			AddObjs(positions);
			UpdateProperties();
		}
		

		public bool IsCircle => renderMode == RenderMode.Circle;
		
		[Range(0.1f, 20)] [SerializeField]
		private float radius = 1f;
		public float Radius
		{
			get => radius;
			set
			{
				radius = value;
				UpdateRadius();
			}
		}
		public void UpdateRadius() => renderObjs.ForEach(obj =>
		{
			switch (renderMode)
			{
				case RenderMode.Sphere:
					obj.transform.localScale = (Vector3.one * radius);
					break;
				case RenderMode.Circle:
				case RenderMode.Point:
					obj.GetComponent<SpriteRenderer>().size = new Vector2(radius, radius);
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		});

		private Vector3 RadiusToScale(float radius) => Vector3.one * (radius + (IsCircle ? pointThickness / 2 : 0));
		public Vector3 Scale => RadiusToScale(radius);

		public void SetRadius(int i, float newRadius)
		{
			if (renderMode == RenderMode.Sphere)
				renderObjs.ElementAt(i).transform.localScale = (Vector3.one * radius);
			else
				renderObjs.ElementAt(i).GetComponent<SpriteRenderer>().size = new Vector2(newRadius, newRadius);
		}


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
			UpdateRadius();
			UpdateColor();
			UpdateThickness();
		}
		
		protected override void UpdateCommonProperties(Renderer renderObj)
		{
			switch (renderMode)
			{
				case RenderMode.Sphere:
					renderObj.transform.localScale = Vector3.one * radius;
					renderObj.GetComponent<MeshFilter>().sharedMesh.SetColor(BaseColor);
					break;
				case RenderMode.Circle:
				case RenderMode.Point:
					renderObj.transform.localRotation = Quaternion.Euler(90,0,0);
					renderObj.GetComponent<SpriteRenderer>().color = BaseColor;
					renderObj.transform.localScale = Vector3.one;
					renderObj.GetComponent<SpriteRenderer>().size = new Vector2(radius, radius);
					break;
			}
		}

		#endregion


		#region INDIVIDUAL PROPS

		public override void UpdateColor()
		{
			switch (renderMode)
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
			
			GameObject obj = Instantiate(Prefab, localPos ?? Vector3.zero, Quaternion.identity, transform);
			obj.name = objName ?? DefaultChildName;
			
			Renderer renderObj = renderMode switch
			{
				RenderMode.Sphere => obj.GetComponent<MeshRenderer>(),
				RenderMode.Circle => obj.GetComponent<SpriteRenderer>(),
				RenderMode.Point => obj.GetComponent<SpriteRenderer>(),
				_ => null
			};
			
			UpdateCommonProperties(renderObj);
			
			return renderObj;
		}
		

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

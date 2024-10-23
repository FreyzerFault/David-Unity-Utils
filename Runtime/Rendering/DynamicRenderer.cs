using System;
using System.Collections.Generic;
using System.Linq;
using DavidUtils.DevTools.CustomAttributes;
using DavidUtils.ExtensionMethods;
using UnityEngine;
using UnityEngine.Serialization;

namespace DavidUtils.Rendering
{
	[Serializable]
	public abstract class DynamicRenderer<T> : MonoBehaviour where T : Component
	{
		protected virtual Material SphereMaterial => Resources.Load<Material>("Materials/Geometry Unlit");
		protected virtual string DefaultChildName => "Render Object";

		[SerializeField] public List<T> renderObjs = new();
		[SerializeField] public List<T> ignoredObjs = new();

		public bool Active
		{
			get => isActiveAndEnabled;
			set => gameObject.SetActive(value);
		}

		public virtual void ToggleVisibility(bool visible) => gameObject.SetActive(visible);

		public void UpdateCommonProperties() => renderObjs.ForEach(UpdateCommonProperties);
		protected abstract void UpdateCommonProperties(T renderObj);


		protected virtual void Awake() => UpdateRenderObjsFromChildren();

		
		private void UpdateRenderObjsFromChildren() => renderObjs =
			GetComponentsInChildren<T>()?.Where(obj => !ignoredObjs.Contains(obj)).ToList() ?? new List<T>();
			

		#region INSTANTIATION

		public void InstantiateObjs(IEnumerable<Vector2> localPositions, string objName = null) =>
			InstantiateObjs(localPositions.ToV3().ToArray(), objName);

		public void InstantiateObjs(IEnumerable<Vector3> localPositions, string objName = null) =>
			renderObjs = new List<T>(localPositions.Select((p, i) => InstantiateObj(p, $"{objName ?? DefaultChildName} {i}", i)));

		public virtual T InstantiateObj(Vector3? localPos = null, string objName = null, int i = -1)
		{
			var renderObj = UnityUtils.InstantiateObject<T>(transform, objName ?? DefaultChildName, localPos);
			UpdateCommonProperties(renderObj);
			if (ProjectedOnTerrain) ProjectOnTerrain();
			return renderObj;
		}

		public virtual T InstantiateObj<Tobj>(Vector3? localPos = null, string objName = null, int i = -1)
			where Tobj : T
		{
			var renderObj = UnityUtils.InstantiateObject<Tobj>(transform, objName ?? DefaultChildName, localPos);
			UpdateCommonProperties(renderObj);
			if (ProjectedOnTerrain) ProjectOnTerrain();
			return renderObj;
		}

		#endregion


		#region CRUD

		/// <summary>
		/// Añade un nuevo objeto en la posición indicada Instanciandolo
		/// </summary>
		public void AddObj(Vector3 pos) => renderObjs.Add(InstantiateObj(pos));
		public void AddObjs(IEnumerable<Vector2> pos) => renderObjs.AddRange(pos.Select(p => InstantiateObj(p)));
		public void AddObjs(IEnumerable<Vector3> pos) => renderObjs.AddRange(pos.Select(p => InstantiateObj(p)));
		
		/// <summary>
		/// Inserta el Objeto ordenado en index 
		/// </summary>
		public void InsertObj(int index, Vector3 pos) => renderObjs.Insert(index, InstantiateObj(pos));

		/// <summary>
		/// Elimina el GameObject de la Jerarquía de Unity y de la lista de renderObjs
		/// </summary>
		public void RemoveObj(int i)
		{
			if (i >= renderObjs.Count) return;
			UnityUtils.DestroySafe(renderObjs[i]);
			renderObjs.RemoveAt(i);
		}
		public void RemoveObjs(int i, int count)
		{
			if (i >= renderObjs.Count) return;
			UnityUtils.DestroySafe(renderObjs.Skip(i).Take(count));
			renderObjs.RemoveRange(i, count);
		}
		public void RemoveObjs(int[] indices)
		{
			UnityUtils.DestroySafe(renderObjs.FromIndices(indices));
			renderObjs = renderObjs.Where((_, i) => !indices.Contains(i)).ToList();
		}

		public void UpdateObj(int i, Vector3 pos) => renderObjs[i].transform.localPosition = pos;
		public void UpdateAllObj(IEnumerable<Vector2> pos) => UpdateAllObj(pos.ToV3());
		public void UpdateAllObj(IEnumerable<Vector3> pos)
		{
			IEnumerable<Vector3> localPositions = pos as Vector3[] ?? pos.ToArray();
			if (localPositions.Count() != renderObjs.Count)
			{
				if (localPositions.Count() < renderObjs.Count)
				{
					RemoveObjs(localPositions.Count(), renderObjs.Count - localPositions.Count());
					UpdateAllObj(localPositions);
				}
				else
				{
					UpdateAllObj(localPositions.Take(renderObjs.Count));
					AddObjs(localPositions.Skip(renderObjs.Count));
				}
			}
			else
				localPositions.ForEach((p, i) => UpdateObj(i, p));
		}

		public virtual void Clear()
		{
			if (renderObjs.IsNullOrEmpty()) UpdateRenderObjsFromChildren();
			if (renderObjs.IsNullOrEmpty()) return;
			UnityUtils.DestroySafe(renderObjs);
			renderObjs?.Clear();
			
			// Por si acaso eliminamos todos los hijos
			// Renderer[] notDeletedRenderers = GetComponentsInChildren<Renderer>();
			// if (notDeletedRenderers.NotNullOrEmpty())
			// 	notDeletedRenderers.ForEach(UnityUtils.DestroySafe);
		}

		#endregion


		#region COLOR

		protected static readonly Color DefaultColor = Color.white;

		public bool singleColor;

		[Serializable]
		public struct ColorPaletteData
		{
			public Color baseColor;
			[Min(-1)] public float paletteStep;
			[Min(0)] public int paletteRange;
		}
		
		[FormerlySerializedAs("colorPalette")] [SerializeField] private ColorPaletteData colorPaletteData = new()
		{
			baseColor = DefaultColor,
			paletteStep = .05f,
			paletteRange = 20
		};
		public ColorPaletteData ColorPalette
		{
			get => colorPaletteData;
			set
			{
				colorPaletteData = value;
				if (singleColor) Colors = BaseColor.ToFilledArray(colors.Length).ToArray();
				else SetRainbowColors(colors.Length);
			}
		}

		public Color BaseColor
		{
			get => colorPaletteData.baseColor;
			set
			{
				colorPaletteData.baseColor = value;
				if (singleColor) Colors = value.ToFilledArray(colors.Length).ToArray();
				else SetRainbowColors(colors.Length);
			}
		}

		[HideInInspector] public Color[] colors = Array.Empty<Color>();

		public Color[] Colors
		{
			get => colors;
			set
			{
				colors = value;
				UpdateColor();
			}
		}
		public abstract void UpdateColor();

		protected Color GetColor(int i = -1) => 
			singleColor || colors.IsNullOrEmpty() 
				? BaseColor
				: i >= 0 && i < colors.Length 
					? colors[i] 
					: colors[0];

		public Color[] SetRainbowColors(int numColors, Color? baseColor = null)
		{
			if (baseColor.HasValue) BaseColor = baseColor.Value;
			return Colors = BaseColor
				.GetRainBowColors(numColors, colorPaletteData.paletteStep, colorPaletteData.paletteRange)
				.ToArray();
		}

		#endregion


		#region SHADOWS

		public void ToggleShadows(bool castShadows = true) =>
			gameObject.ToggleShadows(castShadows);

		#endregion


		#region TERRAIN

		public bool projectedOnTerrain;
		public float terrainHeightOffset = 0.1f;
		private (Vector3, Quaternion, Vector3)[] originalTransforms; // For UNDO Projection on Terrain

		public bool ProjectedOnTerrain
		{
			get => projectedOnTerrain && Terrain != null;
			set
			{
				projectedOnTerrain = value;
				if (projectedOnTerrain && Terrain != null) ProjectOnTerrain();
				else UndoProjectOnTerrain();
			}
		}
		protected Terrain Terrain => Terrain.activeTerrain;

		protected virtual void ProjectOnTerrain()
		{
			if (!ProjectedOnTerrain) return;
			
			// Save old original transforms
			originalTransforms = renderObjs.Select(r => 
				(r.transform.localPosition, r.transform.localRotation, r.transform.localScale))
				.ToArray();
			
			// Project all
			Terrain terrain = Terrain;
			renderObjs.ForEach(r => r.transform.position = terrain.Project(r.transform.position) + Vector3.up * terrainHeightOffset);
		}

		protected virtual void UndoProjectOnTerrain()
		{
			if (originalTransforms == null) return;
			renderObjs.ForEach((r,i) =>
			{
				(Vector3, Quaternion, Vector3) original = originalTransforms[i];
				r.transform.SetLocalPositionAndRotation(original.Item1, original.Item2);
				r.transform.localScale = original.Item3;
			});
		}

		#endregion
	}
}

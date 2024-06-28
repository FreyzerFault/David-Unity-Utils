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
		protected virtual Material Material => Resources.Load<Material>("Materials/Geometry Unlit");
		protected virtual string DefaultChildName => "Render Object";

		public List<T> renderObjs = new();

		public bool Active
		{
			get => isActiveAndEnabled;
			set => gameObject.SetActive(value);
		}

		public virtual void ToggleVisibility(bool visible) => gameObject.SetActive(visible);

		protected abstract void SetCommonProperties(T renderObj);


		#region INSTANTIATION

		public void InstantiateObjs(IEnumerable<Vector2> localPositions, string objName = null) =>
			InstantiateObjs(localPositions.ToV3().ToArray(), objName);

		public void InstantiateObjs(IEnumerable<Vector3> localPositions, string objName = null) =>
			renderObjs = new List<T>(localPositions.Select((p, i) => InstantiateObj(p, $"{objName ?? DefaultChildName} {i}", i)));

		public virtual T InstantiateObj(Vector3? localPos = null, string objName = null, int i = -1)
		{
			var renderObj = UnityUtils.InstantiateObject<T>(transform, objName ?? DefaultChildName, localPos);
			SetCommonProperties(renderObj);
			return renderObj;
		}

		#endregion


		#region CRUD

		public void AddObj(Vector3 pos) => renderObjs.Add(InstantiateObj(pos));
		public void AddObjs(IEnumerable<Vector2> pos) => renderObjs.AddRange(pos.Select(p => InstantiateObj(p)));
		public void AddObjs(IEnumerable<Vector3> pos) => renderObjs.AddRange(pos.Select(p => InstantiateObj(p)));

		public void RemoveObj(int i)
		{
			UnityUtils.DestroySafe(renderObjs[i]);
			renderObjs.RemoveAt(i);
		}

		public void UpdateObj(int i, Vector3 pos) => renderObjs[i].transform.localPosition = pos;
		public void UpdateAllObj(IEnumerable<Vector2> pos) => UpdateAllObj(pos.ToV3());
		public void UpdateAllObj(IEnumerable<Vector3> pos)
		{
			IEnumerable<Vector3> localPositions = pos as Vector3[] ?? pos.ToArray();
			if (localPositions.Count() != renderObjs.Count) InstantiateObjs(localPositions);
			localPositions.ForEach((p, i) => UpdateObj(i, p));
		}

		public virtual void Clear()
		{
			renderObjs.ForEach(UnityUtils.DestroySafe);
			renderObjs.Clear();
		}

		#endregion


		#region COLOR

		private static readonly Color DefaultColor = Color.white;

		public bool useColor = true;

		[Serializable]
		private struct ColorData
		{
			[FormerlySerializedAs("initColorPalette")] public Color initialColor;
			[FormerlySerializedAs("paletteStep")] [FormerlySerializedAs("colorPaletteStep")]
			[Min(-1)] public float palletteStep;
			[FormerlySerializedAs("paletteRange")] [FormerlySerializedAs("colorPaletteRange")]
			[Min(0)] public int palletteRange;
		}

		[ConditionalField("useColor")]
		[SerializeField] private ColorData colorPallette = new()
		{
			initialColor = DefaultColor,
			palletteStep = .05f,
			palletteRange = 20
		};

		public Color InitialColor
		{
			get => colorPallette.initialColor;
			set
			{
				colorPallette.initialColor = value;
				SetRainbowColors(colors.Length);
			}
		}

		[HideInInspector] public Color[] colors = Array.Empty<Color>();

		protected Color GetColor(int i = -1) => i >= 0 && i < colors.Length ? colors[i] : DefaultColor;

		public Color[] SetRainbowColors(int numColors, Color? initColor = null) =>
			colors = (initColor ?? colorPallette.initialColor)
				.GetRainBowColors(numColors, colorPallette.palletteStep, colorPallette.palletteRange)
				.ToArray();

		#endregion


		#region SHADOWS

		public void ToggleShadows(bool castShadows = true) =>
			gameObject.ToggleShadows(castShadows);

		#endregion


		#region TERRAIN

		public virtual void ProjectOnTerrain(Terrain terrain)
		{
			foreach (T pointObj in renderObjs)
				pointObj.transform.position = terrain.Project(pointObj.transform.position);
		}

		#endregion
	}
}

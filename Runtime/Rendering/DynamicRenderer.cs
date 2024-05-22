using System;
using System.Linq;
using DavidUtils.ExtensionMethods;
using UnityEngine;

namespace DavidUtils.Rendering
{
	[Serializable]
	public abstract class DynamicRenderer<T>
	{
		public GameObject renderObj;
		public Transform RenderParent => renderObj.transform;

		public bool active = true;

		public bool IsInitialized => renderObj != null;

		protected virtual string DefaultName => "Render Obj";
		protected virtual Material Material => Resources.Load<Material>("Materials/Geometry Unlit");

		public virtual void Initialize(Transform parent, string name = null) =>
			renderObj = new GameObject(name ?? DefaultName)
			{
				transform =
				{
					parent = parent,
					localPosition = Vector3.zero,
					localRotation = Quaternion.identity,
					localScale = Vector3.one
				}
			};

		public abstract void Instantiate(T data);
		public abstract void Update(T data);

		public abstract void Clear();

		public virtual void UpdateVisibility() => renderObj.SetActive(active);

		#region COLOR

		public Color[] colors = Array.Empty<Color>();
		public Color DefaultColor => Color.cyan;

		protected Color[] SetRainbowColors(int numColors, Color? initColor = null) =>
			colors = (initColor ?? DefaultColor).Darken(.3f).GetRainBowColors(numColors, .005f, 20).ToArray();

		#endregion
	}
}

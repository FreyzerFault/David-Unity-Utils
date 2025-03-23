using System;
using DavidUtils.ExtensionMethods;
using UnityEngine;

namespace DavidUtils.Rendering
{
	[Serializable]
	public class SpritesRenderer : DynamicRenderer<SpriteRenderer>
	{
		protected override string DefaultChildName => "Sprite";

		#region COMMON PROPS

		[SerializeField] private Sprite sprite;
		public Sprite Sprite
		{
			get => sprite;
			set
			{
				sprite = value;
				UpdateTextures();
			}
		}
		private void UpdateTextures() => renderObjs.ForEach(sr => sr.sprite = sprite);

		[SerializeField]
		private float size = 1;
		public float Size
		{
			get => size;
			set
			{
				size = value;
				UpdateSize();
			}
		}
		private void UpdateSize() => renderObjs.ForEach(sr => sr.transform.SetGlobalScale(Vector3.one * size));

		protected override void UpdateCommonProperties(SpriteRenderer sr)
		{
			sr.sprite = sprite;
			sr.transform.SetGlobalScale(Vector3.one * Size);
			sr.drawMode = SpriteDrawMode.Sliced;
			sr.size = Vector2.one;
		}

		#endregion


		#region SINGLE PROPS
		
		public override void UpdateColor() => 
			renderObjs.ForEach((r,i) => r.color = GetColor(i));

		public void SetSize(int i, float newSize) => renderObjs[i].transform.SetGlobalScale(Vector3.one * newSize);

		#endregion
		
		#region DEBUG

#if UNITY_EDITOR
		
		public bool drawGizmos;
		
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

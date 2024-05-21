using DavidUtils.CameraUtils;
using DavidUtils.DebugUtils;
using DavidUtils.ExtensionMethods;
using DavidUtils.PlayerControl;
using UnityEngine;

namespace DavidUtils
{
	public class BillboardObject : MonoBehaviour
	{
		protected static Player Player => Player.Instance;
		protected static Camera Camera => CameraManager.MainCam;

		public bool verticalLock;

		public bool showColliderNearPlayer = true;

		private SpriteRenderer _spriteRenderer;
		private SpriteRenderer SpriteRenderer =>
			_spriteRenderer != null ? _spriteRenderer : GetComponent<SpriteRenderer>();

		protected Sprite Sprite
		{
			get => SpriteRenderer.sprite;
			set => SpriteRenderer.sprite = value;
		}

		private void Update() => transform.Billboard(Camera.transform, verticalLock);

		private void OnDrawGizmos()
		{
			if (!showColliderNearPlayer) return;

			if (Vector3.Distance(transform.position, Player.Position) > 10) return;

			var col = GetComponent<Collider>();
			if (col == null) return;

			Vector3 pos = col.bounds.center;
			float radius = col.bounds.size.x / 2;
			float height = col.bounds.size.y;
			Color color = Color.red;
			// GizmosExtensions.DrawCilinder(pos, radius, height, transform.rotation, 2, color);
			GizmosExtensions.DrawCilinderWire(radius, height, transform.localToWorldMatrix, 2, 2, color);
		}
	}
}

using DavidUtils.CameraUtils;
using DavidUtils.DevTools.GizmosAndHandles;
using DavidUtils.ExtensionMethods;
using DavidUtils.PlayerControl;
using UnityEngine;

namespace DavidUtils.Billboard
{
	public class BillboardObject : MonoBehaviour
	{
		private Player player;
		private Vector3 playerPos = Vector3.zero;

		protected static Player Player => Player.Instance ?? FindObjectOfType<Player>();
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

		private void Awake() => player = Player.Instance ?? FindObjectOfType<Player>();

		private void Update()
		{
			playerPos = player != null ? player.Position : FindObjectOfType<Player>().transform.position;
			transform.Billboard(Camera.transform, verticalLock);
		}

		
		#region DEBUG

		#if UNITY_EDITOR
		
		private void OnDrawGizmos()
		{
			if (!showColliderNearPlayer) return;

			if (Vector3.Distance(transform.position, playerPos) > 10) return;

			var col = GetComponent<Collider>();
			if (col == null) return;

			Vector3 pos = col.bounds.center;
			float radius = col.bounds.size.x / 2;
			float height = col.bounds.size.y;
			Color color = Color.red;
			// GizmosExtensions.DrawCilinder(pos, radius, height, transform.rotation, 2, color);
			GizmosExtensions.DrawCilinderWire(radius, height, transform.localToWorldMatrix, 2, 2, color);
		}
		
		#endif

		#endregion
	}
}

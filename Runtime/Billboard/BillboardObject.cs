using DavidUtils.Camera;
using DavidUtils.DevTools.GizmosAndHandles;
using DavidUtils.Utils;
using UnityEngine;

namespace DavidUtils.Billboard
{
	public class BillboardObject : MonoBehaviour
	{
		private Player.PlayerController playerController;
		private Vector3 _playerPos = Vector3.zero;

		protected static Player.PlayerController PlayerController => DavidUtils.Player.PlayerController.Instance ?? FindFirstObjectByType<Player.PlayerController>();
		protected static UnityEngine.Camera Camera => CameraManager.MainCam;

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

		private void Awake() => playerController = DavidUtils.Player.PlayerController.Instance ?? FindFirstObjectByType<Player.PlayerController>();

		private void Update()
		{
			_playerPos = playerController != null ? playerController.Position : FindFirstObjectByType<Player.PlayerController>().transform.position;
			transform.Billboard(Camera.transform, verticalLock);
		}

		
		#region DEBUG

		#if UNITY_EDITOR
		
		private void OnDrawGizmos()
		{
			if (!showColliderNearPlayer) return;

			if (Vector3.Distance(transform.position, _playerPos) > 10) return;

			Collider col = GetComponent<Collider>();
			if (col == null) return;

			// Vector3 pos = col.bounds.center;
			// GizmosExtensions.DrawCilinder(pos, radius, height, transform.rotation, 2, color);
			
			float radius = col.bounds.size.x / 2;
			float height = col.bounds.size.y;
			Color color = Color.red;
			GizmosExtensions.DrawCilinderWire(radius, height, transform.localToWorldMatrix, 2, 2, color);
		}
		
		#endif

		#endregion
	}
}

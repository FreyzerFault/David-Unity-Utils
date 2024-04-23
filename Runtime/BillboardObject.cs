using DavidUtils.DebugExtensions;
using DavidUtils.ExtensionMethods;
using DavidUtils.PlayerControl;
using UnityEngine;

namespace DavidUtils
{
	public class BillboardObject : MonoBehaviour
	{
		protected Player player;
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

		protected virtual void Awake() => player = FindObjectOfType<Player>();

		private void Update() => transform.Billboard(player.transform, verticalLock);

		private void OnDrawGizmos()
		{
			if (!showColliderNearPlayer) return;
			
			if (Vector3.Distance(transform.position, player.Position) > 10) return;
			
			var col = GetComponent<Collider>();
			if (col == null) return;

			Vector3 pos = col.bounds.center + Vector3.down * (col.bounds.size.y / 2);
			float radius = col.bounds.size.x / 2; 
			float height = col.bounds.size.y;
			Color color = Color.red;
			GizmosExtensions.DrawCilinderWire(pos, radius, height, transform.rotation, 2, color);
		}
	}
}

using DavidUtils.ExtensionMethods;
using DavidUtils.PlayerControl;
using UnityEngine;

namespace DavidUtils
{
	public class BillboardObject : MonoBehaviour
	{
		protected Player player;
		public bool verticalLock;

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
	}
}

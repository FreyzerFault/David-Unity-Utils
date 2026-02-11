using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DavidUtils.Billboard
{
	// Swap between different sprites dependant on the player distance to Obj
	public class BillboardObj_DynamicRes : BillboardObject
	{
		private float DistanceToPlayer => Vector3.Distance(transform.position, PlayerController.transform.position);

		[Serializable]
		public struct SpriteWithRes
		{
			public float maxDistance;
			public Sprite sprite;

			public KeyValuePair<float, Sprite> ToKeyValuePair() => new(maxDistance, sprite);
		}

		public List<SpriteWithRes> sprites = new();
		private Dictionary<float, Sprite> _spritesDic = new();

		protected virtual void Awake() => _spritesDic = new Dictionary<float, Sprite>(
			new List<KeyValuePair<float, Sprite>>(
				sprites.ConvertAll(s => s.ToKeyValuePair())
			)
		);

		private void Start() => PlayerController.OnPlayerMove += HandlePlayerControllerMove;

		private void HandlePlayerControllerMove(Vector2 moveInput) =>
			Sprite = GetSpriteByDistance(DistanceToPlayer);

		private Sprite GetSpriteByDistance(float distance)
		{
			Sprite[] spritesInDistance = _spritesDic.Where(keyValue => distance <= keyValue.Key).Select(pair => pair.Value).ToArray();
			
			foreach (Sprite sprite in spritesInDistance) return sprite;

			return sprites[^1].sprite;
		}
	}
}

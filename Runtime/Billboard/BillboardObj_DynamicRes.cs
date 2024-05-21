using System;
using System.Collections.Generic;
using UnityEngine;

namespace DavidUtils
{
	// Swap between different sprites dependant on the player distance to Obj
	public class BillboardObj_DynamicRes : BillboardObject
	{
		private float DistanceToPlayer => Vector3.Distance(transform.position, Player.transform.position);

		[Serializable]
		public struct SpriteWithRes
		{
			public float maxDistance;
			public Sprite sprite;

			public KeyValuePair<float, Sprite> ToKeyValuePair() => new(maxDistance, sprite);
		}

		public List<SpriteWithRes> sprites = new();
		private Dictionary<float, Sprite> _spritesDic = new();

		protected virtual void Awake()
		{
			_spritesDic = new Dictionary<float, Sprite>(
				new List<KeyValuePair<float, Sprite>>(
					sprites.ConvertAll(s => s.ToKeyValuePair())
				)
			);
		}

		private void Start() => Player.OnPlayerMove += HandlePlayerMove;

		private void HandlePlayerMove(Vector2 moveInput) =>
			Sprite = GetSpriteByDist(DistanceToPlayer);

		private Sprite GetSpriteByDist(float dist)
		{
			foreach (KeyValuePair<float, Sprite> keyValue in _spritesDic)
			{
				if (dist > keyValue.Key) continue;
				return keyValue.Value;
			}

			return sprites[^1].sprite;
		}
	}
}

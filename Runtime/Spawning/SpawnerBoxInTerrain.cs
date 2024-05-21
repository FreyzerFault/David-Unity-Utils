using DavidUtils.ExtensionMethods;
using DavidUtils.TerrainExtensions;
using UnityEngine;

namespace DavidUtils.Spawning
{
	public class SpawnerBoxInTerrain : SpawnerBox
	{
		private Terrain _terrain;

		protected override void Awake()
		{
			base.Awake();
			_terrain = Terrain.activeTerrain;
			ignoreHeight = true;
		}

		public override Spawneable SpawnRandom(bool spawnWithRandomRotation = true)
		{
			if (_terrain == null) return base.SpawnRandom(spawnWithRandomRotation);

			return spawnWithRandomRotation
				? Spawn(GetRandomPosInTerrain(), Quaternion.Euler(0, Random.Range(-180, 180), 0))
				: Spawn(GetRandomPosInTerrain());
		}

		protected Vector3 GetRandomPosInTerrain()
		{
			Vector3 pos = box.bounds.GetRandomPointInBounds(offset, ignoreHeight);
			pos.y = _terrain.GetInterpolatedHeight(pos) + offset.y;
			return pos;
		}
	}
}

using System.Collections;
using DavidUtils.ExtensionMethods;
using UnityEngine;

namespace DavidUtils.Spawning
{
	public class SpawnerBox : Spawner
	{
		[Space]
		public Bounds bounds = new(Vector3.zero, Vector3.one);

		public Vector3 offset = new(0, 0, 0);
		public bool ignoreHeight;

		public Vector3 Center
		{
			get => bounds.center;
			set => bounds.center = value;
		}
		public Vector3 Size
		{
			get => bounds.size;
			set => bounds.size = value;
		}

		protected override void Start()
		{
			for (var i = 0; i < initialNumItems; i++) SpawnRandom();
		}

		// Spawnea el objeto de forma random dentro de la caja
		// spawnWithRandomRotation = true -> Randomiza la rotacion en el Eje Y
		public virtual Spawneable SpawnRandom(bool spawnWithRandomRotation = true)
		{
			Vector3 position = bounds.GetRandomPointInBounds(offset, ignoreHeight);

			return Spawn(
				position,
				spawnWithRandomRotation
					? Quaternion.Euler(0, Random.Range(-180, 180), 0)
					: null
			);
		}

		// Spawnea el objeto sacandolo de la Pool
		protected override Spawneable Spawn(Vector3? position = null, Quaternion? rotation = null)
		{
			// Lo spawnwea en el centro si no se ha elegido posicion
			Spawneable obj = base.Spawn(position ?? Center, rotation);

			return obj;
		}

		protected override IEnumerator SpawnCoroutine()
		{
			while (true)
			{
				yield return new WaitForSeconds(spawnFrequency);

				for (var i = 0; i < burstSpawn; i++)
					SpawnRandom();
			}
		}
	}
}

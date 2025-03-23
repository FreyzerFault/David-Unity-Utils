using System.Collections;
using DavidUtils.ExtensionMethods;
using DavidUtils.Geometry.Bounding_Box;
using UnityEngine;

namespace DavidUtils.Spawning
{
	public class SpawnerBox : Spawner
	{
		[Space]
		public Bounds bounds = new(Vector3.zero, Vector3.one);
		protected AABB_2D AABB2D => new(bounds);

		public Vector3 offset = new(0, 0, 0);
		public Vector2 Offset2D => offset.ToV2(isXZplane);

		public bool is2D;
		public bool isXZplane = true;

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

		public Vector2 Center2D
		{
			get => bounds.center.ToV2(isXZplane);
			set => bounds.center = value.ToV3(isXZplane);
		}

		protected Quaternion RandomRotation =>
			isXZplane
				? Quaternion.Euler(0, Random.Range(-180, 180), 0)
				: Quaternion.Euler(0, 0, Random.Range(-180, 180));

		protected override void Start()
		{
			for (var i = 0; i < initialNumItems; i++) SpawnRandom();
		}

		// Spawnea el objeto sacandolo de la Pool
		protected override Spawneable Spawn(Vector3 position = default, Quaternion rotation = default) =>
			base.Spawn(position + Center, rotation);

		// Spawnea el objeto de forma random dentro de la caja
		// spawnWithRandomRotation = true -> Randomiza la rotacion en el Eje Y
		public virtual Spawneable SpawnRandom(bool setRandomRotation = true) =>
			Spawn(GetRandomPointInBounds(), setRandomRotation ? RandomRotation : default);

		protected Vector3 GetRandomPointInBounds() => is2D
			? AABB2D.GetRandomPointInBounds(Offset2D).ToV3(isXZplane)
			: bounds.GetRandomPointInBounds(offset);


		protected override IEnumerator SpawnCoroutine()
		{
			while (gameObject.activeSelf)
			{
				yield return new WaitForSeconds(spawnFrequency);

				for (var i = 0; i < burstSpawn; i++)
					SpawnRandom();
				
				yield return new WaitUntil(() => gameObject.activeSelf);
			}
		}
	}
}

using System.Collections;
using DavidUtils.ExtensionMethods;
using UnityEngine;

namespace DavidUtils.Spawning
{
	[RequireComponent(typeof(BoxCollider))]
	public class SpawnerBox : Spawner
	{
		protected BoxCollider box;
		
		public bool ignoreHeight = false;
		public Vector3 offset = new Vector3(0, 0, 0);
		
		private Vector3 Center => box.bounds.center;

		// Start is called before the first frame update
		protected override void Awake()
		{
			base.Awake();

			// Ignora las colisiones de cualquier objeto de tipo Spawner
			Physics.IgnoreLayerCollision(0, gameObject.layer);

			box = GetComponent<BoxCollider>();
		}

		protected void Start()
		{
			for (var i = 0; i < initialNumItems; i++) 
				SpawnRandom();
		
			if (spawnFrequency >= 0)
				StartCoroutine(SpawnCoroutine());
		}

		// Spawnea el objeto de forma random dentro de la caja
		// spawnWithRandomRotation = true -> Randomiza la rotacion en el Eje Y
		public virtual Spawneable SpawnRandom(bool spawnWithRandomRotation = true)
		{
			Vector3 position = box.bounds.GetRandomPointInBounds(offset, ignoreHeight);

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

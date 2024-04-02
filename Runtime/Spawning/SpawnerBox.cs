using System.Collections;
using DavidUtils.ExtensionMethods;
using UnityEngine;

namespace DavidUtils.Spawning
{
	[RequireComponent(typeof(BoxCollider))]
	public class SpawnerBox : Spawner
	{
		public bool ignoreHeight = false;
	
		public Vector3 offset = new Vector3(0, 0, 0);

		protected BoxCollider box;

		// Start is called before the first frame update
		protected override void Awake()
		{
			base.Awake();

			// Ignora las colisiones de cualquier objeto de tipo Spawner
			Physics.IgnoreLayerCollision(0, gameObject.layer);

			box = GetComponent<BoxCollider>();
		}

		protected override void Start()
		{
			for (var i = 0; i < initialNumItems; i++) 
				SpawnRandom();
		
			if (spawnFrequency >= 0)
				StartCoroutine(SpawnCoroutine());
		}

		// Spawnea el objeto de forma random dentro de la caja
		// spawnWithRandomRotation = true -> Randomiza la rotacion en el Eje Y
		protected virtual Spawneable SpawnRandom(bool spawnWithRandomRotation = true)
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
			Spawneable obj = base.Spawn(position ?? GetCenter(), rotation);

			// Ignora colisiones entre el item y la caja del Spawner
			Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Spawner"), 0);

			return obj;
		}

		protected override IEnumerator SpawnCoroutine()
		{
			while (true)
			{
				yield return new WaitForSeconds(spawnFrequency);
			
				for (int i = 0; i < burstSpawn; i++)
					SpawnRandom();	
			}
		}


		public Vector3 GetCenter()
		{
			BoxCollider b = GetComponent<BoxCollider>();
			return b.center + transform.position;
		}
	}
}

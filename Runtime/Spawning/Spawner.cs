using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Utiliza POOLING
// Spawnea objetos en la posicion dada
namespace DavidUtils.Spawning
{
	public class Spawner : MonoBehaviour
	{
		public Spawneable objectPrefab;

		public int initialNumItems = 0;
		public float spawnFrequency = -1; // Seconds between spawns (-1 = desactivado)
		public int burstSpawn = 0;

		public Transform parent;

		public Transform Parent => parent ? parent : transform;

		// POOLING
		private readonly Stack<Spawneable> _pool = new();

		private IEnumerator spawnCoroutine;

		// Start is called before the first frame update
		protected virtual void Awake()
		{
			// Puebla la Pool con un numero inicial de items
			LoadPool(initialNumItems);
		}

		private void OnEnable()
		{
			if (!(spawnFrequency >= 0)) return;
			spawnCoroutine = SpawnCoroutine();
			StartCoroutine(spawnCoroutine);
		}

		private void OnDisable()
		{
			if (spawnCoroutine != null)
				StopCoroutine(spawnCoroutine);
		}

		// Carga la pool con un numero de objetos inicial
		public void LoadPool(int numObjects)
		{
			// Spawn Objects
			for (var i = 0; i < numObjects; i++) 
				Generate();
		}

		// Genera el Objeto pero sin activarlo
		private Spawneable Generate()
		{
			// Toma como padre el spawner si no se le pasa un padre
			Spawneable item = Instantiate(objectPrefab, Parent);
			item.spawner = this;
			item.gameObject.SetActive(false);
			_pool.Push(item);

			return item;
		}

		// Guarda el objeto en la pool para volverlo a spawnear luego
		// Esto debe llamarlo el propio objeto que debe heredar de Spawneable
		public void Despawn(Spawneable item)
		{
			item.gameObject.SetActive(false);
			_pool.Push(item);
		}

		// Destruye todos los objetos del pool
		public void Clear()
		{
			for (var i = 0; i < _pool.Count; i++)
				Destroy(_pool.Pop());
		}

		// Spawnea el objeto sacandolo de la Pool
		protected virtual Spawneable Spawn(Vector3? position = null, Quaternion? rotation = null)
		{
			// Si esta vacio creamos otro
			if (_pool.Count == 0)
				Generate();

			// Sacar de la Pool
			Spawneable item = _pool.Pop();
			item.gameObject.SetActive(true);

			// To World Space
			// if (position != null)
			// 	position = Parent.TransformPoint(position.Value);
		
			// Si no se han asignado posicion o rotacion por defecto sera en el centro y su rotacion por defecto
			item.transform.SetPositionAndRotation(
				position ?? (Parent.position),
				rotation ?? (objectPrefab.transform.rotation)
			);

			return item;
		}

		protected virtual IEnumerator SpawnCoroutine()
		{
			while (true)
			{
				yield return new WaitForSeconds(spawnFrequency);

				for (var i = 0; i < burstSpawn; i++)
					Spawn();		
			}
		}

		protected Quaternion GetRandomRotation() => Quaternion.Euler(0, Random.Range(-180, 180), 0);
	}
}

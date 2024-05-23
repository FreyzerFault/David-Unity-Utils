using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DavidUtils.ExtensionMethods;
using UnityEngine;
using Random = UnityEngine.Random;

// Utiliza POOLING
// Spawnea objetos en la posicion dada
namespace DavidUtils.Spawning
{
	public class Spawner : MonoBehaviour
	{
		public Spawneable objectPrefab;

		public Spawneable[] SpawnedItems => Parent.GetComponentsInChildren<Spawneable>();

		public Transform parent;
		public Transform Parent => parent ? parent : transform;

		public int initialNumItems;


		#region UNITY

		// Cache la Pool
		protected virtual void Awake() => InitializePool();

		// Spawnea los items iniciales
		protected virtual void Start()
		{
			for (var i = 0; i < initialNumItems; i++) Spawn();
		}

		private void OnEnable() => StartSpawnRoutine();
		private void OnDisable() => StopSpawnRoutine();

		#endregion


		#region SPAWNING

		/// <summary>
		///     Lo spawnea de la pool
		///     Si la pool está vacía, Instancia un item
		/// </summary>
		protected virtual Spawneable Spawn(Vector3? position = null, Quaternion? rotation = null)
		{
			// Sacamos el item de la pool o lo generamos si esta vacia
			Spawneable item;
			if (PoolCount == 0)
			{
				item = InstantiateItem();
				item.gameObject.SetActive(true);
			}
			else
			{
				item = SpawnFromPool();
			}

			// Asignar nueva Posicion y Rotacion
			item.transform.SetLocalPositionAndRotation(position ?? Vector3.zero, rotation ?? Quaternion.identity);

			return item;
		}

		public void Despawn(Spawneable item) => DespawnToPool(item);
		public void DespawnAll() => SpawnedItems.ForEach(Despawn);

		public void Clear()
		{
			ClearPool();
			SpawnedItems.ForEach(UnityUtils.DestroySafe);
		}

		#endregion


		#region INSTANTIATION

		/// <summary>
		///     Instancia el Objeto inactivo y lo guarda en la Pool
		/// </summary>
		private Spawneable InstantiateDespawned()
		{
			Spawneable item = InstantiateItem();
			Despawn(item);
			return item;
		}

		private Spawneable InstantiateItem()
		{
			Spawneable item = Instantiate(objectPrefab, Parent);
			item.spawner = this;
			return item;
		}

		#endregion


		#region SPAWN ROUTINE

		[Space]
		[Header("Spawn Routine")]
		public float spawnFrequency = -1; // Seconds between spawns (-1 = desactivado)
		public int burstSpawn;

		private IEnumerator spawnCoroutine;

		protected virtual IEnumerator SpawnCoroutine()
		{
			while (true)
			{
				yield return new WaitForSeconds(spawnFrequency);

				for (var i = 0; i < burstSpawn; i++)
					Spawn();
			}
		}

		public void StartSpawnRoutine()
		{
			if (spawnCoroutine != null) StopSpawnRoutine();
			spawnCoroutine = SpawnCoroutine();
			StartCoroutine(spawnCoroutine);
		}

		public void StopSpawnRoutine() => StopCoroutine(spawnCoroutine);

		#endregion


		#region POOLING

		[Space]
		[Header("Pooling")]
		public int initialPoolSize = 10;
		private readonly Stack<Spawneable> _pool = new();
		protected int PoolCount => _pool.Count;

		// Puebla la Pool con un numero inicial de items para mejor Performance
		private void InitializePool()
		{
			ClearPool();
			LoadPool(initialPoolSize);
		}

		/// <summary>
		///     Carga X items en la pool (NO Activos)
		/// </summary>
		public void LoadPool(int numObjects)
		{
			// Spawn Objects
			for (var i = 0; i < numObjects; i++)
				InstantiateDespawned();
		}

		private Spawneable SpawnFromPool()
		{
			if (_pool.IsNullOrEmpty()) return null;
			Spawneable item = _pool.Pop();
			item.gameObject.SetActive(true);
			return item;
		}

		private void DespawnToPool(Spawneable item)
		{
			item.gameObject.SetActive(false);
			_pool.Push(item);
		}

		public bool IsInPool(Spawneable item) => _pool.Contains(item);
		public void RemoveFromPool(Spawneable item) => _pool.ToList().Remove(item);

		// Destruye todos los objetos del pool
		public void ClearPool()
		{
			while (PoolCount != 0)
				UnityUtils.DestroySafe(_pool.Pop());
		}

		#endregion


		protected static Quaternion GetRandomRotation() => Quaternion.Euler(0, Random.Range(-180, 180), 0);
	}
}

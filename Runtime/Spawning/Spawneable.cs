using System;
using UnityEngine;

// Objeto que puede usarse en un Pool de un Spawner
// Activo => En la escena y fuera de la Pool
// Inactivo => En la Pool

namespace DavidUtils.Spawning
{
	public class Spawneable : MonoBehaviour
	{
		public Spawner spawner;

		private void Awake()
		{
			// Ignora colisiones entre el item y la caja del Spawner
			Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Spawner"), 0);
		}

		protected virtual void OnEnable()
		{
			// SPAWNED
		}

		protected virtual void OnDisable()
		{
			// DESPAWNED
		}

		// Cuando se destruye el objeto se devuelve a la pool
		public virtual void Despawn()
		{
			if (spawner)
				spawner.Despawn(this);
			else
				Debug.Log("El objeto no esta volviendo a su pool: " + ToString(), this);
		}
	}
}

using UnityEngine;

// Objeto que puede usarse en un Pool de un Spawner
// Activo => En la escena y fuera de la Pool
// Inactivo => En la Pool

namespace DavidUtils.Spawning
{
	public class Spawneable : MonoBehaviour
	{
		public Spawner spawner;

		// protected virtual void OnDisable() => Despawn();
		// private void OnDestroy() => Destroy();

		// Cuando se destruye el objeto se devuelve a la pool
		public virtual void Despawn()
		{
			if (spawner)
				spawner.Despawn(this);
			else
				Debug.Log("El objeto no esta volviendo a su pool: " + ToString(), this);
		}

		public virtual void Destroy()
		{
			if (!spawner)
			{
				Debug.Log("El objeto no esta volviendo a su pool: " + ToString(), this);
				return;
			}

			if (spawner.IsInPool(this))
				spawner.RemoveFromPool(this);

			Destroy(this);
		}
	}
}

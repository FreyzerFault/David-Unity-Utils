using UnityEngine;

// Objeto que puede usarse en un Pool
// Se puede spawnear (setActive = true)
// Se puede despawner (setActive = false)

namespace DavidUtils.Spawning
{
	public abstract class Spawneable : MonoBehaviour
	{
		public Spawner spawner;

		protected virtual void OnDisable()
		{
			Destroy();
		}

		// Cuando se destruye el objeto se devuelve a la pool
		public virtual void Destroy()
		{
			if (spawner)
				spawner.Pool(this);
			else
				print("El objeto no esta volviendo a su pool: " + ToString());
		}
	}
}

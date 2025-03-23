using DavidUtils.ExtensionMethods;
using UnityEngine;

namespace DavidUtils.Spawning
{
	public class SpawnerBoxInTerrain : SpawnerBox
	{
		public static Terrain Terrain => Terrain.activeTerrain;

		private bool NoTerrainInWorld => Terrain.activeTerrain == null;

		protected override void Awake()
		{
			isXZplane = true; // XZ obligatorio
			base.Awake();
		}

		/// <summary>
		///     Spawnea el objeto en la posición, con la altura del terreno
		///     Si la Bounding Box es 3D, la posicion del terreno debe estar contenida
		///     (Clamp de Y entre MIN y MAX de la BB)
		/// </summary>
		protected override Spawneable Spawn(Vector3 position = default, Quaternion rotation = default)
		{
			position = Terrain.Project(position);

			// Si es 3D Contenida en la BB
			if (!is2D) position.WithY(Mathf.Clamp(position.y, bounds.min.y, bounds.max.y));

			return base.Spawn(position, rotation);
		}
	}
}

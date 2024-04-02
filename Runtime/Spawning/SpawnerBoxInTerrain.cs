using DavidUtils.ExtensionMethods;
using UnityEngine;

namespace DavidUtils.Spawning
{
    public class SpawnerBoxInTerrain : SpawnerBox
    {
        private Terrain _terrain;
        
        protected override void Awake()
        {
            base.Awake();
            _terrain = Terrain.activeTerrain;
        }

        protected override Spawneable SpawnRandom(bool spawnWithRandomRotation = true)
        {
            if (_terrain == null) return base.SpawnRandom(spawnWithRandomRotation);
            
            Vector3 localPosition = box.bounds.GetRandomPointInBounds(offset, ignoreHeight);

            // Local to WORLD Space
            Vector3 worldPosition = Parent.TransformPoint(localPosition);

            // Can be under terrain:
            // Clamp min at terrain height + offset
            float minHeight = _terrain.GetWorldPosition(worldPosition).y + offset.y;

            worldPosition.y = Mathf.Max(worldPosition.y, minHeight);
            localPosition = Parent.InverseTransformPoint(worldPosition);

            return spawnWithRandomRotation 
                ? Spawn(localPosition, Quaternion.Euler(0, Random.Range(-180, 180), 0)) 
                : Spawn(localPosition);
        }
    }
}

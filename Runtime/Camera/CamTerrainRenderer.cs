using UnityEngine;

namespace DavidUtils.Camera
{
    [ExecuteAlways]
    [RequireComponent(typeof(UnityEngine.Camera))]
    public class CamTerrainRenderer : MonoBehaviour
    {
        private UnityEngine.Camera _cam;
        private Terrain _terrain;

        private void Awake() => 
            _cam = GetComponent<UnityEngine.Camera>();

        private void OnEnable()
        {
            _terrain = Terrain.activeTerrain;
            
            if (_terrain == null)
            {
                Debug.LogWarning("No Active Terrain found for cam to render");
                return;
            }
            
            _cam.orthographic = true;
            _cam.farClipPlane = 10000;
            
            CenterCam();
            UpdateSize();
        }

        private void CenterCam()
        {
            _cam.transform.position = _terrain.transform.position + _terrain.terrainData.size / 2 + Vector3.up * 2000;
            _cam.transform.forward = Vector3.down;
        }

        private void UpdateSize() => 
            _cam.orthographicSize = _terrain.terrainData.size.z / 2;
    }
}

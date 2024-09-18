using Cinemachine;
using UnityEngine;

namespace DavidUtils.Camera
{
	public class CameraManager : Singleton<CameraManager>
	{
		[SerializeField] private CinemachineVirtualCameraBase[] cams;
		[SerializeField] private CinemachineVirtualCameraBase activeCam;
		
		public static UnityEngine.Camera MainCam => UnityEngine.Camera.main;
		public static CinemachineVirtualCameraBase ActiveCam => Instance.activeCam;
		
		private int _currentCamIndex;

		protected override void Awake()
		{
			var brain = GetComponent<CinemachineBrain>();
			activeCam = brain.ActiveVirtualCamera as CinemachineVirtualCameraBase;
			if (cams.Length == 0) cams = FindObjectsOfType<CinemachineVirtualCameraBase>();
		}

		private void Start()
		{
			ResetCamPriority();
			ChangeToCam(0);
		}

		private void ResetCamPriority()
		{
			foreach (CinemachineVirtualCameraBase cam in cams) cam.Priority = 10;
		}

		public void NextCam() => ChangeToCam((_currentCamIndex + 1) % cams.Length);

		public void ChangeToCam(int index)
		{
			if (index >= cams.Length) return;

			cams[_currentCamIndex].Priority = 10;
			_currentCamIndex = index;
			cams[index].Priority = 100;
		}

		private void OnNextCamera() => NextCam();

		private void OnCam1() => ChangeToCam(0);

		private void OnCam2() => ChangeToCam(1);

		private void OnCam3() => ChangeToCam(2);

		private void OnCam4() => ChangeToCam(3);

		private void OnCam5() => ChangeToCam(4);

		private void OnCam6() => ChangeToCam(5);
	}
}

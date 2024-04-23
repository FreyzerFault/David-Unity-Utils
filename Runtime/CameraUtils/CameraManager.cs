using Cinemachine;
using UnityEngine;

namespace DavidUtils.CameraUtils
{
	public class CameraManager : Singleton<CameraManager>
	{
		[SerializeField] private CinemachineVirtualCameraBase[] _cams;
		[SerializeField] private CinemachineVirtualCameraBase _activeCam;
		
		public static Camera MainCam => Camera.main;
		public static CinemachineVirtualCameraBase ActiveCam => Instance._activeCam;
		
		private int _currentCamIndex;

		protected override void Awake()
		{
			var brain = GetComponent<CinemachineBrain>();
			_activeCam = brain.ActiveVirtualCamera as CinemachineVirtualCameraBase;
			if (_cams.Length == 0) _cams = FindObjectsOfType<CinemachineVirtualCameraBase>();
		}

		private void Start()
		{
			ResetCamPriority();
			ChangeToCam(0);
		}

		private void ResetCamPriority()
		{
			foreach (CinemachineVirtualCameraBase cam in _cams) cam.Priority = 10;
		}

		public void NextCam() => ChangeToCam(_currentCamIndex + 1);

		public void ChangeToCam(int index)
		{
			if (index >= _cams.Length) return;

			_cams[_currentCamIndex].Priority = 10;
			_currentCamIndex = index;
			_cams[index].Priority = 100;
		}

		private void OnNextCam() => ChangeToCam(_currentCamIndex + 1);

		private void OnCam1() => ChangeToCam(0);

		private void OnCam2() => ChangeToCam(1);

		private void OnCam3() => ChangeToCam(2);

		private void OnCam4() => ChangeToCam(3);

		private void OnCam5() => ChangeToCam(4);

		private void OnCam6() => ChangeToCam(5);
	}
}

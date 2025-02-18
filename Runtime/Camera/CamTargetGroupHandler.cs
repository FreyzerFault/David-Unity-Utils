using System.Linq;
using Unity.Cinemachine;
using UnityEngine;

namespace DavidUtils.Camera
{
	[RequireComponent(typeof(CinemachineTargetGroup))]
	public class CamTargetGroupHandler : MonoBehaviour
	{
		[SerializeField] private float firstMarkerWeight = 2;
		[SerializeField] private float defaultmarkerWeight = 1;

		private CinemachineTargetGroup _targetGroup;

		public Transform[] Targets => _targetGroup.Targets.Select(t => t.Object).ToArray();

		protected virtual void Awake() => _targetGroup = GetComponent<CinemachineTargetGroup>();

		public void AddTarget(Transform target) =>
			_targetGroup.AddMember(
				target,
				_targetGroup.m_Targets.Length == 1 ? firstMarkerWeight : defaultmarkerWeight,
				10
			);

		public void RemoveTarget(Transform target) => _targetGroup.RemoveMember(target);

		public void UpdateTargetGroup(Transform[] targets)
		{
			for (var i = 0; i < targets.Length; i++)
			{
				float weight = i == 0 ? firstMarkerWeight : defaultmarkerWeight;
				_targetGroup.AddMember(targets[i], weight, 10);
			}
		}
	}
}

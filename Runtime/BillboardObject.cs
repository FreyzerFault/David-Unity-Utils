using DavidUtils.ExtensionMethods;
using DavidUtils.PlayerControl;
using UnityEngine;

namespace DavidUtils
{
	public class BillboardObject : MonoBehaviour
	{
		private Player _player;
		public bool verticalLock;

		private void Awake() => _player = FindObjectOfType<Player>();

		private void Update() => transform.Billboard(_player.transform, verticalLock);
	}
}

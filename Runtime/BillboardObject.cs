using DavidUtils.ExtensionMethods;
using UnityEngine;

namespace DavidUtils
{
    public class BillboardObject : MonoBehaviour
    {
        private GameObject _player;
        public bool verticalLock = false;

        private void Awake()
        {
            _player = GameObject.FindWithTag("Player");
        }

        private void Update()
        {
            transform.Billboard(_player.transform, verticalLock);
        }
    }
}

using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DavidUtils.PlayerControl
{
    public class Player : MonoBehaviour
    {
        public enum PlayerState
        {
            Playing,
            Pause
        }

        protected PlayerState _state = PlayerState.Playing;
        protected Vector3 _moveInput = Vector3.zero;

        public event Action<Vector3> OnPlayerMove;
        public event Action<Vector3> OnPlayerStop;

        protected Vector3 Position => transform.position;
        protected Vector3 Forward => transform.forward;
        protected Vector3 Right => transform.right;

        public PlayerState State
        {
            get => _state;
            set
            {
                if (value == _state)
                    return;
                HandleStateChanged(value);
                _state = value;
            }
        }

        protected virtual void Awake() => HandleStateChanged(_state);

        protected virtual void Start() { }

        protected virtual void Update() { }

        protected virtual void HandleStateChanged(PlayerState newState) { }

        protected virtual void OnMove(InputValue value)
        {
            if (_state == PlayerState.Pause)
                return;
            _moveInput = value.Get<Vector2>();

            if (_moveInput == Vector3.zero)
                OnPlayerStop?.Invoke(Position);
            else
                OnPlayerMove?.Invoke(_moveInput);
        }
    }
}

using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DavidUtils.Player
{
    public enum PlayerState
    {
        Playing,
        Pause
    }

    // Controla el movimiento en 2D con OnMove
    public abstract class Player : Singleton<Player>
    {
        public Vector3 Position => transform.position;
        public Vector3 Forward => transform.forward;
        public Vector3 Right => transform.right;
        public Quaternion Rotation => transform.rotation;

        protected override void Awake()
        {
            base.Awake();
            HandleStateChanged(state);
        }


        protected virtual void FixedUpdate()
        {
            if (state == PlayerState.Pause || moveInput == Vector2.zero) return;
            OnPlayerMove?.Invoke(moveInput);
            HandleMovementInput();
        }

        #region STATE

        public event Action<PlayerState> OnStateChanged;
        [SerializeField] protected PlayerState state = PlayerState.Playing;
        public PlayerState State
        {
            get => state;
            set
            {
                if (value == state) return;
                state = value;
                HandleStateChanged(value);
            }
        }

        protected void HandleStateChanged(PlayerState newState) => OnStateChanged?.Invoke(newState);

        #endregion


        #region MOVE

        protected Vector2 moveInput = Vector2.zero;

        public event Action<Vector2> OnPlayerMove;
        public event Action<Vector3> OnPlayerStop;

        // Override THIS
        protected abstract void HandleMovementInput();

        // PLAYER INPUT
        protected virtual void OnMove(InputValue value)
        {
            if (state == PlayerState.Pause) return;
            moveInput = value.Get<Vector2>();

            if (moveInput == Vector2.zero) OnPlayerStop?.Invoke(Position);
        }

        #endregion
    }
}

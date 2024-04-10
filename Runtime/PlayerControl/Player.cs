using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DavidUtils.PlayerControl
{
	public enum PlayerState
	{
		Playing,
		Pause
	}

	// Controla el movimiento en 2D con OnMove
	public class Player : MonoBehaviour
	{
		protected Vector3 Position => transform.position;
		protected Vector3 Forward => transform.forward;
		protected Vector3 Right => transform.right;

		protected virtual void Awake() => HandleStateChanged(state);

		protected virtual void Start()
		{
			if (GameManager.Instance == null) return;
			GameManager.Instance.onGameStateChanged.AddListener(HandleGameStateChanged);
		}

		protected virtual void Update()
		{
			if (state == PlayerState.Pause) return;
			if (_moveInput != Vector3.zero)
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

		protected virtual void HandleStateChanged(PlayerState newState) => OnStateChanged?.Invoke(newState);

		private void HandleGameStateChanged(GameManager.GameState gameState) => State = gameState switch
		{
			GameManager.GameState.Playing => PlayerState.Playing,
			GameManager.GameState.Paused => PlayerState.Pause,
			_ => State
		};

		#endregion


		#region MOVE

		protected Vector3 _moveInput = Vector3.zero;
		public float speed = 1f;

		public event Action<Vector3> OnPlayerMove;
		public event Action<Vector3> OnPlayerStop;

		private void HandleMovementInput() => transform.position +=
			Forward * (_moveInput.y * Time.deltaTime * speed)
			+ Right * (_moveInput.x * Time.deltaTime * speed);

		protected virtual void OnMove(InputValue value)
		{
			if (state == PlayerState.Pause) return;
			_moveInput = value.Get<Vector2>();

			if (_moveInput == Vector3.zero) OnPlayerStop?.Invoke(Position);
			else OnPlayerMove?.Invoke(_moveInput);
		}

		#endregion
	}
}

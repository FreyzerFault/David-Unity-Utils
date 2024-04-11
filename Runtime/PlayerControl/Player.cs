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
		public Vector3 Position => transform.position;
		public Vector3 Forward => transform.forward;
		public Vector3 Right => transform.right;
		public Quaternion Rotation => transform.rotation;

		protected virtual void Awake() => HandleStateChanged(state);

		protected virtual void Start()
		{
			if (GameManager.Instance == null) return;
			GameManager.Instance.onGameStateChanged.AddListener(HandleGameStateChanged);
		}

		protected virtual void Update()
		{
			if (state == PlayerState.Pause) return;
			if (moveInput == Vector2.zero) return;
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

		protected virtual void HandleStateChanged(PlayerState newState) => OnStateChanged?.Invoke(newState);

		private void HandleGameStateChanged(GameManager.GameState gameState) => State = gameState switch
		{
			GameManager.GameState.Playing => PlayerState.Playing,
			GameManager.GameState.Paused => PlayerState.Pause,
			_ => State
		};

		#endregion


		#region MOVE

		protected Vector2 moveInput = Vector2.zero;
		public float speed = 1f;

		public event Action<Vector2> OnPlayerMove;
		public event Action<Vector3> OnPlayerStop;

		private void HandleMovementInput() => transform.position +=
			Forward * (moveInput.y * Time.deltaTime * speed)
			+ Right * (moveInput.x * Time.deltaTime * speed);

		protected virtual void OnMove(InputValue value)
		{
			if (state == PlayerState.Pause) return;
			moveInput = value.Get<Vector2>();

			if (moveInput == Vector2.zero) OnPlayerStop?.Invoke(Position);
		}

		#endregion
	}
}

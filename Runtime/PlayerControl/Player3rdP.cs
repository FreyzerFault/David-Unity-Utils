using UnityEngine;
using UnityEngine.InputSystem;

namespace DavidUtils.PlayerControl
{
	// Adds 3rd Person Control to a Camera
	public class Player3rdP : Player
	{
		protected override void Update()
		{
			base.Update();
			if (state == PlayerState.Pause) return;
			HandleRotationInput();

			// FIX Vertical Rotation on X and Z axis. Only horizontal allowed
			transform.rotation = Quaternion.Euler(new Vector3(0, transform.rotation.eulerAngles.y, 0));
		}

		#region STATE

		protected override void HandleStateChanged(PlayerState newState)
		{
			base.HandleStateChanged(newState);
			switch (newState)
			{
				case PlayerState.Playing:
					Cursor.lockState = CursorLockMode.Locked;
					Cursor.visible = false;
					break;
				case PlayerState.Pause:
					Cursor.lockState = CursorLockMode.None;
					Cursor.visible = true;
					break;
			}
		}

		#endregion


		#region CAM ROTATION

		[SerializeField] private Transform camPoint;

		[Range(0, 10)] public float angularSpeed = 1f;
		[Range(-90, 0)] private const float minPitch = -45f;
		[Range(0, 90)] private const float maxPitch = 45f;

		private static Vector2 MouseDelta => Mouse.current.delta.ReadValue();

		private void HandleRotationInput()
		{
			if (camPoint == null || MouseDelta == Vector2.zero) return;

			// Vertical Rotation
			float angle = -MouseDelta.y * angularSpeed * Time.deltaTime;

			// Convertir el ángulo a un rango de -180 a 180
			if (angle > 180) angle -= 360;

			// Clamp to 89º
			Vector3 localPosition = camPoint.localPosition;
			localPosition = Quaternion.Euler(angle, 0, 0) * localPosition;

			// Limitar a un angulo maximo y minimo
			Vector3 forward = Vector3.forward * localPosition.magnitude;
			float angleDif = Vector3.SignedAngle(forward, localPosition, Vector3.right);
			localPosition = angleDif switch
			{
				> maxPitch => Quaternion.Euler(maxPitch, 0, 0) * forward,
				< minPitch => Quaternion.Euler(minPitch, 0, 0) * forward,
				_ => localPosition
			};
			camPoint.localPosition = localPosition;

			// Horizontal Rotation
			// PLAYER
			transform.rotation *= Quaternion.Euler(0, MouseDelta.x * angularSpeed * Time.deltaTime, 0);
		}

		#endregion
	}
}

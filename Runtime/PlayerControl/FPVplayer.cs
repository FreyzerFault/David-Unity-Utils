using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DavidUtils.PlayerControl
{
    public class FPVplayer: Player
    {
        [Range(0, 10)]public float angularSpeed = 1f;
        [Range(-90, 0)] const float minPitch = -45f;
        [Range(0, 90)] const float maxPitch = 45f;
        [SerializeField] private Transform camPoint;

        protected override void Awake()
        {
            base.Awake();
            // camPoint.rotation = Quaternion.identity;
        }

        protected override void Update()
        {
            base.Update();
            if (_state == PlayerState.Pause) return;
            HandleRotationInput();
        }
        
        private void HandleRotationInput()
        {
            Vector2 mouseDelta = Mouse.current.delta.ReadValue();

            if (mouseDelta == Vector2.zero) return;

            // Vertical Rotation
            float angle = -mouseDelta.y * angularSpeed * Time.deltaTime;

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
            transform.rotation *= Quaternion.Euler(0, mouseDelta.x * angularSpeed * Time.deltaTime, 0);
        }
    }
}
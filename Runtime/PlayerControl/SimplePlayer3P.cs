using DavidUtils.CameraUtils;
using UnityEngine;

namespace DavidUtils.PlayerControl
{
    // Utiliza CharacterController para moverse
    public class SimplePlayer3P : Player
    {
        protected CharacterController controller;

        protected override void Awake()
        {
            base.Awake();
            
            controller = GetComponent<CharacterController>();
        }

        #region MOVEMENT
        
        protected Vector3 moveDir = Vector3.forward;
        
        protected float turnSmoothVelocity;
        
        public float turnSmoothTime = .1f;
        public float speed = 10f;
        
        protected override void HandleMovementInput()
        {
            Vector3 moveVector = new Vector3(moveInput.x, 0, moveInput.y).normalized;
			
            // Turn smoothly towards movement direction
            Transform cam = CameraManager.MainCam.transform;
            float targetAngle = Mathf.Atan2(moveVector.x, moveVector.z) * Mathf.Rad2Deg + cam.eulerAngles.y;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);
            transform.rotation = Quaternion.Euler(0, angle, 0);
			
            moveDir = Quaternion.Euler(0, targetAngle, 0) * Vector3.forward;
			
            controller.Move(moveDir.normalized * (speed * Time.deltaTime));
        }

        #endregion
        

        #region COLLISION

        public void ToggleCollisions(bool value) => controller.detectCollisions = value;

        #endregion
    }
}

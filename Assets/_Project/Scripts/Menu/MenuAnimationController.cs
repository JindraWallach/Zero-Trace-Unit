// Copyright (c) 2024 Synty Studios Limited. All rights reserved.
//
// Simplified Menu Animation Controller - Only handles basic animations without movement systems
// For use in menu scenes where player control is not needed

using UnityEngine;

namespace Synty.AnimationBaseLocomotion.Samples
{
    public class MenuAnimationController : MonoBehaviour
    {
        #region Animation Variable Hashes

        private readonly int _moveSpeedHash = Animator.StringToHash("MoveSpeed");
        private readonly int _isGroundedHash = Animator.StringToHash("IsGrounded");
        private readonly int _fallingDurationHash = Animator.StringToHash("FallingDuration");
        private readonly int _isJumpingAnimHash = Animator.StringToHash("IsJumping");
        private readonly int _headLookXHash = Animator.StringToHash("HeadLookX");
        private readonly int _headLookYHash = Animator.StringToHash("HeadLookY");
        private readonly int _bodyLookXHash = Animator.StringToHash("BodyLookX");
        private readonly int _bodyLookYHash = Animator.StringToHash("BodyLookY");
        private readonly int _leanValueHash = Animator.StringToHash("LeanValue");

        #endregion

        #region Serialized Fields

        [Header("Menu Animation Settings")]
        [Tooltip("Animator component for controlling menu animations")]
        [SerializeField]
        private Animator _animator;

        [Tooltip("Character Controller for ground detection (optional)")]
        [SerializeField]
        private CharacterController _controller;

        [Header("Animation State")]
        [Tooltip("Should character play idle animation")]
        [SerializeField]
        private bool _playIdle = true;

        [Tooltip("Should character simulate falling/landing sequence")]
        [SerializeField]
        private bool _simulateFallSequence = false;

        [Tooltip("Delay before starting fall sequence (if enabled)")]
        [SerializeField]
        private float _fallSequenceDelay = 2f;

        [Header("Ground Detection")]
        [Tooltip("Layer mask for checking ground")]
        [SerializeField]
        private LayerMask _groundLayerMask = 1; // Default layer

        [Tooltip("Offset for ground check")]
        [SerializeField]
        private float _groundedOffset = -0.14f;

        [Header("Camera Look")]
        [Tooltip("Should character rotate to face camera direction")]
        [SerializeField]
        private bool _faceCamera = true;

        [Tooltip("Main camera reference (auto-detected if null)")]
        [SerializeField]
        private Camera _mainCamera;

        [Tooltip("Smoothness of rotation towards camera (higher = smoother)")]
        [SerializeField]
        private float _rotationSmoothing = 5f;

        [Header("Head Look Settings")]
        [Tooltip("Enable head look towards camera")]
        [SerializeField]
        private bool _enableHeadLook = true;

        [Tooltip("Smoothness of head rotation")]
        [SerializeField]
        private float _headLookSmoothing = 5f;

        [Tooltip("Curve for X-axis head turning")]
        [SerializeField]
        private AnimationCurve _headLookXCurve = AnimationCurve.Linear(-1f, -1f, 1f, 1f);

        [Header("Body Look Settings")]
        [Tooltip("Enable body look towards camera")]
        [SerializeField]
        private bool _enableBodyLook = true;

        [Tooltip("Smoothness of body rotation")]
        [SerializeField]
        private float _bodyLookSmoothing = 5f;

        [Tooltip("Curve for X-axis body turning")]
        [SerializeField]
        private AnimationCurve _bodyLookXCurve = AnimationCurve.Linear(-1f, -0.5f, 1f, 0.5f);

        [Header("Lean Settings")]
        [Tooltip("Enable leaning when rotating")]
        [SerializeField]
        private bool _enableLean = false;

        [Tooltip("Curve for leaning")]
        [SerializeField]
        private AnimationCurve _leanCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        [Tooltip("Current value for leaning")]
        [SerializeField]
        private float _leanValue = 0f;

        #endregion

        #region Runtime Variables

        private bool _isGrounded = true;
        private float _fallingDuration = 0f;
        private float _fallStartTime = 0f;
        private float _sequenceTimer = 0f;
        private MenuAnimationState _currentState = MenuAnimationState.Idle;

        // Camera look variables
        private float _headLookX = 0f;
        private float _headLookY = 0f;
        private float _bodyLookX = 0f;
        private float _bodyLookY = 0f;
        private Vector3 _cameraForward;
        private Vector3 _currentRotation = Vector3.zero;
        private Vector3 _previousRotation = Vector3.zero;
        private float _rotationRate = 0f;

        private enum MenuAnimationState
        {
            Idle,
            Falling,
            Landing
        }

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            // Validate animator
            if (_animator == null)
            {
                _animator = GetComponent<Animator>();
                if (_animator == null)
                {
                    Debug.LogError("MenuAnimationController: No Animator component found!");
                    enabled = false;
                    return;
                }
            }

            // Auto-detect main camera if not assigned
            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;
                if (_mainCamera == null)
                {
                    Debug.LogWarning("MenuAnimationController: No camera found! Face camera feature will be disabled.");
                    _faceCamera = false;
                }
            }

            // Initialize to idle state
            SetIdleState();

            // Initialize rotation tracking
            _previousRotation = transform.forward;

            // Start fall sequence if enabled
            if (_simulateFallSequence)
            {
                _sequenceTimer = _fallSequenceDelay;
            }
        }

        private void Update()
        {
            // Handle fall sequence simulation if enabled
            if (_simulateFallSequence && _sequenceTimer > 0f)
            {
                _sequenceTimer -= Time.deltaTime;
                if (_sequenceTimer <= 0f)
                {
                    StartFallSequence();
                }
            }

            // Update camera facing
            if (_faceCamera && _mainCamera != null)
            {
                UpdateCameraFacing();
            }

            // Update current animation state
            switch (_currentState)
            {
                case MenuAnimationState.Idle:
                    UpdateIdleState();
                    break;
                case MenuAnimationState.Falling:
                    UpdateFallingState();
                    break;
                case MenuAnimationState.Landing:
                    UpdateLandingState();
                    break;
            }

            // Update animator with current values
            UpdateAnimator();
        }

        #endregion

        #region Camera Facing

        private void UpdateCameraFacing()
        {
            Vector3 directionToCamera =
                _mainCamera.transform.position - transform.position;
            directionToCamera.y = 0f;

            if (directionToCamera.sqrMagnitude < 0.001f)
                return;

            directionToCamera.Normalize();

            // Otočení těla ke kameře
            Quaternion targetRotation = Quaternion.LookRotation(directionToCamera);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                _rotationSmoothing * Time.deltaTime
            );

            // HEAD LOOK
            if (_enableHeadLook)
            {
                Vector3 localDir =
                    transform.InverseTransformDirection(directionToCamera);

                _headLookX = Mathf.Lerp(
                    _headLookX,
                    Mathf.Clamp(localDir.x, -1f, 1f),
                    _headLookSmoothing * Time.deltaTime
                );

                float cameraTilt = _mainCamera.transform.rotation.eulerAngles.x;
                cameraTilt = (cameraTilt > 180 ? cameraTilt - 360 : cameraTilt) / -180f;

                _headLookY = Mathf.Lerp(
                    _headLookY,
                    Mathf.Clamp(cameraTilt, -0.3f, 0.8f),
                    _headLookSmoothing * Time.deltaTime
                );
            }

            // BODY LOOK (slabší než hlava)
            if (_enableBodyLook)
            {
                Vector3 localDir =
                    transform.InverseTransformDirection(directionToCamera);

                _bodyLookX = Mathf.Lerp(
                    _bodyLookX,
                    localDir.x * 0.5f,
                    _bodyLookSmoothing * Time.deltaTime
                );

                _bodyLookY = _headLookY * 0.5f;
            }
        }


        private void UpdateHeadLook()
        {
            // Get camera tilt for head look Y (vertical)
            float cameraTilt = _mainCamera.transform.rotation.eulerAngles.x;
            cameraTilt = (cameraTilt > 180f ? cameraTilt - 360f : cameraTilt) / -180f;
            cameraTilt = Mathf.Clamp(cameraTilt, -0.1f, 1.0f);

            float targetHeadLookY = cameraTilt;

            // Calculate head look X from rotation rate
            float maxRotationRate = 275.0f;
            float headTurnValue = _rotationRate;
            float changeVariable = headTurnValue / maxRotationRate;
            changeVariable = Mathf.Clamp(changeVariable, -1.0f, 1.0f);
            changeVariable = _headLookXCurve.Evaluate(changeVariable);

            // Smooth interpolation
            _headLookX = Mathf.Lerp(_headLookX, changeVariable, _headLookSmoothing * Time.deltaTime);
            _headLookY = Mathf.Lerp(_headLookY, targetHeadLookY, _headLookSmoothing * Time.deltaTime);
        }

        private void UpdateBodyLook()
        {
            // Get camera tilt for body look Y (vertical) - reduced compared to head
            float cameraTilt = _mainCamera.transform.rotation.eulerAngles.x;
            cameraTilt = (cameraTilt > 180f ? cameraTilt - 360f : cameraTilt) / -180f;
            cameraTilt = Mathf.Clamp(cameraTilt, -0.1f, 1.0f);

            float targetBodyLookY = cameraTilt * 0.5f; // Body moves less than head

            // Calculate body look X from rotation rate
            float maxRotationRate = 275.0f;
            float bodyTurnValue = _rotationRate;
            float changeVariable = bodyTurnValue / maxRotationRate;
            changeVariable = Mathf.Clamp(changeVariable, -1.0f, 1.0f);
            changeVariable = _bodyLookXCurve.Evaluate(changeVariable);

            // Smooth interpolation
            _bodyLookX = Mathf.Lerp(_bodyLookX, changeVariable, _bodyLookSmoothing * Time.deltaTime);
            _bodyLookY = Mathf.Lerp(_bodyLookY, targetBodyLookY, _bodyLookSmoothing * Time.deltaTime);
        }

        private void UpdateLean()
        {
            // Calculate lean from rotation rate
            float leanSmoothness = 5f;
            float maxLeanRotationRate = 275.0f;

            float initialLeanValue = _rotationRate;
            float changeVariable = initialLeanValue / maxLeanRotationRate;
            changeVariable = Mathf.Clamp(changeVariable, -1.0f, 1.0f);

            // Apply lean curve
            float multiplier = _leanCurve.Evaluate(0.5f); // Use middle value for menu
            changeVariable *= multiplier;

            // Smooth interpolation
            _leanValue = Mathf.Lerp(_leanValue, changeVariable, leanSmoothness * Time.deltaTime);
        }

        #endregion

        #region Animation States

        private void SetIdleState()
        {
            _currentState = MenuAnimationState.Idle;
            _isGrounded = true;
            _fallingDuration = 0f;

            _animator.SetFloat(_moveSpeedHash, 0f);
            _animator.SetBool(_isGroundedHash, true);
            _animator.SetBool(_isJumpingAnimHash, false);
            _animator.SetFloat(_fallingDurationHash, 0f);
        }

        private void UpdateIdleState()
        {
            // Check ground if controller exists
            if (_controller != null)
            {
                CheckGround();
            }

            // Just maintain idle animation
            _animator.SetFloat(_moveSpeedHash, 0f);
        }

        private void StartFallSequence()
        {
            _currentState = MenuAnimationState.Falling;
            _isGrounded = false;
            _fallStartTime = Time.time;
            _fallingDuration = 0f;

            _animator.SetBool(_isGroundedHash, false);
        }

        private void UpdateFallingState()
        {
            _fallingDuration = Time.time - _fallStartTime;
            _animator.SetFloat(_fallingDurationHash, _fallingDuration);

            // Simulate landing after 1 second
            if (_fallingDuration > 1f)
            {
                StartLandingSequence();
            }
        }

        private void StartLandingSequence()
        {
            _currentState = MenuAnimationState.Landing;
            _isGrounded = true;

            _animator.SetBool(_isGroundedHash, true);
            _animator.SetFloat(_fallingDurationHash, 0f);
        }

        private void UpdateLandingState()
        {
            // Wait for landing animation to complete (approx 0.5s)
            _fallingDuration += Time.deltaTime;

            if (_fallingDuration > 0.5f)
            {
                // Return to idle or loop sequence
                if (_simulateFallSequence)
                {
                    _sequenceTimer = _fallSequenceDelay;
                }
                SetIdleState();
            }
        }

        #endregion

        #region Ground Detection

        private void CheckGround()
        {
            if (_controller == null) return;

            Vector3 spherePosition = new Vector3(
                _controller.transform.position.x,
                _controller.transform.position.y - _groundedOffset,
                _controller.transform.position.z
            );

            _isGrounded = Physics.CheckSphere(
                spherePosition,
                _controller.radius,
                _groundLayerMask,
                QueryTriggerInteraction.Ignore
            );
        }

        #endregion

        #region Animator Update

        private void UpdateAnimator()
        {
            _animator.SetBool(_isGroundedHash, _isGrounded);
            _animator.SetFloat(_fallingDurationHash, _fallingDuration);

            // Update head and body look
            _animator.SetFloat(_headLookXHash, _headLookX);
            _animator.SetFloat(_headLookYHash, _headLookY);
            _animator.SetFloat(_bodyLookXHash, _bodyLookX);
            _animator.SetFloat(_bodyLookYHash, _bodyLookY);

            // Update lean
            _animator.SetFloat(_leanValueHash, _leanValue);
        }

        #endregion

        #region Public Methods (for external control if needed)

        /// <summary>
        /// Manually trigger fall sequence
        /// </summary>
        public void TriggerFallSequence()
        {
            if (_currentState == MenuAnimationState.Idle)
            {
                StartFallSequence();
            }
        }

        /// <summary>
        /// Reset to idle state
        /// </summary>
        public void ResetToIdle()
        {
            SetIdleState();
        }

        #endregion
    }
}
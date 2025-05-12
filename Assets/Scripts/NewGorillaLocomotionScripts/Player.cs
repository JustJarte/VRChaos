namespace GorillaLocomotion
{
    using UnityEngine;
    using UnityEngine.InputSystem;
    using UnityEngine.XR.Interaction.Toolkit;
    using Fusion.XR.Shared.Rig;
    using Fusion;
    using System.Collections.Generic;
    using Unity.XR.CoreUtils;
    using UnityEngine.SpatialTracking;

    public enum PlayerStatus
    {
        Default,
        Stunned,
        Invulnerable,
        Rabid,
        Buffed,
        Slowed,
        Eliminated
    }

    public enum SkinType
    {
        Default,
        Alt1,
        Rabid,
        Eliminated
    }

    public class Player : NetworkBehaviour
    {
        #region Get and Set
        public Vector3 RigidbodyMovement { get { return rigidbodyMovement; } set { rigidbodyMovement = value; } }
        public NetworkRunner PlayerNetworkRunner { get { return runner; } }
        #endregion

        #region Public Variables
        [Header("XR Camera and Hands")]
        public Camera headCamera;
        public TrackedPoseDriver trackedPoseDriver;
        public SphereCollider headCollider;
        public CapsuleCollider bodyCollider;

        public Transform leftHandFollower;
        public Transform rightHandFollower;

        public Transform leftHandTransform;
        public Transform rightHandTransform;

        public XROrigin xrOrigin;

        [Header("Player Attributes")]
        public int velocityHistorySize;
        public float maxArmLength = 1.5f;
        public float unStickDistance = 1f;
        public float minimumRaycastDistance = 0.05f;
        public float defaultSlideFactor = 0.03f;
        public float defaultPrecision = 0.995f;
        public float velocityLimit;
        public float maxJumpSpeed;
        public float jumpMultiplier;
        public float tagRange = 1.5f;
        public float stunDuration = 2.0f;
        public float invulnerabilityDuration = 3.0f;
        public float slowDuration = 1.5f;
        public float slowMultiplier = 0.6f;

        public LayerMask locomotionEnabledLayers;
        [Tooltip("Only used for testing multiplayer in the Unity engine. Disables VR-based movement for keyboard movement.")]
        public bool debugMultiplayerTesting;

        [HideInInspector] public Vector3 rightHandOffset;
        [HideInInspector] public Vector3 leftHandOffset;
        [HideInInspector] public Vector3 previousLeftHandLocalPos;
        [HideInInspector] public Vector3 previousRightHandLocalPos;

        [HideInInspector] public bool wasLeftHandTouching;
        [HideInInspector] public bool wasRightHandTouching;
        [HideInInspector] public bool disableMovement = false;

        [HideInInspector] public List<string> statusKeys = new List<string>();

        [Header("References")]
        public List<GameObject> playerHands = new List<GameObject>();
        public List<GameObject> holdObjects = new List<GameObject>();
        public Transform leftHandHoldSpace;
        public Transform rightHandHoldSpace;
        public XRControllerInputDevice leftHandDevice;
        public XRControllerInputDevice rightHandDevice;
        public PlayerNameplateHandler namePlateHandler;
        public GameSettingsSO gameSettings;
        public OptionSettingsSO optionSettings;
        public PlayerSkinContainer playerSkins;
        public AudioListener playerAudioListener;
        public SkinnedMeshRenderer playerSkinRenderer;
        #endregion

        #region Networked Variables        
        [Networked, OnChangedRender(nameof(OnInfectedChanged))] [HideInInspector] public bool IsInfected { get; set; }
        [Networked, OnChangedRender(nameof(OnHasBeenEliminated))] [HideInInspector] public bool IsEliminated { get; set; }
        [Networked, OnChangedRender(nameof(OnInvulnerabilityChanged))] [HideInInspector] public bool IsInvulnerable { get; set; }
        [Networked, OnChangedRender(nameof(OnStunChanged))] [HideInInspector] public bool IsStunned { get; set; }
        [Networked, OnChangedRender(nameof(OnLastOfKindChanged))] [HideInInspector] public bool HasBuff { get; set; }
        [Networked, OnChangedRender(nameof(OnSlowChanged))] [HideInInspector] public bool IsSlowed { get; set; }
        [Networked] [HideInInspector] public bool IsAfflicted { get; set; } = false;
        [Networked] private int lives { get; set; } = 3;

        private float stunTimer;
        private float invulnTimer;
        private float slowTimer;

        private TickTimer afflictionTimer;
        #endregion

        #region Private Variables
        private bool leftTriggerHeld = false;
        private bool rightTriggerHeld = false;
        private bool jumpHandIsLeft;
        private bool lastInfectedState = false;
        private bool wasBuffed = false;

        private int velocityIndex;

        private float originalVelocityLimit;
        private float originalJumpMultiplier;
        private float lastSnapTime = -999.0f;
        private float currentTurnSpeed = 0.0f;
        private float turnAccelerationVelocity = 0.0f;
        private float smoothedTurnInput = 0.0f;

        private Vector3 lastLeftHandPosition;
        private Vector3 lastRightHandPosition;
        private Vector3 lastHeadPosition;
        private Vector3 currentVelocity;
        private Vector3 denormalizedVelocityAverage;
        private Vector3 lastPosition;
        private Vector3 rigidbodyMovement;
        private Vector3[] velocityHistory;

        private Rigidbody playerRigidBody;

        private NetworkRunner runner;

        private Dictionary<string, PlayerStatus> currentPlayerStatuses = new Dictionary<string, PlayerStatus>();
        #endregion

        private void Awake()
        {
            //InitializeValues();
        }

        public override void Spawned()
        {
            runner = Runner;

            InitializeValues();

            Debug.Log($"[Join] PlayerRef = {Object.InputAuthority}, LocalPlayer = {runner.LocalPlayer}, HasInputAuthority = {HasInputAuthority}");

            if (Object.HasInputAuthority)
            {
                EnableOrDisableLocalComponents(true);
                
                if (MultipurposeHUD.Instance != null)
                {
                    MultipurposeHUD.Instance?.InitializeForLocalPlayer(headCamera);
                }
            }
            else
            {
                EnableOrDisableLocalComponents(false);
            }

            if (Object.HasStateAuthority)
            {
                if (gameSettings.selectedGamePlayMode == GamePlayMode.Tag)
                {
                    if (TagModeManager.Instance != null)
                    {
                        TagModeManager.Instance?.RegisterPlayer(Object.InputAuthority);

                        if (!TagModeManager.Instance.TimerStarted)
                        {
                            TagModeManager.Instance?.StartTimerNow();
                        }
                    }
                    else
                    {
                        Debug.LogWarning("Tag Mode Manager Instance was not found!");
                    }
                }
                else if (gameSettings.selectedGamePlayMode == GamePlayMode.Battle)
                {
                    if (BattleModeManager.Instance != null)
                    {
                        BattleModeManager.Instance?.RegisterPlayer(Object.InputAuthority, gameSettings.selectedCryptidCharacter);

                        if (!BattleModeManager.Instance.TimerStarted)
                        {
                            BattleModeManager.Instance?.StartTimerNow();
                        }

                        if (!debugMultiplayerTesting)
                        {
                            SetupPrimaryHandHeldObject();
                        }
                    }
                    else
                    {
                        Debug.LogWarning("Battle Mode Manager Instance was not found!");
                    }
                }
                else if (gameSettings.selectedGamePlayMode == GamePlayMode.Decryptid)
                {
                    if (DecryptidModeManager.Instance != null)
                    {
                        DecryptidModeManager.Instance?.RegisterPlayer(Object.InputAuthority);
                    }
                    else
                    {
                        Debug.LogWarning("Decryptid Mode Manager Instance was not found!");
                    }
                }
                else if (gameSettings.selectedGamePlayMode == GamePlayMode.FreePlay)
                {
                    if (FreePlayModeManager.Instance != null)
                    {
                        FreePlayModeManager.Instance?.RegisterPlayer(Object.InputAuthority);
                    }
                    else
                    {
                        Debug.LogWarning("Free Play Mode Manager Instance was not found!");
                    }
                }
            }
        }

        private void EnableOrDisableLocalComponents(bool enable)
        {
            if (!debugMultiplayerTesting)
            {
                if (headCamera != null)
                {
                    headCamera.enabled = enable;
                }

                if (playerAudioListener != null)
                {
                    playerAudioListener.enabled = enable;
                }

                if (xrOrigin != null)
                {
                    xrOrigin.enabled = enable;
                }

                if (leftHandDevice != null)
                {
                    leftHandDevice.enabled = enable;
                }
                if (rightHandDevice != null)
                {
                    rightHandDevice.enabled = enable;
                }

                if (trackedPoseDriver != null)
                {
                    trackedPoseDriver.enabled = enable;
                }

                if (playerRigidBody != null)
                {
                    if (!enable)
                    {
                        playerRigidBody.isKinematic = true;
                    }
                    if (enable)
                    {
                        playerRigidBody.isKinematic = false;
                    }
                }
            }
        }

        private void SetupPrimaryHandHeldObject()
        {
            if (optionSettings.playerRightHanded)
            {
                if (gameSettings.selectedGamePlayMode == GamePlayMode.Battle)
                {
                    if (holdObjects != null)
                    {
                        holdObjects[0].SetActive(true);
                        holdObjects[0].transform.SetParent(leftHandHoldSpace);

                        var localPlacement = holdObjects[0].transform.localPosition;

                        holdObjects[0].transform.SetParent(rightHandHoldSpace);
                        holdObjects[0].transform.localPosition = localPlacement;

                        var readjustVector = holdObjects[0].transform.localPosition;
                        var readjustRotation = holdObjects[0].transform.localRotation;

                        readjustVector.x *= -1.0f;
                        readjustRotation.y *= -1.0f;
                        readjustRotation.z *= -1.0f;

                        holdObjects[0].transform.localPosition = readjustVector;
                        holdObjects[0].transform.localRotation = readjustRotation;
                    }
                }
            }
            else
            {
                if (gameSettings.selectedGamePlayMode == GamePlayMode.Battle)
                {
                    if (holdObjects != null)
                    {
                        holdObjects[0].SetActive(true);

                        holdObjects[0].transform.SetParent(leftHandHoldSpace);
                    }
                }
            }
        }

        public void InitializeValues()
        {
            originalVelocityLimit = velocityLimit;
            originalJumpMultiplier = jumpMultiplier;

            playerRigidBody = GetComponent<Rigidbody>();

            velocityHistory = new Vector3[velocityHistorySize];
            velocityIndex = 0;

            lastHeadPosition = headCollider.transform.position;
            lastPosition = transform.position;
            lastLeftHandPosition = leftHandFollower.transform.position;
            lastRightHandPosition = rightHandFollower.transform.position;

            previousLeftHandLocalPos = headCollider.transform.InverseTransformPoint(leftHandTransform.position);
            previousRightHandLocalPos = headCollider.transform.InverseTransformPoint(rightHandTransform.position);

            if (playerSkins.playerSkinDictionary.Count == 0 || playerSkins.playerSkinDictionary == null)
            {
                playerSkins.FillDictionary();
            }
        }

        #region Hand and Body Positioning
        private Vector3 CurrentLeftHandPosition()
        {
            if ((PositionWithOffset(leftHandTransform, leftHandOffset) - headCollider.transform.position).magnitude < maxArmLength)
            {
                return PositionWithOffset(leftHandTransform, leftHandOffset);
            }
            else
            {
                return headCollider.transform.position + (PositionWithOffset(leftHandTransform, leftHandOffset) - headCollider.transform.position).normalized * maxArmLength;
            }
        }

        private Vector3 CurrentRightHandPosition()
        {
            if ((PositionWithOffset(rightHandTransform, rightHandOffset) - headCollider.transform.position).magnitude < maxArmLength)
            {
                return PositionWithOffset(rightHandTransform, rightHandOffset);
            }
            else
            {
                return headCollider.transform.position + (PositionWithOffset(rightHandTransform, rightHandOffset) - headCollider.transform.position).normalized * maxArmLength;
            }
        }

        private Vector3 PositionWithOffset(Transform transformToModify, Vector3 offsetVector)
        {
            return transformToModify.position + transformToModify.rotation * offsetVector;
        }
        #endregion

        private void Update()
        {
            //if (!HasInputAuthority) return;

            if (!debugMultiplayerTesting)
            {
                bool leftHandColliding = false;
                bool rightHandColliding = false;

                rigidbodyMovement = Vector3.zero;
                Vector3 finalPosition;
                Vector3 firstIterationLeftHand = Vector3.zero;
                Vector3 firstIterationRightHand = Vector3.zero;

                RaycastHit hitInfo;

                bodyCollider.transform.eulerAngles = new Vector3(0, headCollider.transform.eulerAngles.y, 0);

                if (optionSettings.turnModeSelected == TurnMode.Smooth)
                {
                    var dominantHand = optionSettings.GetPrimaryHand(leftHandDevice, rightHandDevice);
                    Vector2 value;

                    if (dominantHand.device.TryReadAxis2DValue(InputHelpers.Axis2D.PrimaryAxis2D, out value))
                    {
                        smoothedTurnInput = Mathf.Lerp(smoothedTurnInput, value.x, Time.deltaTime / 0.1f);

                        if (Mathf.Abs(smoothedTurnInput) > 0.1f)
                        {
                            currentTurnSpeed = Mathf.SmoothDamp(currentTurnSpeed, 45.0f, ref turnAccelerationVelocity, 0.2f);

                            transform.Rotate(Vector3.up, value.x * currentTurnSpeed * Time.deltaTime);
                        }
                        else
                        {
                            currentTurnSpeed = Mathf.SmoothDamp(currentTurnSpeed, 0.0f, ref turnAccelerationVelocity, 0.2f);
                        }
                    }
                }
                else if (optionSettings.turnModeSelected == TurnMode.SnapTurn)
                {
                    var dominantHand = optionSettings.GetPrimaryHand(leftHandDevice, rightHandDevice);
                    Vector2 value;

                    if (Time.time - lastSnapTime < gameSettings.snapTurnCooldown) return;

                    if (dominantHand.device.TryReadAxis2DValue(InputHelpers.Axis2D.PrimaryAxis2D, out value))
                    {
                        if (value.x > 0.5f)
                        {
                            transform.Rotate(Vector3.up, gameSettings.snapTurnAngle);
                            lastSnapTime = Time.time;
                        }
                        else if (value.x < -0.5f)
                        {
                            transform.Rotate(Vector3.up, -gameSettings.snapTurnAngle);
                            lastSnapTime = Time.time;
                        }
                    }
                }

                //Left Hand Logic
                Vector3 distanceTraveled = CurrentLeftHandPosition() - lastLeftHandPosition + Vector3.down * 2f * 9.8f * Time.deltaTime * Time.deltaTime;

                if (IterativeCollisionSphereCast(lastLeftHandPosition, minimumRaycastDistance, distanceTraveled, defaultPrecision, out finalPosition, true))
                {
                    //this lets you stick to the position you touch, as long as you keep touching the surface this will be the zero point for that hand
                    if (wasLeftHandTouching)
                    {
                        firstIterationLeftHand = lastLeftHandPosition - CurrentLeftHandPosition();
                    }
                    else
                    {
                        firstIterationLeftHand = finalPosition - CurrentLeftHandPosition();
                    }

                    if (HasInputAuthority)
                    {
                        playerRigidBody.velocity = Vector3.zero;
                    }

                    leftHandColliding = true;
                }

                //Right Hand Logic
                distanceTraveled = CurrentRightHandPosition() - lastRightHandPosition + Vector3.down * 2f * 9.8f * Time.deltaTime * Time.deltaTime;

                if (IterativeCollisionSphereCast(lastRightHandPosition, minimumRaycastDistance, distanceTraveled, defaultPrecision, out finalPosition, true))
                {
                    if (wasRightHandTouching)
                    {
                        firstIterationRightHand = lastRightHandPosition - CurrentRightHandPosition();
                    }
                    else
                    {
                        firstIterationRightHand = finalPosition - CurrentRightHandPosition();
                    }

                    if (HasInputAuthority)
                    {
                        playerRigidBody.velocity = Vector3.zero;
                    }

                    rightHandColliding = true;
                }

                //average or add

                if ((leftHandColliding || wasLeftHandTouching) && (rightHandColliding || wasRightHandTouching))
                {
                    //this lets you grab stuff with both hands at the same time
                    rigidbodyMovement = (firstIterationLeftHand + firstIterationRightHand) / 2;
                }
                else
                {
                    rigidbodyMovement = firstIterationLeftHand + firstIterationRightHand;
                }

                //check valid head movement

                if (IterativeCollisionSphereCast(lastHeadPosition, headCollider.radius, headCollider.transform.position + rigidbodyMovement - lastHeadPosition, defaultPrecision, out finalPosition, false))
                {
                    rigidbodyMovement = finalPosition - lastHeadPosition;
                    //last check to make sure the head won't phase through geometry
                    if (Physics.Raycast(lastHeadPosition, headCollider.transform.position - lastHeadPosition + rigidbodyMovement, out hitInfo, (headCollider.transform.position - lastHeadPosition + rigidbodyMovement).magnitude + headCollider.radius * defaultPrecision * 0.999f, locomotionEnabledLayers.value))
                    {
                        rigidbodyMovement = lastHeadPosition - headCollider.transform.position;
                    }
                }

                if (rigidbodyMovement != Vector3.zero)
                {
                    transform.position = transform.position + rigidbodyMovement;
                }

                lastHeadPosition = headCollider.transform.position;

                //do final left hand position

                distanceTraveled = CurrentLeftHandPosition() - lastLeftHandPosition;

                if (IterativeCollisionSphereCast(lastLeftHandPosition, minimumRaycastDistance, distanceTraveled, defaultPrecision, out finalPosition, !((leftHandColliding || wasLeftHandTouching) && (rightHandColliding || wasRightHandTouching))))
                {
                    lastLeftHandPosition = finalPosition;
                    leftHandColliding = true;
                }
                else
                {
                    lastLeftHandPosition = CurrentLeftHandPosition();
                }

                //do final right hand position

                distanceTraveled = CurrentRightHandPosition() - lastRightHandPosition;

                if (IterativeCollisionSphereCast(lastRightHandPosition, minimumRaycastDistance, distanceTraveled, defaultPrecision, out finalPosition, !((leftHandColliding || wasLeftHandTouching) && (rightHandColliding || wasRightHandTouching))))
                {
                    lastRightHandPosition = finalPosition;
                    rightHandColliding = true;
                }
                else
                {
                    lastRightHandPosition = CurrentRightHandPosition();
                }

                StoreVelocities();

                if ((rightHandColliding || leftHandColliding) && !disableMovement)
                {
                    if (denormalizedVelocityAverage.magnitude > velocityLimit)
                    {
                        if (denormalizedVelocityAverage.magnitude * jumpMultiplier > maxJumpSpeed)
                        {
                            if (HasInputAuthority)
                            {
                                playerRigidBody.velocity = denormalizedVelocityAverage.normalized * maxJumpSpeed;
                            }
                        }
                        else
                        {
                            if (HasInputAuthority)
                            {
                                playerRigidBody.velocity = jumpMultiplier * denormalizedVelocityAverage;
                            }
                        }
                    }
                }

                //check to see if left hand is stuck and we should unstick it

                if (leftHandColliding && (CurrentLeftHandPosition() - lastLeftHandPosition).magnitude > unStickDistance && !Physics.SphereCast(headCollider.transform.position, minimumRaycastDistance * defaultPrecision, CurrentLeftHandPosition() - headCollider.transform.position, out hitInfo, (CurrentLeftHandPosition() - headCollider.transform.position).magnitude - minimumRaycastDistance, locomotionEnabledLayers.value))
                {
                    lastLeftHandPosition = CurrentLeftHandPosition();
                    leftHandColliding = false;
                }

                //check to see if right hand is stuck and we should unstick it

                if (rightHandColliding && (CurrentRightHandPosition() - lastRightHandPosition).magnitude > unStickDistance && !Physics.SphereCast(headCollider.transform.position, minimumRaycastDistance * defaultPrecision, CurrentRightHandPosition() - headCollider.transform.position, out hitInfo, (CurrentRightHandPosition() - headCollider.transform.position).magnitude - minimumRaycastDistance, locomotionEnabledLayers.value))
                {
                    lastRightHandPosition = CurrentRightHandPosition();
                    rightHandColliding = false;
                }

                leftHandFollower.position = lastLeftHandPosition;
                rightHandFollower.position = lastRightHandPosition;

                wasLeftHandTouching = leftHandColliding;
                wasRightHandTouching = rightHandColliding;

                previousLeftHandLocalPos = headCollider.transform.InverseTransformPoint(leftHandTransform.position);
                previousRightHandLocalPos = headCollider.transform.InverseTransformPoint(rightHandTransform.position);
            }
        }

        public override void FixedUpdateNetwork()
        {
            if (IsAfflicted && afflictionTimer.Expired(Runner))
            {
                IsAfflicted = false;
            }

            if (IsInfected)
            {
                foreach (var player in FindObjectsOfType<Player>())
                {
                    if (player == this || player.IsInfected) continue;

                    float distance = Vector3.Distance(transform.position, player.transform.position);

                    if (distance <= tagRange)
                    {
                        player.RPC_BecomeInfected();
                    }
                }
            }

            //Battle Mode Specific Update Tracking
            if (IsStunned)
            {
                stunTimer -= Runner.DeltaTime;

                if (stunTimer <= 0.0f)
                {
                    IsStunned = false;
                }
            }

            if (IsInvulnerable)
            {
                invulnTimer -= Runner.DeltaTime;

                if (invulnTimer <= 0.0f)
                {
                    IsInvulnerable = false;
                }
            }

            if (IsSlowed)
            {
                slowTimer -= Runner.DeltaTime;

                if (slowTimer <= 0.0f)
                {
                    IsSlowed = false;
                }
            }
        }

        #region XR Controller Input
        public bool CheckIfPrimaryButtonPressed()
        {
            bool primaryButtonPressed = false;

            if (rightHandDevice.device.IsPressed(InputHelpers.Button.PrimaryButton, out primaryButtonPressed, 0.1f))
            {
                return primaryButtonPressed;
            }

            return primaryButtonPressed;
        }

        public bool CheckIfSecondaryButtonPressed()
        {
            bool secondaryButtonPressed = false;

            if (rightHandDevice.device.IsPressed(InputHelpers.Button.SecondaryButton, out secondaryButtonPressed, 0.1f))
            {
                return secondaryButtonPressed;
            }

            return secondaryButtonPressed;
        }

        public bool CheckIfLeftTriggerPressed()
        {
            bool leftTriggerPressed = false;

            if (leftHandDevice.device.IsPressed(InputHelpers.Button.Trigger, out leftTriggerPressed, 0.1f))
            {
                return leftTriggerPressed;
            }

            return leftTriggerPressed;
        }

        public bool CheckIfRightTriggerPressed()
        {
            bool rightTriggerPressed = false;

            if (rightHandDevice.device.IsPressed(InputHelpers.Button.Trigger, out rightTriggerPressed, 0.1f))
            {
                return rightTriggerPressed;
            }

            return rightTriggerPressed;
        }

        public bool CheckIfPlayerHoldingTriggers()
        {
            if (leftHandDevice.device.IsPressed(InputHelpers.Button.Trigger, out leftTriggerHeld, 0.1f) && rightHandDevice.device.IsPressed(InputHelpers.Button.Trigger, out rightTriggerHeld, 0.1f))
            {
                return leftTriggerHeld && rightTriggerHeld;
            }

            return false;
        }

        public void SendHapticImpulse(XRControllerInputDevice controller, float amplitude, float duration)
        {
            if (controller != null && controller.device.isValid)
            {
                controller.device.SendHapticImpulse(0u, amplitude, duration);
            }
        }
        #endregion

        #region Conditional Checks
        public bool CheckIfGrounded()
        {
            return Physics.Raycast(bodyCollider.gameObject.transform.position, -Vector3.up, 0.5f);
        }

        public bool IsHandTouching(bool forLeftHand)
        {
            if (forLeftHand)
            {
                return wasLeftHandTouching;
            }
            else
            {
                return wasRightHandTouching;
            }
        }
        #endregion

        #region Rpc Networking Status Checks
        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        public void RPC_BecomeInfected()
        {
            if (IsInfected) return;

            IsInfected = true;
            TagModeManager.Instance.TagPlayer(Object.InputAuthority);
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        public void RPC_TakeHit(PlayerRef attacker)
        {
            if (IsInvulnerable || IsEliminated) return;

            lives--;
            Debug.Log($"Player hit! Lives remaining: {lives}");

            if (lives <= 0 && !IsEliminated)
            {
                IsEliminated = true;

                //Elimination visuals and sounds here
                IsStunned = false;
                HasBuff = false;

                BattleModeManager.Instance?.PlayerHit(Object.InputAuthority, attacker);
            }
            else if (lives <= 0 && IsEliminated)
            {
                return;
            }

            IsStunned = true;
            stunTimer = stunDuration;

            IsInvulnerable = true;
            invulnTimer = invulnerabilityDuration;

            BattleModeManager.Instance?.PlayerHit(Object.InputAuthority, attacker);
        }
        #endregion

        #region Player Collision and Velocity Calculations        
        private bool IterativeCollisionSphereCast(Vector3 startPosition, float sphereRadius, Vector3 movementVector, float precision, out Vector3 endPosition, bool singleHand)
        {
            RaycastHit hitInfo;
            Vector3 movementToProjectedAboveCollisionPlane;
            Surface gorillaSurface;
            float slipPercentage;
            //first spherecast from the starting position to the final position
            if (CollisionsSphereCast(startPosition, sphereRadius * precision, movementVector, precision, out endPosition, out hitInfo))
            {
                //if we hit a surface, do a bit of a slide. this makes it so if you grab with two hands you don't stick 100%, and if you're pushing along a surface while braced with your head, your hand will slide a bit

                //take the surface normal that we hit, then along that plane, do a spherecast to a position a small distance away to account for moving perpendicular to that surface
                Vector3 firstPosition = endPosition;
                gorillaSurface = hitInfo.collider.GetComponent<Surface>();
                slipPercentage = gorillaSurface != null ? gorillaSurface.slipPercentage : (!singleHand ? defaultSlideFactor : 0.001f);
                movementToProjectedAboveCollisionPlane = Vector3.ProjectOnPlane(startPosition + movementVector - firstPosition, hitInfo.normal) * slipPercentage;
                if (CollisionsSphereCast(endPosition, sphereRadius, movementToProjectedAboveCollisionPlane, precision * precision, out endPosition, out hitInfo))
                {
                    //if we hit trying to move perpendicularly, stop there and our end position is the final spot we hit
                    return true;
                }
                //if not, try to move closer towards the true point to account for the fact that the movement along the normal of the hit could have moved you away from the surface
                else if (CollisionsSphereCast(movementToProjectedAboveCollisionPlane + firstPosition, sphereRadius, startPosition + movementVector - (movementToProjectedAboveCollisionPlane + firstPosition), precision * precision * precision, out endPosition, out hitInfo))
                {
                    //if we hit, then return the spot we hit
                    return true;
                }
                else
                {
                    //this shouldn't really happe, since this means that the sliding motion got you around some corner or something and let you get to your final point. back off because something strange happened, so just don't do the slide
                    endPosition = firstPosition;
                    return true;
                }
            }
            //as kind of a sanity check, try a smaller spherecast. this accounts for times when the original spherecast was already touching a surface so it didn't trigger correctly
            else if (CollisionsSphereCast(startPosition, sphereRadius * precision * 0.66f, movementVector.normalized * (movementVector.magnitude + sphereRadius * precision * 0.34f), precision * 0.66f, out endPosition, out hitInfo))
            {
                endPosition = startPosition;
                return true;
            }
            else
            {
                endPosition = Vector3.zero;
                return false;
            }
        }

        private bool CollisionsSphereCast(Vector3 startPosition, float sphereRadius, Vector3 movementVector, float precision, out Vector3 finalPosition, out RaycastHit hitInfo)
        {
            //kind of like a souped up spherecast. includes checks to make sure that the sphere we're using, if it touches a surface, is pushed away the correct distance (the original sphereradius distance). since you might
            //be pushing into sharp corners, this might not always be valid, so that's what the extra checks are for

            //initial spherecase
            RaycastHit innerHit;
            if (Physics.SphereCast(startPosition, sphereRadius * precision, movementVector, out hitInfo, movementVector.magnitude + sphereRadius * (1 - precision), locomotionEnabledLayers.value))
            {
                //if we hit, we're trying to move to a position a sphereradius distance from the normal
                finalPosition = hitInfo.point + hitInfo.normal * sphereRadius;

                //check a spherecase from the original position to the intended final position
                if (Physics.SphereCast(startPosition, sphereRadius * precision * precision, finalPosition - startPosition, out innerHit, (finalPosition - startPosition).magnitude + sphereRadius * (1 - precision * precision), locomotionEnabledLayers.value))
                {
                    finalPosition = startPosition + (finalPosition - startPosition).normalized * Mathf.Max(0, hitInfo.distance - sphereRadius * (1f - precision * precision));
                    hitInfo = innerHit;
                }
                //bonus raycast check to make sure that something odd didn't happen with the spherecast. helps prevent clipping through geometry
                else if (Physics.Raycast(startPosition, finalPosition - startPosition, out innerHit, (finalPosition - startPosition).magnitude + sphereRadius * precision * precision * 0.999f, locomotionEnabledLayers.value))
                {
                    finalPosition = startPosition;
                    hitInfo = innerHit;
                    return true;
                }
                return true;
            }
            //anti-clipping through geometry check
            else if (Physics.Raycast(startPosition, movementVector, out hitInfo, movementVector.magnitude + sphereRadius * precision * 0.999f, locomotionEnabledLayers.value))
            {
                finalPosition = startPosition;
                return true;
            }
            else
            {
                finalPosition = Vector3.zero;
                return false;
            }
        }

        private void StoreVelocities()
        {
            if (velocityHistorySize != 0)
            {
                velocityIndex = (velocityIndex + 1) % velocityHistorySize;

                Vector3 oldestVelocity = velocityHistory[velocityIndex];
                currentVelocity = (transform.position - lastPosition) / Time.deltaTime;
                denormalizedVelocityAverage += (currentVelocity - oldestVelocity) / (float)velocityHistorySize;
                velocityHistory[velocityIndex] = currentVelocity;
                lastPosition = transform.position;
            }
        }
        #endregion

        #region Player Status Checks        
        public void OnInfectedChanged()
        {
            if (IsInfected)
            {
                currentPlayerStatuses.Add(PlayerStatus.Rabid.ToString(), PlayerStatus.Rabid);
                statusKeys.Add(PlayerStatus.Rabid.ToString());

                UpdatePlayerSkin(SkinType.Rabid);
            }
            else
            {
                currentPlayerStatuses.Remove(PlayerStatus.Rabid.ToString());
                statusKeys.Remove(PlayerStatus.Rabid.ToString());

                UpdatePlayerSkin(SkinType.Default);
            }
        }

        public void OnSlowChanged()
        {
            if (IsSlowed)
            {
                currentPlayerStatuses.Add(PlayerStatus.Slowed.ToString(), PlayerStatus.Slowed);
                statusKeys.Add(PlayerStatus.Slowed.ToString());

                slowTimer = slowDuration;
                wasBuffed = HasBuff;

                velocityLimit *= slowMultiplier;
                jumpMultiplier *= slowMultiplier;

                Debug.Log("Player is slowed!");
            }
            else
            {
                currentPlayerStatuses.Remove(PlayerStatus.Slowed.ToString());
                statusKeys.Remove(PlayerStatus.Slowed.ToString());

                if (wasBuffed)
                {
                    HasBuff = true;
                    wasBuffed = false;
                }

                velocityLimit = originalVelocityLimit;
                jumpMultiplier = originalJumpMultiplier;
            }
        }

        public void OnStunChanged()
        {
            if (IsStunned)
            {
                currentPlayerStatuses.Add(PlayerStatus.Stunned.ToString(), PlayerStatus.Stunned);
                statusKeys.Add(PlayerStatus.Stunned.ToString());

                Debug.Log("Player has been stunned!");

                disableMovement = true; //Disables Gorilla Locomotion temporarily
            }
            else
            {
                currentPlayerStatuses.Remove(PlayerStatus.Stunned.ToString());
                statusKeys.Remove(PlayerStatus.Stunned.ToString());

                disableMovement = false;
            }

            //TODO: Visuals for any stun effects
        }

        public void OnLastOfKindChanged()
        {
            if (HasBuff)
            {
                currentPlayerStatuses.Add(PlayerStatus.Buffed.ToString(), PlayerStatus.Buffed);
                statusKeys.Add(PlayerStatus.Buffed.ToString());

                velocityLimit *= 1.25f;
                jumpMultiplier *= 1.25f;
            }
            else
            {
                currentPlayerStatuses.Remove(PlayerStatus.Buffed.ToString());
                statusKeys.Remove(PlayerStatus.Buffed.ToString());

                velocityLimit = originalVelocityLimit;
                jumpMultiplier = originalJumpMultiplier;
            }
        }

        public void OnInvulnerabilityChanged()
        {
            if (IsInvulnerable)
            {
                currentPlayerStatuses.Add(PlayerStatus.Invulnerable.ToString(), PlayerStatus.Invulnerable);
                statusKeys.Add(PlayerStatus.Invulnerable.ToString());

                Debug.Log("Player is currently invulnerable after being hit.");
            }
            else
            {
                currentPlayerStatuses.Remove(PlayerStatus.Invulnerable.ToString());
                statusKeys.Remove(PlayerStatus.Invulnerable.ToString());
            }
        }

        public void OnHasBeenEliminated()
        {
            if (IsEliminated)
            {
                currentPlayerStatuses.Add(PlayerStatus.Eliminated.ToString(), PlayerStatus.Eliminated);
                statusKeys.Add(PlayerStatus.Eliminated.ToString());

                UpdatePlayerSkin(SkinType.Eliminated);
            }
            else
            {
                currentPlayerStatuses.Remove(PlayerStatus.Eliminated.ToString());
                statusKeys.Remove(PlayerStatus.Eliminated.ToString());

                UpdatePlayerSkin(SkinType.Default);
            }
        }

        private void UpdatePlayerSkin(SkinType skinType)
        {
            PlayerSkin newPlayerSkin = null;
            var playerMaterials = playerSkinRenderer.materials;

            switch (skinType)
            {
                case SkinType.Default:
                    newPlayerSkin = playerSkins.GetSpecificSkin("Default" + gameSettings.selectedCryptidCharacter.ToString() + "Skin");
                    break;
                case SkinType.Rabid:
                    newPlayerSkin = playerSkins.GetSpecificSkin("Rabid" + gameSettings.selectedCryptidCharacter.ToString() + "Skin");
                    break;
                case SkinType.Eliminated:
                    newPlayerSkin = playerSkins.GetSpecificSkin("Eliminated" + gameSettings.selectedCryptidCharacter.ToString() + "Skin");
                    break;
                case SkinType.Alt1:
                    newPlayerSkin = playerSkins.GetSpecificSkin("Alt1" + gameSettings.selectedCryptidCharacter.ToString() + "Skin");
                    break;
                default:
                    break;
            }

            for (int i = 0; i < newPlayerSkin.skinColors.Count; i++)
            {
                playerMaterials[i] = newPlayerSkin.skinColors[i];
            }

            playerSkinRenderer.materials = playerMaterials;
        }

        private void UpdateNameplate()
        {
            string fullPrefixString = "";
            Color textColorForDisplay = Color.white;

            for (int i = 0; i < statusKeys.Count; i++)
            {
                var statusType = currentPlayerStatuses[statusKeys[i]];

                switch (statusType)
                {
                    case PlayerStatus.Rabid:
                        textColorForDisplay = Color.red;
                        break;
                    case PlayerStatus.Stunned:
                        textColorForDisplay = Color.yellow;
                        break;
                    case PlayerStatus.Invulnerable:
                        textColorForDisplay = Color.cyan;
                        break;
                    case PlayerStatus.Buffed:
                        textColorForDisplay = Color.blue;
                        break;
                    case PlayerStatus.Slowed:
                        textColorForDisplay = Color.gray;
                        break;
                    case PlayerStatus.Eliminated:
                        textColorForDisplay = Color.magenta;
                        break;
                    default:
                        break;
                }

                if (i == 0)
                {
                    fullPrefixString += $"[<color={textColorForDisplay}>" + statusKeys[i] + "</color>";
                }
                else if (i != 0 && i != currentPlayerStatuses.Count - 1)
                {
                    fullPrefixString += $", <color={textColorForDisplay}>" + statusKeys[i] + "</color>";
                }
                else if (i == currentPlayerStatuses.Count - 1)
                {
                    fullPrefixString += $", <color={textColorForDisplay}>" + statusKeys[i] + "</color>]";
                }
            }

            namePlateHandler.UpdatePlayerPrefix(fullPrefixString);
        }

        public void ApplyMagicEffect(float duration)
        {
            if (!HasInputAuthority) return;

            IsAfflicted = true;
            afflictionTimer = TickTimer.CreateFromSeconds(Runner, duration);

            Debug.Log($"Player is stunned for {duration} seconds!");
        }
        #endregion

        #region Unused logic
        /*public void Turn(float degrees)
        {
            transform.RotateAround(headCollider.transform.position, transform.up, degrees);
            denormalizedVelocityAverage = Quaternion.Euler(0, degrees, 0) * denormalizedVelocityAverage;
            for (int i = 0; i < velocityHistory.Length; i++)
            {
                velocityHistory[i] = Quaternion.Euler(0, degrees, 0) * velocityHistory[i];
            }
        }*/
        #endregion
    }
}
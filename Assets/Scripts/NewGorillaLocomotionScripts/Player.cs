namespace GorillaLocomotion
{
    using UnityEngine;
    using UnityEngine.InputSystem;
    using UnityEngine.XR.Interaction.Toolkit;
    using UnityEngine.XR;
    using Fusion.XR.Shared.Rig;
    using Fusion;
    using System.Collections.Generic;

    public class Player : NetworkBehaviour
    {
        private static Player _instance;

        public static Player Instance { get { return _instance; } }

        public SphereCollider headCollider;
        public CapsuleCollider bodyCollider;

        public Transform leftHandFollower;
        public Transform rightHandFollower;

        public Transform rightHandTransform;
        public Transform leftHandTransform;
        
        private Vector3 lastLeftHandPosition;
        private Vector3 lastRightHandPosition;
        private Vector3 lastHeadPosition;

        private Rigidbody playerRigidBody;

        public int velocityHistorySize;
        public float maxArmLength = 1.5f;
        public float unStickDistance = 1f;

        public float velocityLimit;
        public float maxJumpSpeed;
        public float jumpMultiplier;
        public float minimumRaycastDistance = 0.05f;
        public float defaultSlideFactor = 0.03f;
        public float defaultPrecision = 0.995f;

        private Vector3[] velocityHistory;
        private int velocityIndex;
        private Vector3 currentVelocity;
        private Vector3 denormalizedVelocityAverage;
        private bool jumpHandIsLeft;
        private Vector3 lastPosition;

        private NetworkRunner runner;

        public Vector3 rightHandOffset;
        public Vector3 leftHandOffset;

        public LayerMask locomotionEnabledLayers;

        public bool wasLeftHandTouching;
        public bool wasRightHandTouching;

        public bool disableMovement = false;

        public LayerMask affectablePlayerLayer;

        public XRControllerInputDevice leftHandDevice;
        public XRControllerInputDevice rightHandDevice;
        public List<GameObject> playerHands = new List<GameObject>();
        private bool leftTriggerHeld = false;
        private bool rightTriggerHeld = false;
        [Networked] public bool IsAfflicted { get; set; }
        private TickTimer afflictionTimer;

        #region Alien Unique Variables
        public NetworkPrefabRef beaconPrefab;
        private NetworkObject activeBeacon;
        private float beaconCooldown = 5.0f;
        private float lastBeaconDropTime = 10.0f;
        private bool justRecalled = false;
        #endregion

        #region Bigfoot Unique Variables
        private float slamCooldown = 5.0f;
        private float lastSlamTime = 10.0f;

        private float slamRadius = 5.0f;
        private float slamForce = 10.0f;
        #endregion

        #region Frogman Unique Variables
        private float spellCooldown = 5.0f;
        private float spellRange = 10.0f;
        private float effectDuration = 2.0f;
        private float lastSpellUseTime = 10.0f;
        public Transform wandTipPoint;
        public NetworkPrefabRef magicProjectilePrefab;
        #endregion

        #region Mothman Unique Variables
        private int currentWingFlapsRemaining = 5;
        private int maxWingFlaps = 5;

        private bool hasRecentlyFlapped = false;
        private float flapCooldown = 0.25f;
        private float lastFlapTime = 0.0f;

        private float glideFallSpeed = -2.0f;

        private Vector3 previousLeftHandLocalPos;
        private Vector3 previousRightHandLocalPos;
        #endregion

        private CryptidType cryptidType;
        private CryptidCharacterType cryptidCharacterType;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
            }
            else
            {
                _instance = this;
            }
            InitializeValues();
        }

        public override void Spawned()
        {
            runner = Runner;

            if (!HasInputAuthority) return;
        }

        public void InitializeValues()
        {
            var cryptidType = GetComponent<CryptidType>();

            if (cryptidType != null)
            {
                cryptidCharacterType = cryptidType.cryptidCharacterType;
            }

            playerRigidBody = GetComponent<Rigidbody>();
            velocityHistory = new Vector3[velocityHistorySize];
            lastLeftHandPosition = leftHandFollower.transform.position;
            lastRightHandPosition = rightHandFollower.transform.position;
            lastHeadPosition = headCollider.transform.position;
            velocityIndex = 0;
            lastPosition = transform.position;

            previousLeftHandLocalPos = headCollider.transform.InverseTransformPoint(leftHandTransform.position);
            previousRightHandLocalPos = headCollider.transform.InverseTransformPoint(rightHandTransform.position);
        }

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

        private void Update()
        {
            bool leftHandColliding = false;
            bool rightHandColliding = false;
            Vector3 finalPosition;
            Vector3 rigidBodyMovement = Vector3.zero;
            Vector3 firstIterationLeftHand = Vector3.zero;
            Vector3 firstIterationRightHand = Vector3.zero;
            RaycastHit hitInfo;

            bodyCollider.transform.eulerAngles = new Vector3(0, headCollider.transform.eulerAngles.y, 0);

            if (IsAfflicted)
            {
                //Show affliction effect here
                return;
            }
            else
            {
                //Remove affliction effect here
            }

            //left hand

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
                playerRigidBody.velocity = Vector3.zero;

                leftHandColliding = true;
            }

            //right hand

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

                playerRigidBody.velocity = Vector3.zero;

                rightHandColliding = true;
            }

            //average or add

            if ((leftHandColliding || wasLeftHandTouching) && (rightHandColliding || wasRightHandTouching))
            {
                //this lets you grab stuff with both hands at the same time
                rigidBodyMovement = (firstIterationLeftHand + firstIterationRightHand) / 2;
            }
            else
            {
                rigidBodyMovement = firstIterationLeftHand + firstIterationRightHand;
            }

            //check valid head movement

            if (IterativeCollisionSphereCast(lastHeadPosition, headCollider.radius, headCollider.transform.position + rigidBodyMovement - lastHeadPosition, defaultPrecision, out finalPosition, false))
            {
                rigidBodyMovement = finalPosition - lastHeadPosition;
                //last check to make sure the head won't phase through geometry
                if (Physics.Raycast(lastHeadPosition, headCollider.transform.position - lastHeadPosition + rigidBodyMovement, out hitInfo, (headCollider.transform.position - lastHeadPosition + rigidBodyMovement).magnitude + headCollider.radius * defaultPrecision * 0.999f, locomotionEnabledLayers.value))
                {
                    rigidBodyMovement = lastHeadPosition - headCollider.transform.position;
                }
            }

            if (justRecalled)
            {
                justRecalled = false;
            }
            else if (rigidBodyMovement != Vector3.zero)
            {
                transform.position = transform.position + rigidBodyMovement;
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
                        playerRigidBody.velocity = denormalizedVelocityAverage.normalized * maxJumpSpeed;
                    }
                    else
                    {
                        playerRigidBody.velocity = jumpMultiplier * denormalizedVelocityAverage;
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

            if (cryptidCharacterType == CryptidCharacterType.Bigfoot)
            {
                HandleBigfootAbility();
            }
            else if (cryptidCharacterType == CryptidCharacterType.Mothman)
            {
                HandleMothmanAbility();
            }
            else if (cryptidCharacterType == CryptidCharacterType.Alien)
            {
                HandleAlienAbility();
            }
            else if (cryptidCharacterType == CryptidCharacterType.Frogman)
            {
                HandleFrogmanAbility();
            }

            leftHandFollower.position = lastLeftHandPosition;
            rightHandFollower.position = lastRightHandPosition;

            wasLeftHandTouching = leftHandColliding;
            wasRightHandTouching = rightHandColliding;

            previousLeftHandLocalPos = headCollider.transform.InverseTransformPoint(leftHandTransform.position);
            previousRightHandLocalPos = headCollider.transform.InverseTransformPoint(rightHandTransform.position);
        }

        public override void FixedUpdateNetwork()
        {
            if (IsAfflicted && afflictionTimer.Expired(Runner))
            {
                IsAfflicted = false;
            }
        }

        private void HandleBigfootAbility()
        {
            if (lastSlamTime < slamCooldown)
            {
                lastSlamTime += Time.deltaTime;
            }
            else
            {
                if (CheckIfGrounded() && CheckIfPlayerHoldingTriggers())
                {
                    bool bothHandsSlammedDown = wasLeftHandTouching && wasRightHandTouching;

                    if (bothHandsSlammedDown)
                    {
                        Debug.Log("Officially using slam ability!");

                        lastSlamTime = 0.0f;

                        Vector3 origin = transform.position;

                        Collider[] hitColliders = Physics.OverlapSphere(origin, slamRadius, affectablePlayerLayer);

                        foreach (var hit in hitColliders)
                        {
                            if (hit.attachedRigidbody && hit.gameObject != gameObject)
                            {
                                Vector3 direction = (hit.transform.position - origin).normalized;
                                hit.attachedRigidbody.AddForce(direction * slamForce, ForceMode.Impulse);
                            }
                        }
                    }
                }
            }
        }

        private void HandleMothmanAbility()
        {
            if (CheckIfGrounded() && currentWingFlapsRemaining < maxWingFlaps)
            {
                currentWingFlapsRemaining = maxWingFlaps;
            }

            var currentLeftLocal = headCollider.transform.InverseTransformPoint(leftHandTransform.position);
            var currentRightLocal = headCollider.transform.InverseTransformPoint(rightHandTransform.position);

            float leftDownSpeed = previousLeftHandLocalPos.y - currentLeftLocal.y;
            float rightDownSpeed = previousRightHandLocalPos.y - currentRightLocal.y;

           // Debug.Log("LEft Down Speed: " + leftDownSpeed);
            //Debug.Log("Right Down Speed: " + rightDownSpeed);

            bool bothHandsFlapping = leftDownSpeed > 0.0001f && rightDownSpeed > 0.0001f;
            //bool handsRaised = leftVelocity > 0.005f && rightVelocity > 0.005f;

            bool handsRaised = false;

            if (lastFlapTime < flapCooldown)
            {
                lastFlapTime += Time.deltaTime;
            }
            else
            {
                if (bothHandsFlapping && !hasRecentlyFlapped && currentWingFlapsRemaining > 0)
                {
                    Vector3 headForward = headCollider.transform.forward;
                    Vector3 flapDirection = (Vector3.up * 1.2f + headForward * 0.8f).normalized;

                    playerRigidBody.velocity = flapDirection * 10.0f;

                    currentWingFlapsRemaining--;
                    lastFlapTime = 0.0f;
                    hasRecentlyFlapped = true;

                    Debug.Log("MOTHMAN IS FLAPPING! FLAPS REMAINING: " + currentWingFlapsRemaining);
                }
                else if (!bothHandsFlapping)
                {
                    hasRecentlyFlapped = false;
                }
            }

            if (!handsRaised && currentWingFlapsRemaining < maxWingFlaps)
            {
                if (playerRigidBody.velocity.y < glideFallSpeed)
                {
                    playerRigidBody.velocity = new Vector3(playerRigidBody.velocity.x, glideFallSpeed, playerRigidBody.velocity.z);
                }
            }
        }

        private void HandleFrogmanAbility()
        {
            if (lastSpellUseTime < spellCooldown)
            {
                lastSpellUseTime += Time.deltaTime;
            }
            else
            {
                //Maybe add a condition to check if player is holding Trigger to "prime" wand
                if (CheckIfPrimaryButtonPressed())
                {
                    lastSpellUseTime = 0.0f;

                    NetworkObject magicProjectile = runner.Spawn(
                        magicProjectilePrefab,
                        wandTipPoint.position,
                        Quaternion.LookRotation(wandTipPoint.forward),
                        Object.InputAuthority);

                    var rb = magicProjectile.GetComponent<Rigidbody>();

                    if (rb != null)
                    {
                        rb.velocity = wandTipPoint.forward * 10.0f;
                    }

                    Debug.Log("Frogman launched a spell!");
                }
            }
        }

        public void ApplyMagicEffect(float duration)
        {
            if (!HasInputAuthority) return;

            IsAfflicted = true;
            afflictionTimer = TickTimer.CreateFromSeconds(Runner, duration);

            Debug.Log($"Player is stunned for {duration} seconds!");
        }

        private void HandleAlienAbility()
        {
            if (activeBeacon != null)
            {
                if (CheckIfSecondaryButtonPressed())
                {
                    transform.position = new Vector3(activeBeacon.transform.position.x, transform.position.y, activeBeacon.transform.position.z);

                    Debug.Log("Recalled to beacon!");

                    runner.Despawn(activeBeacon);
                    activeBeacon = null;

                    justRecalled = true;
                }
            }
            else
            {
                if (CheckIfSecondaryButtonPressed())
                {
                    Debug.Log("No active beacon to recall!");

                    SendHapticImpulse(leftHandDevice, 0.3f, 0.15f);
                    SendHapticImpulse(rightHandDevice, 0.3f, 0.15f);
                }
            }

            if (lastBeaconDropTime < beaconCooldown)
            {
                lastBeaconDropTime += Time.deltaTime;
            }
            else
            {
                if (CheckIfGrounded() && CheckIfPrimaryButtonPressed())
                {
                    lastBeaconDropTime = 0.0f;
                    TryDropAlienBeacon();
                }
            }
        }

        private void TryDropAlienBeacon()
        {
            if (activeBeacon != null)
            {
                Runner.Despawn(activeBeacon);
                activeBeacon = null;
            }

            var spawnedObject = Runner.Spawn(
                beaconPrefab,
                new Vector3(transform.position.x, 0.5f, transform.position.z),
                Quaternion.identity,
                inputAuthority: Object.InputAuthority
                );

            activeBeacon = spawnedObject;

            AlienBeacon beaconObject = activeBeacon.GetComponent<AlienBeacon>();

            if (beaconObject != null)
            {
                beaconObject.owner = this;
            }
        }

        public void NotifyBeaconDestroyed()
        {
            activeBeacon = null;

            Debug.Log("Your beacon was destroyed!");

            SendHapticImpulse(leftHandDevice, 0.6f, 0.15f);
            SendHapticImpulse(rightHandDevice, 0.6f, 0.15f);
        }

        private bool CheckIfPrimaryButtonPressed()
        {
            bool primaryButtonPressed = false;

            if (rightHandDevice.device.IsPressed(InputHelpers.Button.PrimaryButton, out primaryButtonPressed, 0.1f))
            {
                return primaryButtonPressed;
            }

            return primaryButtonPressed;
        }

        private bool CheckIfSecondaryButtonPressed()
        {
            bool secondaryButtonPressed = false;

            if (rightHandDevice.device.IsPressed(InputHelpers.Button.SecondaryButton, out secondaryButtonPressed, 0.1f))
            {
                return secondaryButtonPressed;
            }

            return secondaryButtonPressed;
        }

        private bool CheckIfGrounded()
        {
            return Physics.Raycast(bodyCollider.gameObject.transform.position, -Vector3.up, 0.5f);
        }

        private bool CheckIfPlayerHoldingTriggers()
        {
            if (leftHandDevice.device.IsPressed(InputHelpers.Button.Trigger, out leftTriggerHeld, 0.1f) && rightHandDevice.device.IsPressed(InputHelpers.Button.Trigger, out rightTriggerHeld, 0.1f))
            {
                return leftTriggerHeld && rightTriggerHeld;
            }

            return false;
        }

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
            } else
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

        public void Turn(float degrees)
        {
            transform.RotateAround(headCollider.transform.position, transform.up, degrees);
            denormalizedVelocityAverage = Quaternion.Euler(0, degrees, 0) * denormalizedVelocityAverage;
            for (int i = 0; i < velocityHistory.Length; i++)
            {
                velocityHistory[i] = Quaternion.Euler(0, degrees, 0) * velocityHistory[i];
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

        private void SendHapticImpulse(XRControllerInputDevice controller, float amplitude, float duration)
        {
            if (controller != null && controller.device.isValid)
            {
                controller.device.SendHapticImpulse(0u, amplitude, duration);
            }
        }
    }
}
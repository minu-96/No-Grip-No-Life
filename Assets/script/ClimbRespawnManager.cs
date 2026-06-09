using UnityEngine;
using System.Collections;
using Unity.XR.CoreUtils;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;

public class ClimbRespawnManager : MonoBehaviour
{
    [Header("손 설정")]
    public ClimbingHand leftHand;
    public ClimbingHand rightHand;

    [Header("중력 및 바닥 체크 설정")]
    public bool useGravity = true;
    public float gravity = 9.81f;

    [Header("리스폰 설정")]
    [Tooltip("낙하 상태가 된 뒤 몇 초 후 페이드 아웃/리스폰을 시작할지 결정합니다.")]
    public float respawnDelay = 0.3f;

    [Tooltip("이 Y축 높이 이하로 추락하면 즉시 리스폰시킵니다.")]
    public float deathYThreshold = -30f;

    [Header("Checkpoint Detection")]
    [Tooltip("Detects Checkpoint tags in this radius even if trigger events are missed.")]
    public float checkpointDetectionRadius = 1.0f;

    [Tooltip("Detects RespawnPoint objects in this horizontal radius.")]
    public float respawnPointDetectionRadius = 5.0f;

    [Tooltip("How high above a checkpoint to start searching for a safe respawn floor.")]
    public float checkpointGroundSearchHeight = 3.0f;

    [Tooltip("How far below a checkpoint to search for a safe respawn floor.")]
    public float checkpointGroundSearchDepth = 8.0f;

    [Tooltip("Extra distance used to detect and snap onto ground below the player.")]
    public float groundSnapDistance = 0.5f;

    [Tooltip("Keep the CharacterController centered on the checkpoint instead of applying room-scale camera XZ offset.")]
    public bool centerControllerOnCheckpoint = true;

    [Tooltip("Allows a lower checkpoint to replace the current checkpoint.")]
    public bool allowLowerCheckpointOverride = false;

    [Tooltip("A lower checkpoint must be this much higher than the current one to be accepted when lower overrides are disabled.")]
    public float checkpointHeightTolerance = 0.5f;

    [Tooltip("Logs ignored lower checkpoints. Useful only while debugging checkpoint order.")]
    public bool logIgnoredLowerCheckpoints = false;

    [Tooltip("Seconds to ignore checkpoint changes and fall respawn checks after respawning.")]
    public float postRespawnGraceTime = 3.0f;

    [Tooltip("Seconds to keep gravity disabled after respawning at an explicit RespawnPoint.")]
    public float explicitRespawnGravityPause = 3.0f;

    [Tooltip("Keeps gravity disabled at explicit RespawnPoints until the player grabs again.")]
    public bool holdAtExplicitRespawnUntilGrab = true;

    [Tooltip("Keeps the CharacterController capsule under the XR camera.")]
    public bool alignCharacterControllerWithCamera = true;

    [Tooltip("Camera-based ground check distance used before applying custom gravity.")]
    public float cameraGroundedDistance = 2.0f;

    [Tooltip("Camera height above the respawn floor after resolving a safe spawn position.")]
    public float respawnCameraHeight = 1.6f;

    [Header("페이드 설정")]
    [Tooltip("XR 카메라 앞에 붙인 FadeQuad의 Mesh Renderer를 연결하세요.")]
    public Renderer fadeRenderer;
    public float fadeOutDuration = 0.25f;
    public float fadeInDuration = 0.35f;

    [Header("클라이밍 배율")]
    public float climbMultiplier = 1.0f;

    private XROrigin xrOrigin;
    private CharacterController characterController;
    private LocomotionMediator locomotionMediator;

    private ClimbingHand activeHand;
    private Vector3 fallVelocity;
    private Vector3 lastCheckpointPos;
    private Transform lastCheckpointRoot;
    private string lastCheckpointSpawnSource = "";
    private bool lastSpawnPositionIsExplicit;
    private bool isRespawning = false;
    private bool isClimbing = false;
    private Coroutine respawnCoroutine;
    private Coroutine safeRespawnCoroutine;
    private Material fadeMaterial;
    private readonly Collider[] checkpointHits = new Collider[16];
    private Collider[] checkpointColliders;
    private Transform[] respawnPoints;
    private Vector3 lastCheckpointProbePosition;
    private bool hasCheckpointProbePosition;
    private float ignoreCheckpointUntilTime;
    private float ignoreRespawnUntilTime;
    private float pauseGravityUntilTime;
    private bool holdingAtExplicitRespawn;

    void Start()
    {
        xrOrigin = GetComponentInParent<XROrigin>();
        if (xrOrigin == null) xrOrigin = FindAnyObjectByType<XROrigin>();

        if (xrOrigin != null)
        {
            characterController = xrOrigin.GetComponent<CharacterController>();
            locomotionMediator = FindAnyObjectByType<LocomotionMediator>();
        }

        if (xrOrigin == null || characterController == null || locomotionMediator == null)
        {
            Debug.LogError("[ClimbRespawnManager] 필수 컴포넌트를 찾을 수 없습니다!");
            enabled = false;
            return;
        }

        if (fadeRenderer != null)
        {
            fadeMaterial = fadeRenderer.material;
            SetFadeAlpha(0f);
        }
        else
        {
            Debug.LogWarning("[ClimbRespawnManager] Fade Renderer가 연결되지 않았습니다. 페이드 없이 리스폰됩니다.");
        }

        lastCheckpointPos = xrOrigin.transform.position;
        RefreshCheckpointColliders();
        RefreshRespawnPoints();
    }

    void Update()
    {
        if (holdingAtExplicitRespawn)
        {
            if (GetCurrentHand() == null)
            {
                AlignCharacterControllerToCamera();
                fallVelocity = Vector3.zero;

                if (characterController != null &&
                    characterController.enabled &&
                    (TrySnapToGround() || IsCameraNearGround()))
                {
                    holdingAtExplicitRespawn = false;
                    pauseGravityUntilTime = 0f;
                }
                else
                {
                    HoldPlayerAtExplicitRespawnHeight();
                }
            }
            else
            {
                holdingAtExplicitRespawn = false;
                pauseGravityUntilTime = 0f;
            }
        }

        if (!holdingAtExplicitRespawn && Time.time >= ignoreCheckpointUntilTime)
        {
            DetectCheckpointNearby();
        }

        ClimbingHand newHand = GetCurrentHand();

        if (newHand != null)
        {
            if (!isClimbing || activeHand != newHand)
            {
                activeHand = newHand;
                isClimbing = true;
                fallVelocity = Vector3.zero;
                StopRespawn();
            }

            PerformClimb();
        }
        else
        {
            if (isClimbing)
            {
                isClimbing = false;
                activeHand = null;
                fallVelocity = Vector3.zero;
            }

            if (ClimbingManager.Instance != null && ClimbingManager.Instance.IsClimbingOrHandingOff())
            {
                fallVelocity = Vector3.zero;
                StopRespawn();
                return;
            }

            if (useGravity)
            {
                ApplyGravity();
            }
        }

        bool isClimbingOrHandingOff = ClimbingManager.Instance != null && ClimbingManager.Instance.IsClimbingOrHandingOff();
        float playerHeight = xrOrigin.Camera != null ? xrOrigin.Camera.transform.position.y : xrOrigin.transform.position.y;
        if (!isClimbingOrHandingOff &&
            !isRespawning &&
            Time.time >= ignoreRespawnUntilTime &&
            playerHeight < deathYThreshold)
        {
            ResetToSafety();
        }
    }

    ClimbingHand GetCurrentHand()
    {
        if (ClimbingManager.Instance != null &&
            ClimbingManager.Instance.activeHand != null &&
            ClimbingManager.Instance.activeHand.isGrabbing)
        {
            return ClimbingManager.Instance.activeHand;
        }

        if (activeHand != null && activeHand.isGrabbing) return activeHand;
        if (rightHand != null && rightHand.isGrabbing) return rightHand;
        if (leftHand != null && leftHand.isGrabbing) return leftHand;
        return null;
    }

    void PerformClimb()
    {
        // 실제 클라이밍 이동은 ClimbingHand에서 xrOrigin을 이동시키고 있으므로 비워둡니다.
    }

    void ApplyGravity()
    {
        if (characterController == null) return;
        if (safeRespawnCoroutine != null) return;
        if (holdingAtExplicitRespawn)
        {
            if (GetCurrentHand() == null)
            {
                fallVelocity = Vector3.zero;
                StopRespawn();
                return;
            }

            holdingAtExplicitRespawn = false;
        }

        if (Time.time < ignoreRespawnUntilTime) return;
        if (Time.time < pauseGravityUntilTime) return;

        AlignCharacterControllerToCamera();

        if (IsCameraNearGround())
        {
            fallVelocity = Vector3.zero;
            StopRespawn();
            return;
        }

        if (!characterController.enabled)
        {
            characterController.enabled = true;
            Physics.SyncTransforms();
        }

        if (TrySnapToGround())
        {
            fallVelocity = Vector3.zero;
            StopRespawn();
            return;
        }

        CollisionFlags groundProbe = characterController.Move(Vector3.down * 0.02f);

        if ((groundProbe & CollisionFlags.Below) != 0)
        {
            fallVelocity = Vector3.zero;
            StopRespawn();
            return;
        }

        fallVelocity.y -= gravity * Time.deltaTime;

        CollisionFlags fallCollision = characterController.Move(fallVelocity * Time.deltaTime);

        if ((fallCollision & CollisionFlags.Below) != 0)
        {
            fallVelocity = Vector3.zero;
            StopRespawn();
            TrySnapToGround();
            return;
        }

        if (!isRespawning && respawnCoroutine == null)
        {
            respawnCoroutine = StartCoroutine(RespawnAfterDelay());
        }
    }

    IEnumerator RespawnAfterDelay()
    {
        isRespawning = true;

        yield return new WaitForSeconds(respawnDelay);

        respawnCoroutine = null;

        bool isClimbingOrHandingOff = ClimbingManager.Instance != null && ClimbingManager.Instance.IsClimbingOrHandingOff();
        if (!isClimbingOrHandingOff && !isClimbing && GetCurrentHand() == null)
        {
            StartSafeRespawn();
        }
        else
        {
            isRespawning = false;
        }
    }

    public void ResetToSafety()
    {
        fallVelocity = Vector3.zero;
        StartSafeRespawn();
    }

    private void StartSafeRespawn()
    {
        if (safeRespawnCoroutine != null) return;

        if (gameObject.activeInHierarchy)
        {
            ReleaseHandsForRespawn();

            if (lastCheckpointRoot == null)
            {
                SelectBestRespawnPointForCurrentPosition();
            }

            isRespawning = true;
            safeRespawnCoroutine = StartCoroutine(SafeRespawnRoutine());
        }
    }

    private void ReleaseHandsForRespawn()
    {
        if (leftHand != null && leftHand.isGrabbing) leftHand.Release();
        if (rightHand != null && rightHand.isGrabbing) rightHand.Release();

        if (ClimbingManager.Instance != null)
        {
            ClimbingManager.Instance.SetActiveHand(null);
        }

        activeHand = null;
        isClimbing = false;
    }

    private IEnumerator SafeRespawnRoutine()
    {
        fallVelocity = Vector3.zero;

        if (fadeRenderer != null)
        {
            yield return Fade(0f, 1f, fadeOutDuration);
            yield return new WaitForSecondsRealtime(0.2f);
        }

        Vector3 rawSpawnPos = lastCheckpointPos;
        bool explicitSpawnPoint = lastSpawnPositionIsExplicit || IsRespawnPointName(lastCheckpointSpawnSource);
        Vector3 targetSpawnPos = centerControllerOnCheckpoint
            ? rawSpawnPos
            : rawSpawnPos - GetCameraPlanarOffset();

        targetSpawnPos = ResolveSafeSpawnPosition(targetSpawnPos);
        bool shouldHoldAtExplicitRespawn = false;

        if (characterController != null)
        {
            characterController.enabled = false;
        }

        MovePlayerToWorldLocation(targetSpawnPos);
        AlignCharacterControllerToCamera();

        Physics.SyncTransforms();
        hasCheckpointProbePosition = false;
        ignoreCheckpointUntilTime = Time.time + postRespawnGraceTime;
        ignoreRespawnUntilTime = Time.time + postRespawnGraceTime;
        if (explicitSpawnPoint)
        {
            pauseGravityUntilTime = Time.time + explicitRespawnGravityPause;
            holdingAtExplicitRespawn = shouldHoldAtExplicitRespawn;
        }

        yield return new WaitForEndOfFrame();

        MovePlayerToWorldLocation(targetSpawnPos);
        AlignCharacterControllerToCamera();
        Physics.SyncTransforms();

        if (characterController != null)
        {
            characterController.enabled = true;
            if (!shouldHoldAtExplicitRespawn)
            {
                TrySnapToGround();
            }
        }

        StaminaManager stamina = FindObjectOfType<StaminaManager>();
        if (stamina != null)
        {
            stamina.currentStamina = stamina.maxStamina;
        }

        fallVelocity = Vector3.zero;

        if (fadeRenderer != null)
        {
            yield return new WaitForSecondsRealtime(0.2f);
            yield return Fade(1f, 0f, fadeInDuration);
        }

        MovePlayerToWorldLocation(targetSpawnPos);
        AlignCharacterControllerToCamera();
        Physics.SyncTransforms();
        if (characterController != null && characterController.enabled && !shouldHoldAtExplicitRespawn)
        {
            TrySnapToGround();
        }

        isRespawning = false;
        safeRespawnCoroutine = null;

        Debug.LogWarning($"🏁 [완벽 부활] VR 페이드 후 체크포인트로 안전하게 복귀했습니다. target={targetSpawnPos}, actualXROrigin={xrOrigin.transform.position}, actualOriginBase={(xrOrigin.Origin != null ? xrOrigin.Origin.transform.position : Vector3.zero)}, actualCamera={(xrOrigin.Camera != null ? xrOrigin.Camera.transform.position : Vector3.zero)}, checkpoint={lastCheckpointRoot?.name}, source={lastCheckpointSpawnSource}");
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        if (fadeRenderer == null)
        {
            Debug.LogError("[ClimbRespawnManager] Fade Renderer가 연결되지 않았습니다!");
            yield break;
        }

        if (fadeMaterial == null)
        {
            fadeMaterial = fadeRenderer.material;
        }

        float time = 0f;
        SetFadeAlpha(from);

        while (time < duration)
        {
            time += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(time / duration);
            SetFadeAlpha(Mathf.Lerp(from, to, t));
            yield return null;
        }

        SetFadeAlpha(to);
    }

    private void SetFadeAlpha(float alpha)
    {
        if (fadeMaterial == null) return;

        Color color = fadeMaterial.color;
        color.a = alpha;
        fadeMaterial.color = color;

        fadeRenderer.enabled = alpha > 0.001f;
    }

    void StopRespawn()
    {
        if (respawnCoroutine != null)
        {
            StopCoroutine(respawnCoroutine);
            respawnCoroutine = null;
        }

        if (safeRespawnCoroutine == null)
        {
            isRespawning = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (Time.time < ignoreCheckpointUntilTime) return;

        if (other.CompareTag("Checkpoint"))
        {
            if (SetCheckpoint(other))
            {
                Debug.Log($"📍 새로운 체크포인트 등록: {other.gameObject.name}");
            }
        }
    }

    private void DetectCheckpointNearby()
    {
        Vector3 probePosition = characterController != null
            ? characterController.bounds.center
            : xrOrigin.transform.position;

        DetectRespawnPointNearby(probePosition);
        DetectCheckpointByFootPosition(probePosition);

        if (!hasCheckpointProbePosition)
        {
            lastCheckpointProbePosition = probePosition;
            hasCheckpointProbePosition = true;
        }

        int hitCount = Physics.OverlapCapsuleNonAlloc(
            lastCheckpointProbePosition,
            probePosition,
            Mathf.Max(checkpointDetectionRadius, 0.1f),
            checkpointHits,
            ~0,
            QueryTriggerInteraction.Collide);

        lastCheckpointProbePosition = probePosition;

        Collider nearestCheckpoint = null;
        float nearestDistance = float.MaxValue;

        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = checkpointHits[i];
            checkpointHits[i] = null;

            if (hit == null || !hit.CompareTag("Checkpoint")) continue;

            Vector3 closestPoint = hit.ClosestPoint(probePosition);
            float distance = (closestPoint - probePosition).sqrMagnitude;
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestCheckpoint = hit;
            }
        }

        if (nearestCheckpoint != null)
        {
            SetCheckpoint(nearestCheckpoint);
        }
    }

    private void DetectCheckpointByFootPosition(Vector3 probePosition)
    {
        if (checkpointColliders == null || checkpointColliders.Length == 0)
        {
            RefreshCheckpointColliders();
        }

        if (checkpointColliders == null) return;

        Vector3 footPosition;
        if (characterController != null)
        {
            Bounds controllerBounds = characterController.bounds;
            footPosition = new Vector3(controllerBounds.center.x, controllerBounds.min.y, controllerBounds.center.z);
        }
        else
        {
            footPosition = xrOrigin.transform.position;
        }

        Collider bestCheckpoint = null;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < checkpointColliders.Length; i++)
        {
            Collider checkpoint = checkpointColliders[i];
            if (checkpoint == null || !checkpoint.CompareTag("Checkpoint")) continue;

            Bounds bounds = checkpoint.bounds;
            float horizontalPadding = Mathf.Max(checkpointDetectionRadius, characterController != null ? characterController.radius : 0.5f);
            float minX = bounds.min.x - horizontalPadding;
            float maxX = bounds.max.x + horizontalPadding;
            float minZ = bounds.min.z - horizontalPadding;
            float maxZ = bounds.max.z + horizontalPadding;
            float minY = bounds.min.y - groundSnapDistance;
            float maxY = bounds.max.y + Mathf.Max(checkpointDetectionRadius, 1.5f);

            if (footPosition.x < minX || footPosition.x > maxX) continue;
            if (footPosition.z < minZ || footPosition.z > maxZ) continue;
            if (footPosition.y < minY || footPosition.y > maxY) continue;

            Vector3 closestPoint = bounds.ClosestPoint(footPosition);
            float distance = (closestPoint - footPosition).sqrMagnitude;
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestCheckpoint = checkpoint;
            }
        }

        if (bestCheckpoint != null)
        {
            SetCheckpoint(bestCheckpoint);
        }
    }

    private void RefreshCheckpointColliders()
    {
        GameObject[] checkpointObjects = GameObject.FindGameObjectsWithTag("Checkpoint");
        checkpointColliders = new Collider[checkpointObjects.Length];

        for (int i = 0; i < checkpointObjects.Length; i++)
        {
            checkpointColliders[i] = checkpointObjects[i].GetComponent<Collider>();
        }
    }

    private void DetectRespawnPointNearby(Vector3 probePosition)
    {
        if (respawnPoints == null || respawnPoints.Length == 0)
        {
            RefreshRespawnPoints();
        }

        if (respawnPoints == null || respawnPoints.Length == 0) return;

        Transform bestRespawnPoint = null;
        float bestDistance = float.MaxValue;
        float maxDistance = Mathf.Max(respawnPointDetectionRadius, 1.5f);
        float maxDistanceSqr = maxDistance * maxDistance;

        for (int i = 0; i < respawnPoints.Length; i++)
        {
            Transform respawnPoint = respawnPoints[i];
            if (respawnPoint == null) continue;

            Vector3 delta = respawnPoint.position - probePosition;
            delta.y = 0f;
            float distance = delta.sqrMagnitude;
            if (distance > maxDistanceSqr || distance >= bestDistance) continue;

            bestDistance = distance;
            bestRespawnPoint = respawnPoint;
        }

        if (bestRespawnPoint != null)
        {
            SetCheckpoint(bestRespawnPoint);
        }
    }

    private void SelectBestRespawnPointForCurrentPosition()
    {
        if (lastCheckpointRoot != null) return;

        Transform bestRespawnPoint = FindNearestRespawnPointByHorizontalDistance();
        if (bestRespawnPoint != null)
        {
            SetCheckpoint(bestRespawnPoint, true);
        }
    }

    private Vector3 GetBestRespawnPositionForCurrentLocation(out bool explicitSpawnPoint)
    {
        Transform bestRespawnPoint = FindNearestRespawnPointByHorizontalDistance();
        if (bestRespawnPoint == null)
        {
            explicitSpawnPoint = lastSpawnPositionIsExplicit;
            return lastCheckpointPos;
        }

        SetCheckpoint(bestRespawnPoint, true);
        explicitSpawnPoint = true;
        return bestRespawnPoint.position;
    }

    private Transform FindNearestRespawnPointByHorizontalDistance()
    {
        if (respawnPoints == null || respawnPoints.Length == 0)
        {
            RefreshRespawnPoints();
        }

        if (respawnPoints == null || respawnPoints.Length == 0) return null;

        Vector3 playerPosition = xrOrigin.Camera != null
            ? xrOrigin.Camera.transform.position
            : xrOrigin.transform.position;

        Transform bestRespawnPoint = null;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < respawnPoints.Length; i++)
        {
            Transform respawnPoint = respawnPoints[i];
            if (respawnPoint == null) continue;

            Vector3 delta = respawnPoint.position - playerPosition;
            delta.y = 0f;
            float distance = delta.sqrMagnitude;
            if (distance >= bestDistance) continue;

            bestDistance = distance;
            bestRespawnPoint = respawnPoint;
        }

        return bestRespawnPoint;
    }

    private void RefreshRespawnPoints()
    {
        Transform[] sceneTransforms = FindObjectsOfType<Transform>();
        System.Collections.Generic.List<Transform> foundRespawnPoints = new System.Collections.Generic.List<Transform>();

        for (int i = 0; i < sceneTransforms.Length; i++)
        {
            Transform sceneTransform = sceneTransforms[i];
            if (IsRespawnPointName(sceneTransform.name))
            {
                foundRespawnPoints.Add(sceneTransform);
            }
        }

        respawnPoints = foundRespawnPoints.ToArray();

        Debug.Log($"[ClimbRespawnManager] Found {respawnPoints.Length} RespawnPoint object(s): {GetRespawnPointSummary()}");
    }

    private string GetRespawnPointSummary()
    {
        if (respawnPoints == null || respawnPoints.Length == 0) return "none";

        System.Text.StringBuilder summary = new System.Text.StringBuilder();
        for (int i = 0; i < respawnPoints.Length; i++)
        {
            Transform respawnPoint = respawnPoints[i];
            if (respawnPoint == null) continue;

            if (summary.Length > 0) summary.Append(", ");
            summary.Append(respawnPoint.name);
            summary.Append("=");
            summary.Append(respawnPoint.position);
        }

        return summary.ToString();
    }

    private bool SetCheckpoint(Collider checkpoint)
    {
        Transform checkpointRoot = checkpoint.transform.parent != null
            ? checkpoint.transform.parent
            : checkpoint.transform;

        Vector3 checkpointPos = GetCheckpointSpawnCenter(checkpointRoot, checkpoint, out string spawnSource);
        if (!CanAcceptCheckpoint(checkpointPos, false))
        {
            if (logIgnoredLowerCheckpoints)
            {
                Debug.Log($"[ClimbRespawnManager] Ignored lower checkpoint: {checkpointRoot.name} at {checkpointPos}");
            }

            return false;
        }

        bool checkpointChanged = lastCheckpointRoot != checkpointRoot;
        bool spawnChanged = (lastCheckpointPos - checkpointPos).sqrMagnitude > 0.0001f || lastCheckpointSpawnSource != spawnSource;

        lastCheckpointRoot = checkpointRoot;
        lastCheckpointPos = checkpointPos;
        lastCheckpointSpawnSource = spawnSource;
        lastSpawnPositionIsExplicit = IsRespawnPointName(spawnSource);

        if (checkpointChanged || spawnChanged)
        {
            Debug.Log($"[ClimbRespawnManager] Checkpoint saved: {checkpointRoot.name} at {lastCheckpointPos} ({spawnSource})");
        }

        return true;
    }

    private bool SetCheckpoint(Transform respawnPoint, bool force = false)
    {
        Transform checkpointRoot = respawnPoint.parent != null
            ? respawnPoint.parent
            : respawnPoint;

        Vector3 checkpointPos = respawnPoint.position;
        if (!CanAcceptCheckpoint(checkpointPos, force))
        {
            if (logIgnoredLowerCheckpoints)
            {
                Debug.Log($"[ClimbRespawnManager] Ignored lower respawn point: {respawnPoint.name} at {checkpointPos}");
            }

            return false;
        }

        bool checkpointChanged = lastCheckpointRoot != checkpointRoot;
        bool spawnChanged = (lastCheckpointPos - checkpointPos).sqrMagnitude > 0.0001f || lastCheckpointSpawnSource != respawnPoint.name;

        lastCheckpointRoot = checkpointRoot;
        lastCheckpointPos = checkpointPos;
        lastCheckpointSpawnSource = respawnPoint.name;
        lastSpawnPositionIsExplicit = true;

        if (checkpointChanged || spawnChanged)
        {
            string forcedText = force ? ", forced" : "";
            Debug.Log($"[ClimbRespawnManager] Checkpoint saved: {checkpointRoot.name} at {lastCheckpointPos} ({respawnPoint.name}{forcedText})");
        }

        return true;
    }

    private bool CanAcceptCheckpoint(Vector3 checkpointPos, bool force)
    {
        if (force || allowLowerCheckpointOverride) return true;
        if (checkpointPos.y >= lastCheckpointPos.y - checkpointHeightTolerance) return true;

        return IsPlayerStablyGrounded();
    }

    private bool IsPlayerStablyGrounded()
    {
        if (isRespawning || safeRespawnCoroutine != null) return false;
        if (characterController == null || !characterController.enabled) return false;
        if (characterController.isGrounded) return true;

        return TryFindGroundBelow(out _);
    }

    private Vector3 GetCheckpointSpawnCenter(Transform checkpointRoot, Collider checkpointTrigger, out string spawnSource)
    {
        Transform explicitSpawnPoint = FindCheckpointSpawnPoint(checkpointRoot);
        if (explicitSpawnPoint != null)
        {
            spawnSource = explicitSpawnPoint.name;
            return explicitSpawnPoint.position;
        }

        Renderer rootRenderer = checkpointRoot.GetComponent<Renderer>();
        if (rootRenderer != null)
        {
            spawnSource = "Renderer.bounds.center";
            return rootRenderer.bounds.center;
        }

        MeshFilter rootMesh = checkpointRoot.GetComponent<MeshFilter>();
        if (rootMesh != null && rootMesh.sharedMesh != null)
        {
            spawnSource = "Mesh.bounds.center";
            return checkpointRoot.TransformPoint(rootMesh.sharedMesh.bounds.center);
        }

        Collider rootCollider = checkpointRoot.GetComponent<Collider>();
        if (rootCollider != null)
        {
            spawnSource = "Collider.bounds.center";
            return rootCollider.bounds.center;
        }

        spawnSource = "Trigger.bounds.center";
        return checkpointTrigger.bounds.center;
    }

    private Transform FindCheckpointSpawnPoint(Transform checkpointRoot)
    {
        return FindCheckpointSpawnPointRecursive(checkpointRoot);
    }

    private Transform FindCheckpointSpawnPointRecursive(Transform root)
    {
        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);

            if (IsRespawnPointName(child.name))
            {
                return child;
            }

            Transform nestedSpawnPoint = FindCheckpointSpawnPointRecursive(child);
            if (nestedSpawnPoint != null)
            {
                return nestedSpawnPoint;
            }
        }

        return null;
    }

    private bool IsRespawnPointName(string objectName)
    {
        string normalizedName = objectName.Replace(" ", "").Replace("_", "").Replace("-", "").ToLowerInvariant();

        return normalizedName.StartsWith("respawnpoint") ||
            normalizedName.StartsWith("spawnpoint") ||
            normalizedName.StartsWith("respawnposition") ||
            normalizedName.StartsWith("spawnposition");
    }

    private Vector3 ResolveSafeSpawnPosition(Vector3 spawnPosition)
    {
        if (characterController == null)
        {
            spawnPosition.y += 0.5f;
            return spawnPosition;
        }

        Vector3 rayOrigin = new Vector3(
            spawnPosition.x,
            lastCheckpointPos.y + checkpointGroundSearchHeight,
            spawnPosition.z);

        float rayDistance = checkpointGroundSearchHeight + checkpointGroundSearchDepth;
        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, rayDistance, ~0, QueryTriggerInteraction.Ignore))
        {
            spawnPosition.y = hit.point.y + GetResolvedRespawnCameraHeight();
        }
        else
        {
            spawnPosition.y += GetResolvedRespawnCameraHeight();
            Debug.LogWarning("[ClimbRespawnManager] 체크포인트 아래 바닥을 찾지 못해 기본 높이로 리스폰합니다.");
        }

        return spawnPosition;
    }

    private Vector3 GetCameraPlanarOffset()
    {
        if (xrOrigin.Camera == null) return Vector3.zero;

        Vector3 cameraOffset = xrOrigin.Camera.transform.position - xrOrigin.transform.position;
        cameraOffset.y = 0f;
        return cameraOffset;
    }

    private float GetResolvedRespawnCameraHeight()
    {
        return Mathf.Max(respawnCameraHeight, characterController != null ? characterController.radius * 2f : 1.0f);
    }

    private void HoldPlayerAtExplicitRespawnHeight()
    {
        if (xrOrigin == null) return;

        Vector3 currentPosition = xrOrigin.Camera != null
            ? xrOrigin.Camera.transform.position
            : xrOrigin.transform.position;

        Vector3 heldPosition = new Vector3(currentPosition.x, lastCheckpointPos.y, currentPosition.z);
        MovePlayerToWorldLocation(heldPosition);
        AlignCharacterControllerToCamera();
        Physics.SyncTransforms();
    }

    private void MovePlayerToWorldLocation(Vector3 targetWorldPosition)
    {
        if (xrOrigin.Camera != null)
        {
            xrOrigin.MoveCameraToWorldLocation(targetWorldPosition);
            Vector3 cameraDelta = targetWorldPosition - xrOrigin.Camera.transform.position;
            if (cameraDelta.sqrMagnitude > 0.0001f)
            {
                Transform originTransform = xrOrigin.Origin != null ? xrOrigin.Origin.transform : xrOrigin.transform;
                originTransform.position += cameraDelta;
            }
        }
        else
        {
            xrOrigin.transform.position = targetWorldPosition;
        }
    }

    private void AlignCharacterControllerToCamera()
    {
        if (!alignCharacterControllerWithCamera) return;
        if (characterController == null || xrOrigin.Camera == null) return;

        Vector3 cameraLocalPosition = xrOrigin.transform.InverseTransformPoint(xrOrigin.Camera.transform.position);
        float height = Mathf.Max(characterController.height, characterController.radius * 2f);

        Vector3 center = characterController.center;
        center.x = cameraLocalPosition.x;
        center.y = cameraLocalPosition.y - (height * 0.5f);
        center.z = cameraLocalPosition.z;
        characterController.center = center;
    }

    private bool TrySnapToGround()
    {
        if (characterController == null || !characterController.enabled) return false;
        if (!TryFindGroundBelow(out RaycastHit hit)) return false;

        float halfHeight = Mathf.Max(characterController.height * 0.5f, characterController.radius);
        float desiredOriginY = hit.point.y + halfHeight - characterController.center.y + characterController.skinWidth + 0.01f;
        float deltaY = desiredOriginY - xrOrigin.transform.position.y;

        if (deltaY > 0.2f || deltaY < -groundSnapDistance) return false;

        characterController.enabled = false;
        Vector3 position = xrOrigin.transform.position;
        position.y = desiredOriginY;
        xrOrigin.transform.position = position;
        Physics.SyncTransforms();
        characterController.enabled = true;

        return true;
    }

    private bool IsCameraNearGround()
    {
        if (xrOrigin.Camera == null) return false;

        Vector3 rayOrigin = xrOrigin.Camera.transform.position + Vector3.up * 0.05f;
        if (!Physics.Raycast(
            rayOrigin,
            Vector3.down,
            out RaycastHit hit,
            cameraGroundedDistance,
            ~0,
            QueryTriggerInteraction.Ignore))
        {
            return false;
        }

        return Vector3.Angle(hit.normal, Vector3.up) <= characterController.slopeLimit;
    }

    private bool TryFindGroundBelow(out RaycastHit hit)
    {
        Bounds bounds = characterController.bounds;
        Vector3 origin = bounds.center + Vector3.up * 0.05f;
        float halfHeight = Mathf.Max(characterController.height * 0.5f, characterController.radius);
        float distance = halfHeight + groundSnapDistance + 0.1f;

        if (!Physics.SphereCast(
            origin,
            characterController.radius * 0.8f,
            Vector3.down,
            out hit,
            distance,
            ~0,
            QueryTriggerInteraction.Ignore))
        {
            return false;
        }

        return Vector3.Angle(hit.normal, Vector3.up) <= characterController.slopeLimit;
    }
}

using UnityEngine;
using UnityEngine.Animations.Rigging;
using Steamworks;
using Steamworks.Data;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

// --- [데이터 클래스] ---
[System.Serializable]
public class Weapon
{
    public string Name;
    public int Damage;
    public int MaxAmmo;
    public int CurrentAmmo;
    public bool IsAutoWeapon;

    public Weapon(string name, int damage, int maxAmmo, bool isAuto = false)
    {
        Name = name;
        Damage = damage;
        MaxAmmo = maxAmmo;
        CurrentAmmo = maxAmmo;
        IsAutoWeapon = isAuto;
    }

    public string GetAmmoText() => $"{CurrentAmmo}/{MaxAmmo}";
    public int GetAmmoPerShot() => IsAutoWeapon ? 2 : 1;
    public bool CanFire() => CurrentAmmo >= GetAmmoPerShot();

    public void Fire()
    {
        CurrentAmmo -= GetAmmoPerShot();
    }

    public void Reload()
    {
        CurrentAmmo = MaxAmmo;
    }

    public string Serialize()
    {
        return $"{CurrentAmmo}";
    }

    public void Deserialize(string data)
    {
        if (string.IsNullOrEmpty(data)) return;
        if (int.TryParse(data, out int ammo))
        {
            CurrentAmmo = ammo;
        }
    }
}

[System.Serializable]
public class PlayerSeat
{
    public string seatName;
    public Transform seatRoot;
    public Transform weaponAnchor;
    public Transform hpUiAnchor;
    public GameObject seatClickCollider;
    
    // Animation Rigging
    public Transform rightHandTarget;
    public Transform leftHandTarget;
    public Transform headTarget;
    public MultiAimConstraint headAimConstraint;
    public TwoBoneIKConstraint rightArmIK;
    public TwoBoneIKConstraint leftArmIK;
    
    // 캐릭터 참조 (런타임에 찾음)
    [HideInInspector] public GameObject character;
    [HideInInspector] public CharacterCustomizer customizer;
}

// --- [메인 게임 매니저] ---
public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public enum GamePhase { WaitingForStart, ActionSelecting, ResultsShowing, Tiebreaker }
    public GamePhase currentPhase = GamePhase.WaitingForStart;

    [Header("Cameras & Rig")]
    public GameObject uiCamera;
    public Camera gameCamera;
    public Transform cameraPivot;

    [Header("Camera Offset Settings")]
    public Vector3 camOffset = new Vector3(0, 3.2f, -8.9f);
    public Vector3 camRotation = new Vector3(10, 0, 0);

    [Header("First Person Look")]
    public Transform headBone;              // 머리 본 연결
    public float lookSensitivity = 2f;
    public float maxLookY = 60f;            // 상하 제한
    public float maxLookX = 80f;            // 좌우 제한
    private bool isLookMode = false;
    private float lookRotX = 0f;
    private float lookRotY = 0f;
    private Quaternion headBoneInitialRotation;

    [Header("Animation Rigging")]
    // 각 좌석별 타겟은 PlayerSeat에서 관리
    
    [Header("Hit Effect")]
    public UnityEngine.UI.Image hitFlashImage;  // 빨간 화면 효과용 UI Image
    public UnityEngine.UI.Image blockOverlayImage;  // 봉인 당했을 때 어두운 오버레이
    
    private string lastProcessedPhase = "";
    
    [Header("Test Mode")]
    public bool enableTestMode = true;  // 테스트 모드 활성화
    private bool isTestMode = false;  // 실제 테스트 모드 실행 중
    private List<ulong> fakePlayerIds = new List<ulong>();  // 가짜 플레이어 ID
    
    // 오른손 포즈 데이터
    private readonly Vector3 handPoseTable = new Vector3(0.139f, 0.6987f, 0.571f);
    private readonly Vector3 handRotTable = new Vector3(0f, -90f, 20f);
    private readonly Vector3 handPoseAimFront = new Vector3(0.139f, 1.0771f, 0.571f);
    private readonly Vector3 handRotAim = new Vector3(-90f, -90f, 20f);
    private readonly Vector3 handPoseAimLeft = new Vector3(-0.343f, 1.0771f, 0.571f);
    private readonly Vector3 handRotAimLeft = new Vector3(-90f, -140f, 20f);
    private readonly Vector3 handPoseAimRight = new Vector3(0.5f, 1.1f, 0.571f);
    private readonly Vector3 handRotAimRight = new Vector3(-90f, -90f, 65f);
    
    // 왼손 포즈 데이터 (테이블 위 기본 위치)
    private Vector3 leftHandPoseTable = new Vector3(-0.139f, 0.6987f, 0.571f);
    private readonly Vector3 leftHandRotTable = new Vector3(180f, 90f, 20f);

    [Header("UI Panels & HUD")]
    public GameObject actionSubmitButton;
    public GameObject WaitingUI;
    public Canvas hudCanvas;
    public TextMeshProUGUI myHpText2D;
    public TextMeshProUGUI ammoTextUI;
    public GameObject cancelXButton;

    [Header("Action Buttons")]
    public GameObject actionButtonPanel;
    public Button shootButton;
    public Button reloadButton;
    public Button skillButton;
    public TextMeshProUGUI skillButtonText;
    public Vector3 buttonOffset = new Vector3(0, 0.5f, 0);

    [Header("Job UI")]
    public GameObject jobCardUI;
    public TextMeshProUGUI jobNameText;
    public GameObject jobDetailPanel;

    [Header("Spatial Prefabs")]
    public GameObject weaponAutoPrefab;
    public GameObject weaponRevolverPrefab;
    public GameObject weaponBreakPrefab;
    public GameObject hpUiPrefab;

    [Header("Seating Settings")]
    public PlayerSeat[] playerSeats;

    [Header("Stats")]
    public int maxHP = 10;
    public int autoDamage = 2;
    public int autoMaxAmmo = 8;
    public int revolverDamage = 3;
    public int revolverMaxAmmo = 6;
    public int breakDamage = 5;
    public int breakMaxAmmo = 1;

    [Header("Data")]
    public Weapon MyWeapon;
    public Job MyJob;

    private int mySeatIndex = -1;
    private bool isConfirmed = false;
    private bool gameInitialized = false;
    private bool isCameraInitialized = false;

    private int tempTargetIndex = -1;
    private int tempSecondTargetIndex = -1;
    private bool isReloadingReserved = false;
    private bool isGunSelected = false;

    private bool isUsingActiveSkill = false;
    private int activeSkillTargetIndex = -1;

    private bool isProcessingResults = false;
    private string lastProcessedState = "";
    private bool isDead = false;

    private Dictionary<int, TextMeshProUGUI> hpTextCache = new Dictionary<int, TextMeshProUGUI>();
    private Coroutine turnCheckCoroutine = null;

    [Header("Death Effect")]
    public GameObject deathOverlayPanel;

    [Header("Tiebreaker")]
    public Transform tiebreakerRevolverAnchor;
    public GameObject tiebreakerUI;
    private GameObject tiebreakerRevolverInstance;
    private List<ulong> tiebreakerParticipants = new List<ulong>();
    private List<bool> tiebreakerChamber = new List<bool>();
    private bool isTiebreakerActive = false;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (uiCamera != null) uiCamera.SetActive(false);
        if (gameCamera != null) gameCamera.gameObject.SetActive(true);
        if (hudCanvas != null) hudCanvas.gameObject.SetActive(true);

        if (actionSubmitButton != null) actionSubmitButton.SetActive(false);
        if (WaitingUI != null) WaitingUI.SetActive(true);
        if (cancelXButton != null) cancelXButton.SetActive(false);
        if (actionButtonPanel != null) actionButtonPanel.SetActive(false);
        if (jobDetailPanel != null) jobDetailPanel.SetActive(false);

        // 마우스 기본 상태
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (SteamClient.IsValid && SteamLobby.Instance?.CurrentLobby != null)
        {
            var lobby = SteamLobby.Instance.CurrentLobby.Value;
            SteamMatchmaking.OnLobbyDataChanged += OnLobbyDataUpdate;
            SteamMatchmaking.OnLobbyMemberDataChanged += OnMemberDataUpdate;

            lobby.SetMemberData("HP", maxHP.ToString());
            lobby.SetMemberData("Turn_Ready", "false");

            MyJob = SteamLobby.Instance.MyJob;
            Debug.Log($"<color=green>내 직업: {MyJob?.Name ?? "없음"}</color>");

            AssignMyWeapon(lobby);
            InitializeJobUI();

            string gameState = lobby.GetData("GameState");
            Debug.Log($"[GameManager] 시작 - GameState: {gameState}");

            if (gameState == "DistributingItems" && lobby.Owner.Id == SteamClient.SteamId)
            {
                ExecuteGameSetup(lobby);
            }
        }
    }

    void AssignMyWeapon(Lobby lobby)
    {
        string weaponIDStr = lobby.GetMemberData(
            lobby.Members.FirstOrDefault(m => m.Id == SteamClient.SteamId), 
            "SelectedWeapon"
        );
        
        if (string.IsNullOrEmpty(weaponIDStr))
        {
            weaponIDStr = SteamLobby.Instance.GetSelectedWeaponID().ToString();
        }

        int weaponID = int.TryParse(weaponIDStr, out int id) ? id : 0;
        
        switch (weaponID)
        {
            case 0: MyWeapon = new Weapon("Auto", autoDamage, autoMaxAmmo, true); break;
            case 1: MyWeapon = new Weapon("Revolver", revolverDamage, revolverMaxAmmo, false); break;
            case 2: MyWeapon = new Weapon("Break", breakDamage, breakMaxAmmo, false); break;
            default: MyWeapon = new Weapon("Auto", autoDamage, autoMaxAmmo, true); break;
        }

        lobby.SetMemberData("WeaponState", MyWeapon.Serialize());
        Debug.Log($"<color=green>무기 할당 완료: {MyWeapon.Name}</color>");
    }

    void InitializeJobUI()
    {
        if (MyJob == null) return;

        if (jobNameText != null) jobNameText.text = MyJob.Name;
        if (jobCardUI != null) jobCardUI.SetActive(true);

        if (skillButton != null)
        {
            bool isActiveSkill = MyJob.SkillType == SkillType.Active;
            skillButton.gameObject.SetActive(isActiveSkill);

            if (isActiveSkill && skillButtonText != null)
            {
                skillButtonText.text = GetSkillButtonText(MyJob.Type);
            }
        }
    }

    string GetSkillButtonText(JobType type)
    {
        return type switch
        {
            JobType.SpeedShooter => "재장전+발포",
            JobType.Hacker => "해킹",
            JobType.Doctor => "치료",
            _ => "스킬"
        };
    }

    void OnDestroy()
    {
        SteamMatchmaking.OnLobbyDataChanged -= OnLobbyDataUpdate;
        SteamMatchmaking.OnLobbyMemberDataChanged -= OnMemberDataUpdate;
    }

    void Update()
    {
        // ===== 테스트 키 =====
        if (Keyboard.current != null && mySeatIndex >= 0)
        {
            if (Keyboard.current.digit1Key.wasPressedThisFrame)
                StartCoroutine(TestAim(mySeatIndex, 2));  // 정면
            if (Keyboard.current.digit2Key.wasPressedThisFrame)
                StartCoroutine(TestAim(mySeatIndex, 1));  // 왼쪽
            if (Keyboard.current.digit3Key.wasPressedThisFrame)
                StartCoroutine(TestAim(mySeatIndex, 3));  // 오른쪽
            if (Keyboard.current.digit4Key.wasPressedThisFrame)
                StartCoroutine(PlayReloadSequence(mySeatIndex));  // 재장전
            if (Keyboard.current.digit5Key.wasPressedThisFrame)
                StartCoroutine(PlayHitEffect(mySeatIndex));  // 피격
        }
        // ===== 테스트 키 끝 =====

        // 우클릭 토글 - 시점 회전 모드
        if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
        {
            isLookMode = !isLookMode;
            
            if (isLookMode)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        // 시점 회전 모드일 때 마우스로 카메라/머리 회전
        if (isLookMode && Mouse.current != null)
        {
            float mouseX = Mouse.current.delta.x.ReadValue() * lookSensitivity * 0.1f;
            float mouseY = Mouse.current.delta.y.ReadValue() * lookSensitivity * 0.1f;
            
            lookRotX += mouseX;
            lookRotY -= mouseY;
            
            lookRotX = Mathf.Clamp(lookRotX, -maxLookX, maxLookX);
            lookRotY = Mathf.Clamp(lookRotY, -maxLookY, 2.5f);  // 아래는 2.5까지만
        }

        // 타이브레이커
        if (currentPhase == GamePhase.Tiebreaker && isTiebreakerActive)
        {
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
                HandleTiebreakerClick();
            }
            return;
        }

        if (currentPhase != GamePhase.ActionSelecting || isConfirmed || isDead) return;

        // 시점 회전 모드면 클릭 무시
        if (isLookMode) return;

        // R 키 - 재장전
        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
        {
            OnReloadButtonClick();
        }

        // 마우스 클릭 - 타겟 선택
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
            HandleTargetSelection();
        }
    }

    void LateUpdate()
    {
        if (!isCameraInitialized || gameCamera == null) return;
        
        // 카메라 회전 적용
        gameCamera.transform.localRotation = Quaternion.Euler(camRotation.x + lookRotY, lookRotX, 0);
        
        // 머리 본 회전 - Animation Rigging 사용 시 비활성화
        // if (headBone != null)
        // {
        //     Quaternion lookRotation = Quaternion.Euler(-lookRotX, 0, lookRotY + 15f);
        //     headBone.localRotation = headBoneInitialRotation * lookRotation;
        // }
        
        // HP 텍스트 빌보드
        foreach (var hpText in hpTextCache.Values)
        {
            if (hpText != null)
                hpText.transform.parent.LookAt(hpText.transform.parent.position + gameCamera.transform.forward);
        }
        
        UpdateActionButtonPosition();
    }

    void UpdateActionButtonPosition()
    {
        // 화면 고정 위치 사용 - Inspector에서 RectTransform으로 위치 설정
        // 더 이상 월드 좌표 기반으로 이동하지 않음
    }

    void ResetActionSelection()
    {
        tempTargetIndex = -1;
        tempSecondTargetIndex = -1;
        isReloadingReserved = false;
        isGunSelected = false;
        isUsingActiveSkill = false;
        activeSkillTargetIndex = -1;
        if (cancelXButton != null) cancelXButton.SetActive(false);
    }

    void HandleTargetSelection()
    {
        if (!isGunSelected && !(isUsingActiveSkill && MyJob?.Type == JobType.Hacker)) return;

        Vector2 mousePosition = Mouse.current.position.ReadValue();
        Ray ray = gameCamera.ScreenPointToRay(mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            GameObject hitObj = hit.collider.gameObject;
            int seatIdx = FindSeatIndex(hitObj);

            if (seatIdx != -1 && seatIdx != mySeatIndex)
            {
                if (isUsingActiveSkill && MyJob?.Type == JobType.Hacker)
                {
                    activeSkillTargetIndex = seatIdx;
                    isUsingActiveSkill = false;
                    if (cancelXButton != null) cancelXButton.SetActive(false);
                    Debug.Log($"<color=magenta>해킹 타겟: {seatIdx}</color>");
                    UpdateActionSubmitButtonState();
                    return;
                }

                if (isGunSelected)
                {
                    if (MyWeapon.IsAutoWeapon && tempTargetIndex != -1 && tempSecondTargetIndex == -1)
                    {
                        if (seatIdx != tempTargetIndex)
                        {
                            tempSecondTargetIndex = seatIdx;
                            Debug.Log($"<color=green>두 번째 타겟: {seatIdx}</color>");
                        }
                    }
                    else
                    {
                        tempTargetIndex = seatIdx;
                        tempSecondTargetIndex = -1;
                        Debug.Log($"<color=green>타겟 선택: {seatIdx}</color>");
                    }

                    if (!MyWeapon.IsAutoWeapon || tempSecondTargetIndex != -1)
                    {
                        isGunSelected = false;
                        if (cancelXButton != null) cancelXButton.SetActive(false);
                    }

                    UpdateActionSubmitButtonState();
                }
            }
        }
    }

    int FindSeatIndex(GameObject hitObj)
    {
        for (int i = 0; i < playerSeats.Length; i++)
        {
            if (playerSeats[i].seatClickCollider == null) continue;
            if (playerSeats[i].seatClickCollider == hitObj ||
                (hitObj.transform.parent != null && playerSeats[i].seatClickCollider == hitObj.transform.parent.gameObject))
            {
                return i;
            }
        }
        return -1;
    }

    void SetupLocalPlayer(GameObject character)
    {
        int layer = LayerMask.NameToLayer("LocalPlayerBody");
        Transform geo = character.transform.Find("Geo");
        
        if (geo == null) return;
        
        // 머리카락만 숨김 (얼굴/피부는 유지 - 손 때문에)
        string[] hideParts = {
            "HAIR_A", "HAIR_B", "HAIR_C", "HAIR_D", "HAIR_E", 
            "HAIR_F", "HAIR_G", "HAIR_H", "HAIR_I"
        };
        
        foreach (string partName in hideParts)
        {
            Transform part = geo.Find(partName);
            if (part != null)
            {
                SetLayerRecursively(part.gameObject, layer);
            }
        }
        
        // 머리 본 찾아서 연결 (headBone이 null이면)
        if (headBone == null)
        {
            Transform root = character.transform.Find("root");
            if (root != null)
            {
                headBone = FindBoneRecursive(root, "Head");
            }
        }
        
        // 초기 회전 저장
        if (headBone != null)
        {
            headBoneInitialRotation = headBone.localRotation;
        }
    }

    Transform FindBoneRecursive(Transform parent, string boneName)
    {
        foreach (Transform child in parent)
        {
            if (child.name.ToLower().Contains(boneName.ToLower()))
            {
                return child;
            }
            Transform found = FindBoneRecursive(child, boneName);
            if (found != null) return found;
        }
        return null;
    }

    void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }

    public void OnCancelClick()
    {
        ResetActionSelection();
        UpdateActionSubmitButtonState();
    }

    // ==================== 버튼 클릭 핸들러 ====================

    public void OnShootButtonClick()
    {
        if (currentPhase != GamePhase.ActionSelecting || isConfirmed || isDead) return;

        if (MyWeapon != null && !MyWeapon.CanFire())
        {
            Debug.LogWarning("탄약이 없습니다! 재장전하세요.");
            return;
        }

        ResetActionSelection();
        isGunSelected = true;
        if (cancelXButton != null) cancelXButton.SetActive(true);
        
        if (MyWeapon.IsAutoWeapon)
        {
            Debug.Log("<color=cyan>발사 모드: 타겟 2명을 선택하세요 (또는 1명 집중)</color>");
        }
        else
        {
            Debug.Log("<color=cyan>발사 모드: 타겟을 선택하세요</color>");
        }
    }

    public void OnReloadButtonClick()
    {
        if (currentPhase != GamePhase.ActionSelecting || isConfirmed || isDead) return;

        ResetActionSelection();
        isReloadingReserved = true;
        Debug.Log("<color=yellow>재장전 예약됨</color>");
        UpdateActionSubmitButtonState();
    }

    public void OnSkillButtonClick()
    {
        if (MyJob == null || MyJob.IsUsed) return;
        if (currentPhase != GamePhase.ActionSelecting || isConfirmed || isDead) return;

        switch (MyJob.Type)
        {
            case JobType.Doctor:
                ResetActionSelection();
                isUsingActiveSkill = true;
                Debug.Log("<color=green>의사 스킬 사용 예약</color>");
                UpdateActionSubmitButtonState();
                break;

            case JobType.Hacker:
                ResetActionSelection();
                isUsingActiveSkill = true;
                if (cancelXButton != null) cancelXButton.SetActive(true);
                Debug.Log("<color=magenta>해킹 타겟을 선택하세요</color>");
                break;

            case JobType.SpeedShooter:
                ResetActionSelection();
                isUsingActiveSkill = true;
                isGunSelected = true;
                if (cancelXButton != null) cancelXButton.SetActive(true);
                Debug.Log("<color=cyan>스피드슈터: 타겟을 선택하세요 (재장전+발포)</color>");
                break;
        }
    }

    public void OnJobCardClick()
    {
        if (jobDetailPanel == null || MyJob == null) return;

        bool isActive = jobDetailPanel.activeSelf;
        jobDetailPanel.SetActive(!isActive);

        if (!isActive)
        {
            var descText = jobDetailPanel.GetComponentInChildren<TextMeshProUGUI>();
            if (descText != null)
            {
                descText.text = $"<b>{MyJob.Name}</b>\n\n{MyJob.Description}";
            }
        }
    }

    void UpdateActionSubmitButtonState()
    {
        bool hasAction = false;

        if (isReloadingReserved) hasAction = true;
        if (tempTargetIndex != -1) hasAction = true;

        if (isUsingActiveSkill)
        {
            if (MyJob?.Type == JobType.Doctor) hasAction = true;
            if (MyJob?.Type == JobType.Hacker && activeSkillTargetIndex != -1) hasAction = true;
            if (MyJob?.Type == JobType.SpeedShooter && tempTargetIndex != -1) hasAction = true;
        }

        if (actionSubmitButton != null) actionSubmitButton.SetActive(hasAction);
    }

    public void OnActionSubmitClick()
    {
        if (isConfirmed) return;
        
        bool hasValidAction = isReloadingReserved || tempTargetIndex != -1 || 
                              (isUsingActiveSkill && MyJob?.Type == JobType.Doctor) ||
                              (isUsingActiveSkill && MyJob?.Type == JobType.Hacker && activeSkillTargetIndex != -1);

        if (!hasValidAction)
        {
            Debug.LogWarning("행동을 선택해주세요!");
            return;
        }

        isConfirmed = true;
        currentPhase = GamePhase.ResultsShowing;

        if (actionSubmitButton != null) actionSubmitButton.SetActive(false);
        if (cancelXButton != null) cancelXButton.SetActive(false);
        if (WaitingUI != null) WaitingUI.SetActive(true);
        HideActionButtons();

        if (SteamLobby.Instance.CurrentLobby.HasValue)
        {
            var lobby = SteamLobby.Instance.CurrentLobby.Value;
            string myId = SteamClient.SteamId.ToString();
            
            lobby.SetData($"Turn_Target_{myId}", tempTargetIndex.ToString());
            lobby.SetData($"Turn_Target2_{myId}", tempSecondTargetIndex.ToString());
            lobby.SetData($"Turn_Reload_{myId}", isReloadingReserved.ToString());
            lobby.SetData($"Turn_UseSkill_{myId}", isUsingActiveSkill.ToString());
            lobby.SetData($"Turn_SkillTarget_{myId}", activeSkillTargetIndex.ToString());
            lobby.SetData($"Turn_Ready_{myId}", "true");

            Debug.Log($"<color=magenta>행동 제출: Target={tempTargetIndex}, Target2={tempSecondTargetIndex}, Reload={isReloadingReserved}, Skill={isUsingActiveSkill}</color>");

            // 테스트 모드: 가짜 플레이어 행동도 제출
            if (isTestMode)
            {
                SubmitFakePlayerActions(lobby);
            }

            if (lobby.Owner.Id == SteamClient.SteamId && turnCheckCoroutine == null)
            {
                turnCheckCoroutine = StartCoroutine(HostTurnCheckRoutine());
            }
        }
    }

    IEnumerator HostTurnCheckRoutine()
    {
        yield return new WaitForSeconds(1.0f);
        
        while (!isProcessingResults && SteamLobby.Instance.CurrentLobby.HasValue)
        {
            var lobby = SteamLobby.Instance.CurrentLobby.Value;
            string gameState = lobby.GetData("GameState");
            
            if (gameState == "GameReady")
            {
                CheckAllTurnsSubmitted(lobby);
            }
            else
            {
                turnCheckCoroutine = null;
                yield break;
            }
            yield return new WaitForSeconds(0.3f);
        }
        turnCheckCoroutine = null;
    }

    private void OnLobbyDataUpdate(Lobby lobby)
    {
        string currentState = lobby.GetData("GameState");
        Debug.Log($"[GameManager] 상태: {currentState}");

        // Phase 변경 감지 (애니메이션 재생)
        string currentPhaseStr = lobby.GetData("Phase");
        if (!string.IsNullOrEmpty(currentPhaseStr) && currentPhaseStr != lastProcessedPhase)
        {
            lastProcessedPhase = currentPhaseStr;
            HandlePhaseChange(lobby, currentPhaseStr);
        }

        if (currentState == lastProcessedState && currentState != "GameReady") return;

        switch (currentState)
        {
            case "DistributingItems":
                lastProcessedState = currentState;
                if (lobby.Owner.Id == SteamClient.SteamId)
                    ExecuteGameSetup(lobby);
                break;

            case "GameReady":
                if (lastProcessedState == "GameReady") return;
                lastProcessedState = currentState;

                isConfirmed = false;
                isProcessingResults = false;
                currentPhase = GamePhase.ActionSelecting;

                ResetActionSelection();

                if (WaitingUI != null) WaitingUI.SetActive(false);
                if (actionSubmitButton != null) actionSubmitButton.SetActive(false);
                
                ShowActionButtons();

                FetchAndInitializeStage(lobby);

                if (lobby.Owner.Id == SteamClient.SteamId)
                {
                    if (turnCheckCoroutine != null) StopCoroutine(turnCheckCoroutine);
                    turnCheckCoroutine = StartCoroutine(HostTurnCheckRoutine());
                }

                Debug.Log("<color=cyan>===== 행동을 선택하세요 =====</color>");
                break;

            case "ProcessingResults":
                lastProcessedState = currentState;
                isConfirmed = true;
                isProcessingResults = true;
                currentPhase = GamePhase.ResultsShowing;
                if (WaitingUI != null) WaitingUI.SetActive(true);
                if (actionSubmitButton != null) actionSubmitButton.SetActive(false);
                break;

            case "ShowResults":
                lastProcessedState = currentState;
                UpdateAllPlayersSpatialUI(lobby);
                SyncMyWeaponState(lobby);
                CheckMyDeath(lobby);
                break;

            case "Tiebreaker":
                if (lastProcessedState != "Tiebreaker")
                {
                    lastProcessedState = currentState;
                    currentPhase = GamePhase.Tiebreaker;
                    isConfirmed = false;
                    isProcessingResults = false;
                    if (WaitingUI != null) WaitingUI.SetActive(false);
                    InitializeTiebreaker(lobby);
                }
                else
                {
                    CheckTiebreakerStatus(lobby);
                }
                break;

            case "GameOver":
                lastProcessedState = currentState;
                HandleGameOver(lobby);
                break;
        }
    }

    void ShowActionButtons()
    {
        if (actionButtonPanel != null) actionButtonPanel.SetActive(true);
        
        if (shootButton != null)
        {
            shootButton.interactable = MyWeapon != null && MyWeapon.CanFire();
        }

        if (reloadButton != null)
        {
            reloadButton.interactable = MyWeapon != null && MyWeapon.CurrentAmmo < MyWeapon.MaxAmmo;
        }

        if (skillButton != null && MyJob != null)
        {
            bool isActive = MyJob.SkillType == SkillType.Active && !MyJob.IsUsed;
            skillButton.gameObject.SetActive(MyJob.SkillType == SkillType.Active);
            skillButton.interactable = isActive;

            if (skillButtonText != null)
            {
                skillButtonText.text = MyJob.IsUsed ? "(사용됨)" : GetSkillButtonText(MyJob.Type);
            }
        }
    }

    void HideActionButtons()
    {
        if (actionButtonPanel != null) actionButtonPanel.SetActive(false);
    }

    void CheckAllTurnsSubmitted(Lobby lobby)
    {
        bool allReady = true;
        int aliveCount = 0;
        
        // 실제 플레이어 체크
        foreach (var m in lobby.Members)
        {
            string deadStr = lobby.GetMemberData(m, "IsDead");
            if (deadStr == "true" || deadStr == "True") continue;
            
            aliveCount++;
            string turnReady = lobby.GetData($"Turn_Ready_{m.Id}");
            if (turnReady != "true" && turnReady != "True") allReady = false;
        }
        
        // 테스트 모드: 가짜 플레이어도 체크
        if (isTestMode)
        {
            foreach (var fakeId in fakePlayerIds)
            {
                string deadStr = lobby.GetData($"IsDead_{fakeId}");
                if (deadStr == "true" || deadStr == "True") continue;
                
                aliveCount++;
                string turnReady = lobby.GetData($"Turn_Ready_{fakeId}");
                if (turnReady != "true" && turnReady != "True") allReady = false;
            }
        }

        if (aliveCount <= 1)
        {
            Debug.Log("<color=yellow>생존자 1명 이하</color>");
        }

        if (allReady && !isProcessingResults && aliveCount > 0)
        {
            isProcessingResults = true;
            Debug.Log("<color=yellow>모든 플레이어 행동 제출 완료</color>");
            lobby.SetData("GameState", "ProcessingResults");
            ResolveAllTurns(lobby);
        }
    }

    void ResolveAllTurns(Lobby lobby)
    {
        if (lobby.Owner.Id != SteamClient.SteamId) return;
        StartCoroutine(ResolveAllTurnsCoroutine(lobby));
    }

    IEnumerator ResolveAllTurnsCoroutine(Lobby lobby)
    {
        Debug.Log("<color=yellow>========== 턴 정산 ==========</color>");
        var members = lobby.Members.ToList();

        Dictionary<ulong, int> hpMap = new Dictionary<ulong, int>();
        Dictionary<ulong, Job> jobMap = new Dictionary<ulong, Job>();
        Dictionary<ulong, bool> gotKillThisTurn = new Dictionary<ulong, bool>();
        Dictionary<ulong, List<ulong>> attackersMap = new Dictionary<ulong, List<ulong>>();

        foreach (var m in members)
        {
            string hpStr = lobby.GetData($"HP_{m.Id}");
            hpMap[m.Id] = string.IsNullOrEmpty(hpStr) ? maxHP : int.Parse(hpStr);
            
            string jobStr = lobby.GetData($"Job_{m.Id}");
            jobMap[m.Id] = int.TryParse(jobStr, out int jobIdx) ? Job.Create((JobType)jobIdx) : null;
            
            gotKillThisTurn[m.Id] = false;
            attackersMap[m.Id] = new List<ulong>();
        }

        Dictionary<ulong, bool> isBlocked = new Dictionary<ulong, bool>();
        foreach (var m in members) isBlocked[m.Id] = false;

        // ========== 스킬 페이즈 ==========
        lobby.SetData("Phase", "Skill");
        
        // 해커 스킬 처리
        foreach (var member in members)
        {
            string deadStr = lobby.GetMemberData(member, "IsDead");
            if (deadStr == "true" || deadStr == "True") continue;

            string useSkillStr = lobby.GetData($"Turn_UseSkill_{member.Id}");
            bool usedSkill = useSkillStr == "True" || useSkillStr == "true";

            if (usedSkill && jobMap[member.Id]?.Type == JobType.Hacker && !jobMap[member.Id].IsUsed)
            {
                string skillTargetStr = lobby.GetData($"Turn_SkillTarget_{member.Id}");
                if (int.TryParse(skillTargetStr, out int skillTargetIdx) && skillTargetIdx >= 0 && skillTargetIdx < members.Count)
                {
                    ulong targetId = members[skillTargetIdx].Id;
                    isBlocked[targetId] = true;
                    jobMap[member.Id].IsUsed = true;
                    lobby.SetData($"Blocked_{targetId}", "true");
                    Debug.Log($"<color=magenta>해커 발동! {members[skillTargetIdx].Name} 행동 무효</color>");
                }
            }
        }

        // 의사 스킬 처리
        foreach (var member in members)
        {
            if (isBlocked[member.Id]) continue;
            string deadStr = lobby.GetMemberData(member, "IsDead");
            if (deadStr == "true" || deadStr == "True") continue;

            string useSkillStr = lobby.GetData($"Turn_UseSkill_{member.Id}");
            bool usedSkill = useSkillStr == "True" || useSkillStr == "true";

            if (usedSkill && jobMap[member.Id]?.Type == JobType.Doctor && !jobMap[member.Id].IsUsed)
            {
                JobEffectHandler.UseDoctorSkill(jobMap[member.Id], member.Id, hpMap, maxHP);
                lobby.SetData($"HP_{member.Id}", hpMap[member.Id].ToString());
            }
        }

        yield return new WaitForSeconds(2f);  // 스킬 애니메이션 대기

        // ========== 재장전 페이즈 ==========
        lobby.SetData("Phase", "Reload");
        
        // 재장전 처리
        foreach (var member in members)
        {
            if (isBlocked[member.Id]) continue;
            string deadStr = lobby.GetMemberData(member, "IsDead");
            if (deadStr == "true" || deadStr == "True") continue;

            string memberId = member.Id.ToString();
            string reloadStr = lobby.GetData($"Turn_Reload_{memberId}");
            string useSkillStr = lobby.GetData($"Turn_UseSkill_{memberId}");
            bool isReloading = reloadStr == "True" || reloadStr == "true";
            bool usedSkill = useSkillStr == "True" || useSkillStr == "true";

            // 스피드슈터는 발포 페이즈에서 처리
            bool isSpeedShooter = usedSkill && jobMap[member.Id]?.Type == JobType.SpeedShooter && !jobMap[member.Id].IsUsed;
            
            if (isReloading && !isSpeedShooter)
            {
                Debug.Log($"{member.Name} 재장전");
                string weaponIDStr = lobby.GetMemberData(member, "SelectedWeapon");
                int weaponID = string.IsNullOrEmpty(weaponIDStr) ? 0 : int.Parse(weaponIDStr);
                int weaponMaxAmmo = (weaponID == 0) ? autoMaxAmmo : (weaponID == 1) ? revolverMaxAmmo : breakMaxAmmo;
                lobby.SetData($"WeaponState_{member.Id}", weaponMaxAmmo.ToString());
            }
        }

        yield return new WaitForSeconds(3f);  // 재장전 애니메이션 대기

        // ========== 발포 페이즈 ==========
        lobby.SetData("Phase", "Fire");

        Dictionary<ulong, int> damageReceived = new Dictionary<ulong, int>();
        foreach (var m in members) damageReceived[m.Id] = 0;

        foreach (var member in members)
        {
            if (isBlocked[member.Id])
            {
                Debug.Log($"{member.Name} 행동 무효됨 (해커)");
                continue;
            }

            string deadStr = lobby.GetMemberData(member, "IsDead");
            if (deadStr == "true" || deadStr == "True") continue;

            string memberId = member.Id.ToString();
            string reloadStr = lobby.GetData($"Turn_Reload_{memberId}");
            string targetStr = lobby.GetData($"Turn_Target_{memberId}");
            string target2Str = lobby.GetData($"Turn_Target2_{memberId}");
            string useSkillStr = lobby.GetData($"Turn_UseSkill_{memberId}");

            bool isReloading = reloadStr == "True" || reloadStr == "true";
            bool usedSkill = useSkillStr == "True" || useSkillStr == "true";
            int targetIdx = string.IsNullOrEmpty(targetStr) ? -1 : int.Parse(targetStr);
            int target2Idx = string.IsNullOrEmpty(target2Str) ? -1 : int.Parse(target2Str);

            bool isSpeedShooter = usedSkill && jobMap[member.Id]?.Type == JobType.SpeedShooter && !jobMap[member.Id].IsUsed;
            if (isSpeedShooter)
            {
                Debug.Log($"{member.Name} 스피드슈터 발동! 재장전 + 발포");
                jobMap[member.Id].IsUsed = true;
                
                string weaponIDStr = lobby.GetMemberData(member, "SelectedWeapon");
                int weaponID = string.IsNullOrEmpty(weaponIDStr) ? 0 : int.Parse(weaponIDStr);
                int weaponMaxAmmo = (weaponID == 0) ? autoMaxAmmo : (weaponID == 1) ? revolverMaxAmmo : breakMaxAmmo;
                lobby.SetData($"WeaponState_{member.Id}", weaponMaxAmmo.ToString());
                
                isReloading = false;
            }
            else if (isReloading)
            {
                continue;  // 재장전은 이미 처리됨
            }

            if (targetIdx != -1 && targetIdx < members.Count)
            {
                string weaponState = lobby.GetData($"WeaponState_{member.Id}");
                string weaponIDStr = lobby.GetMemberData(member, "SelectedWeapon");
                int weaponID = string.IsNullOrEmpty(weaponIDStr) ? 0 : int.Parse(weaponIDStr);
                
                int baseDamage = (weaponID == 0) ? autoDamage : (weaponID == 1) ? revolverDamage : breakDamage;
                bool isAuto = (weaponID == 0);
                int currentAmmo = string.IsNullOrEmpty(weaponState) ? 0 : int.Parse(weaponState);
                int ammoNeeded = isAuto ? 2 : 1;

                if (isSpeedShooter)
                {
                    currentAmmo = (weaponID == 0) ? autoMaxAmmo : (weaponID == 1) ? revolverMaxAmmo : breakMaxAmmo;
                }

                if (currentAmmo < ammoNeeded)
                {
                    Debug.Log($"{member.Name} 탄약 부족");
                    continue;
                }

                var target1 = members[targetIdx];
                int target1HP = hpMap[target1.Id];
                int damage1 = JobEffectHandler.ModifyDamage(jobMap[member.Id], baseDamage, target1HP);
                
                damageReceived[target1.Id] += damage1;
                attackersMap[target1.Id].Add(member.Id);
                Debug.Log($"{member.Name} -> {target1.Name}: {damage1}뎀");

                if (isAuto && target2Idx != -1 && target2Idx < members.Count)
                {
                    var target2 = members[target2Idx];
                    int target2HP = hpMap[target2.Id];
                    int damage2 = JobEffectHandler.ModifyDamage(jobMap[member.Id], baseDamage, target2HP);
                    
                    damageReceived[target2.Id] += damage2;
                    attackersMap[target2.Id].Add(member.Id);
                    Debug.Log($"{member.Name} -> {target2.Name}: {damage2}뎀");
                }

                currentAmmo -= ammoNeeded;
                lobby.SetData($"WeaponState_{member.Id}", currentAmmo.ToString());
            }
        }

        yield return new WaitForSeconds(3f);  // 발포 애니메이션 대기

        // ========== 결과 페이즈 ==========
        lobby.SetData("Phase", "Result");

        List<ulong> thisRoundDead = new List<ulong>();
        bool isFirstDeath = true;
        ulong? jokerWinner = null;

        foreach (var member in members)
        {
            string wasDeadStr = lobby.GetMemberData(member, "IsDead");
            if (wasDeadStr == "true" || wasDeadStr == "True") continue;

            int damage = damageReceived[member.Id];
            if (damage <= 0) continue;

            int oldHP = hpMap[member.Id];
            int newHP = oldHP - damage;

            if (newHP <= 0)
            {
                JobEffectHandler.CheckSheriffSurvival(jobMap[member.Id], ref newHP);
            }

            hpMap[member.Id] = Mathf.Max(0, newHP);
            Debug.Log($"{member.Name}: HP {oldHP} -> {hpMap[member.Id]}");

            if (hpMap[member.Id] <= 0)
            {
                string targetStr = lobby.GetData($"Turn_Target_{member.Id}");
                bool didNotAttack = string.IsNullOrEmpty(targetStr) || int.Parse(targetStr) == -1;
                
                if (JobEffectHandler.CheckJokerWin(jobMap[member.Id], isFirstDeath, didNotAttack))
                {
                    jokerWinner = member.Id;
                }

                thisRoundDead.Add(member.Id);
                isFirstDeath = false;
            }
        }

        if (jokerWinner.HasValue)
        {
            lobby.SetData("WinnerId", jokerWinner.Value.ToString());
            lobby.SetData("GameState", "GameOver");
            yield break;
        }

        foreach (var deadId in thisRoundDead.ToList())
        {
            bool gotKill = false;
            foreach (var targetId in attackersMap.Keys)
            {
                if (attackersMap[targetId].Contains(deadId) && hpMap[targetId] <= 0)
                {
                    gotKill = true;
                    break;
                }
            }

            int zombieHP = hpMap[deadId];
            if (JobEffectHandler.CheckZombieRevival(jobMap[deadId], gotKill, ref zombieHP))
            {
                thisRoundDead.Remove(deadId);
                hpMap[deadId] = zombieHP;
            }
        }

        foreach (var deadId in thisRoundDead)
        {
            JobEffectHandler.ApplyMartyrEffect(jobMap[deadId], deadId, hpMap);
        }

        foreach (var member in members)
        {
            if (thisRoundDead.Contains(member.Id)) continue;

            foreach (var targetId in attackersMap.Keys)
            {
                if (hpMap[targetId] <= 0 && attackersMap[targetId].Contains(member.Id))
                {
                    bool isSoloKill = attackersMap[targetId].Count == 1;
                    JobEffectHandler.ApplyVampireHeal(jobMap[member.Id], member.Id, isSoloKill, hpMap, maxHP);
                }
            }
        }

        foreach (var m in members)
        {
            lobby.SetData($"HP_{m.Id}", hpMap[m.Id].ToString());
        }

        List<ulong> survivors = members.Where(m => hpMap[m.Id] > 0).Select(m => (ulong)m.Id).ToList();

        foreach (var m in members)
        {
            if (hpMap[m.Id] <= 0 && !thisRoundDead.Contains(m.Id))
            {
                string wasDeadStr = lobby.GetMemberData(m, "IsDead");
                if (wasDeadStr != "true" && wasDeadStr != "True")
                {
                    thisRoundDead.Add(m.Id);
                    survivors.Remove(m.Id);
                }
            }
        }

        Debug.Log($"생존자: {survivors.Count}명, 이번 턴 사망: {thisRoundDead.Count}명");

        if (survivors.Count == 0 && thisRoundDead.Count >= 2)
        {
            foreach (var deadId in thisRoundDead)
            {
                if (JobEffectHandler.CheckMartyrTiebreakerWin(jobMap[deadId]))
                {
                    lobby.SetData("WinnerId", deadId.ToString());
                    lobby.SetData("GameState", "GameOver");
                    yield break;
                }
            }

            Debug.Log("<color=magenta>타이브레이커!</color>");
            lobby.SetData("TiebreakerParticipants", string.Join(",", thisRoundDead));
            
            List<bool> chamber = new List<bool> { false };
            for (int i = 1; i < thisRoundDead.Count; i++) chamber.Add(true);
            for (int i = 0; i < chamber.Count; i++)
            {
                bool temp = chamber[i];
                int randomIdx = UnityEngine.Random.Range(i, chamber.Count);
                chamber[i] = chamber[randomIdx];
                chamber[randomIdx] = temp;
            }
            lobby.SetData("TiebreakerChamber", string.Join(",", chamber.Select(b => b ? "1" : "0")));
            lobby.SetData("GameState", "Tiebreaker");
            yield break;
        }

        if (survivors.Count == 1)
        {
            lobby.SetData("WinnerId", survivors[0].ToString());
            lobby.SetData("GameState", "GameOver");
            yield break;
        }

        if (survivors.Count == 0)
        {
            lobby.SetData("GameState", "GameOver");
            yield break;
        }

        lobby.SetData("GameState", "ShowResults");
        StartCoroutine(NextTurnRoutine(lobby));
    }

    IEnumerator NextTurnRoutine(Lobby lobby)
    {
        yield return new WaitForSeconds(2.0f);
        turnCheckCoroutine = null;

        foreach (var m in lobby.Members)
        {
            string memberId = m.Id.ToString();
            lobby.SetData($"Turn_Ready_{memberId}", "false");
            lobby.SetData($"Turn_Target_{memberId}", "-1");
            lobby.SetData($"Turn_Target2_{memberId}", "-1");
            lobby.SetData($"Turn_Reload_{memberId}", "false");
            lobby.SetData($"Turn_UseSkill_{memberId}", "false");
            lobby.SetData($"Turn_SkillTarget_{memberId}", "-1");
            lobby.SetData($"Blocked_{m.Id}", "false");  // 봉인 초기화
        }
        
        // 테스트 모드: 가짜 플레이어 데이터도 초기화
        if (isTestMode)
        {
            foreach (var fakeId in fakePlayerIds)
            {
                lobby.SetData($"Turn_Ready_{fakeId}", "false");
                lobby.SetData($"Turn_Target_{fakeId}", "-1");
                lobby.SetData($"Turn_Target2_{fakeId}", "-1");
                lobby.SetData($"Turn_Reload_{fakeId}", "false");
                lobby.SetData($"Turn_UseSkill_{fakeId}", "false");
                lobby.SetData($"Turn_SkillTarget_{fakeId}", "-1");
                lobby.SetData($"Blocked_{fakeId}", "false");
            }
        }
        
        lobby.SetData("Phase", "");  // 페이즈 초기화
        lastProcessedPhase = "";  // 로컬 페이즈 추적 초기화

        yield return new WaitForSeconds(0.5f);

        lastProcessedState = "NextTurnTransition";
        isProcessingResults = false;
        isConfirmed = false;
        currentPhase = GamePhase.WaitingForStart;
        
        lobby.SetData("GameState", "GameReady");
    }

    void SyncMyWeaponState(Lobby lobby)
    {
        if (MyWeapon == null) return;
        string weaponState = lobby.GetData($"WeaponState_{SteamClient.SteamId}");
        if (!string.IsNullOrEmpty(weaponState))
        {
            MyWeapon.Deserialize(weaponState);
            if (ammoTextUI != null) ammoTextUI.text = MyWeapon.GetAmmoText();
        }
    }

    void CheckMyDeath(Lobby lobby)
    {
        if (isDead) return;
        string hpStr = lobby.GetData($"HP_{SteamClient.SteamId}");
        if (string.IsNullOrEmpty(hpStr)) return;

        if (int.Parse(hpStr) <= 0)
        {
            isDead = true;
            lobby.SetMemberData("IsDead", "true");
            ApplyDeathEffect();
            Debug.Log("<color=red>당신은 사망했습니다</color>");
        }
    }

    void ApplyDeathEffect()
    {
        if (deathOverlayPanel != null) deathOverlayPanel.SetActive(true);
        else CreateDeathOverlay();
        if (actionSubmitButton != null) actionSubmitButton.SetActive(false);
        if (cancelXButton != null) cancelXButton.SetActive(false);
        if (WaitingUI != null) WaitingUI.SetActive(false);
        HideActionButtons();
    }

    void CreateDeathOverlay()
    {
        if (hudCanvas == null) return;
        GameObject overlay = new GameObject("DeathOverlay");
        overlay.transform.SetParent(hudCanvas.transform, false);
        RectTransform rt = overlay.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        var img = overlay.AddComponent<UnityEngine.UI.Image>();
        img.color = new UnityEngine.Color(0.3f, 0.3f, 0.3f, 0.7f);
        img.raycastTarget = false;
        deathOverlayPanel = overlay;
    }

    void ExecuteGameSetup(Lobby lobby)
    {
        Debug.Log("게임 셋업 시작");
        
        // 테스트 모드 체크 (플레이어 1명일 때)
        int realPlayerCount = lobby.Members.Count();
        if (enableTestMode && realPlayerCount == 1)
        {
            isTestMode = true;
            InitializeFakePlayers(lobby);
        }
        else
        {
            isTestMode = false;
            fakePlayerIds.Clear();
        }
        
        foreach (var member in lobby.Members)
        {
            lobby.SetData($"HP_{member.Id}", maxHP.ToString());

            string weaponIDStr = lobby.GetMemberData(member, "SelectedWeapon");
            if (!string.IsNullOrEmpty(weaponIDStr))
            {
                int weaponID = int.Parse(weaponIDStr);
                int weaponMaxAmmo = (weaponID == 0) ? autoMaxAmmo : (weaponID == 1) ? revolverMaxAmmo : breakMaxAmmo;
                lobby.SetData($"WeaponState_{member.Id}", weaponMaxAmmo.ToString());
            }

            string memberId = member.Id.ToString();
            lobby.SetData($"Turn_Ready_{memberId}", "false");
            lobby.SetData($"Turn_Target_{memberId}", "-1");
            lobby.SetData($"Turn_Target2_{memberId}", "-1");
            lobby.SetData($"Turn_Reload_{memberId}", "false");
            lobby.SetData($"Turn_UseSkill_{memberId}", "false");
            lobby.SetData($"Turn_SkillTarget_{memberId}", "-1");
        }

        lobby.SetData("GameState", "GameReady");
        gameInitialized = true;
    }
    
    void InitializeFakePlayers(Lobby lobby)
    {
        fakePlayerIds.Clear();
        
        // 가짜 플레이어 3명 생성 (임의의 ID)
        for (int i = 0; i < 3; i++)
        {
            ulong fakeId = 10000001ul + (ulong)i;  // 간단한 가짜 ID
            fakePlayerIds.Add(fakeId);
            
            // 가짜 플레이어 데이터 초기화
            lobby.SetData($"HP_{fakeId}", maxHP.ToString());
            lobby.SetData($"Job_{fakeId}", UnityEngine.Random.Range(0, 8).ToString());  // 랜덤 직업
            
            int fakeWeaponID = UnityEngine.Random.Range(0, 3);
            int fakeMaxAmmo = (fakeWeaponID == 0) ? autoMaxAmmo : (fakeWeaponID == 1) ? revolverMaxAmmo : breakMaxAmmo;
            lobby.SetData($"WeaponState_{fakeId}", fakeMaxAmmo.ToString());
            lobby.SetData($"FakeWeapon_{fakeId}", fakeWeaponID.ToString());
            
            lobby.SetData($"Turn_Ready_{fakeId}", "false");
            lobby.SetData($"Turn_Target_{fakeId}", "-1");
            lobby.SetData($"Turn_Target2_{fakeId}", "-1");
            lobby.SetData($"Turn_Reload_{fakeId}", "false");
            lobby.SetData($"Turn_UseSkill_{fakeId}", "false");
            lobby.SetData($"Turn_SkillTarget_{fakeId}", "-1");
        }
    }
    
    void SubmitFakePlayerActions(Lobby lobby)
    {
        if (!isTestMode) return;
        
        var realMembers = lobby.Members.ToList();
        int totalPlayers = realMembers.Count + fakePlayerIds.Count;
        
        for (int i = 0; i < fakePlayerIds.Count; i++)
        {
            ulong fakeId = fakePlayerIds[i];
            int fakeSeatIdx = realMembers.Count + i;  // 실제 플레이어 다음 좌석
            
            // 무조건 발포 - 자기 자신이 아닌 랜덤 타겟 (본인 포함)
            int targetIdx;
            do
            {
                targetIdx = UnityEngine.Random.Range(0, totalPlayers);
            } while (targetIdx == fakeSeatIdx);
            
            lobby.SetData($"Turn_Target_{fakeId}", targetIdx.ToString());
            lobby.SetData($"Turn_Reload_{fakeId}", "false");
            
            lobby.SetData($"Turn_Ready_{fakeId}", "true");
        }
    }

    void FetchAndInitializeStage(Lobby lobby)
    {
        SyncMyWeaponState(lobby);
        InitializeAllCharacters(lobby);  // 캐릭터 초기화 및 커스터마이즈
        UpdateAllPlayersSpatialUI(lobby);

        if (ammoTextUI != null && MyWeapon != null)
        {
            ammoTextUI.gameObject.SetActive(true);
            ammoTextUI.text = MyWeapon.GetAmmoText();
        }
    }

    public void UpdateAllPlayersSpatialUI(Lobby lobby)
    {
        SetMyCameraPosition(lobby);

        foreach (var seat in playerSeats)
            if (seat.seatClickCollider != null) seat.seatClickCollider.SetActive(false);

        var members = lobby.Members.ToList();
        int totalSeats = isTestMode ? 4 : members.Count;
        
        for (int seatIdx = 0; seatIdx < totalSeats && seatIdx < playerSeats.Length; seatIdx++)
        {
            if (playerSeats[seatIdx].seatClickCollider != null)
                playerSeats[seatIdx].seatClickCollider.SetActive(true);

            ulong memberId;
            bool isFake = false;
            
            if (seatIdx < members.Count)
            {
                memberId = members[seatIdx].Id;
            }
            else if (isTestMode && (seatIdx - members.Count) < fakePlayerIds.Count)
            {
                memberId = fakePlayerIds[seatIdx - members.Count];
                isFake = true;
            }
            else
            {
                continue;
            }

            string hpStr = lobby.GetData($"HP_{memberId}");
            if (string.IsNullOrEmpty(hpStr) && !isFake)
            {
                hpStr = lobby.GetMemberData(members[seatIdx], "HP");
            }
            int currentHP = string.IsNullOrEmpty(hpStr) ? maxHP : int.Parse(hpStr);
            bool isLocal = (memberId == SteamClient.SteamId);

            UpdateHPUIAtSeat(seatIdx, currentHP, isLocal);
            
            if (isFake)
            {
                SpawnWeaponAtSeatForFake(seatIdx, memberId, lobby);
            }
            else
            {
                SpawnWeaponAtSeat(seatIdx, members[seatIdx], lobby);
            }
        }
    }
    
    void SpawnWeaponAtSeatForFake(int seatIdx, ulong fakeId, Lobby lobby)
    {
        if (seatIdx < 0 || seatIdx >= playerSeats.Length) return;

        string weaponIDStr = lobby.GetData($"FakeWeapon_{fakeId}");
        int weaponID = string.IsNullOrEmpty(weaponIDStr) ? 0 : int.Parse(weaponIDStr);

        GameObject prefab = weaponID switch
        {
            0 => weaponAutoPrefab,
            1 => weaponRevolverPrefab,
            2 => weaponBreakPrefab,
            _ => weaponAutoPrefab
        };

        if (prefab != null)
        {
            Transform handBone = FindHandBoneAtSeat(seatIdx);
            if (handBone != null)
            {
                Transform existingWeapon = handBone.Find("Weapon");
                if (existingWeapon != null) Destroy(existingWeapon.gameObject);
                
                GameObject gunObj = Instantiate(prefab, handBone);
                gunObj.name = "Weapon";
                gunObj.transform.localPosition = new Vector3(0.1462f, -0.0707f, 0.0471f);
                gunObj.transform.localRotation = Quaternion.Euler(20f, 90f, 90f);
                gunObj.transform.localScale = Vector3.one;
            }
        }
    }
    
    Transform FindHandBoneAtSeat(int seatIdx)
    {
        if (seatIdx < 0 || seatIdx >= playerSeats.Length) return null;
        
        Transform seatRoot = playerSeats[seatIdx].seatRoot;
        if (seatRoot == null) return null;
        
        Transform target = seatRoot.Find("Target");
        if (target == null) target = seatRoot.Find("Target 1");
        if (target == null) return null;
        
        Transform character = target.Find("urp for me");
        if (character == null) return null;
        
        return FindBoneRecursive(character, "hand_r");
    }

    void SetMyCameraPosition(Lobby lobby)
    {
        if (isCameraInitialized) return;

        int index = 0;
        foreach (var m in lobby.Members)
        {
            if (m.Id == SteamClient.SteamId)
            {
                mySeatIndex = index;
                if (mySeatIndex < playerSeats.Length && gameCamera != null && cameraPivot != null)
                {
                    cameraPivot.SetParent(playerSeats[mySeatIndex].seatRoot);
                    cameraPivot.localPosition = Vector3.zero;
                    cameraPivot.localRotation = Quaternion.identity;
                    gameCamera.transform.localPosition = camOffset;
                    gameCamera.transform.localRotation = Quaternion.Euler(camRotation);
                    UpdateHPUIAtSeat(mySeatIndex, maxHP, true);
                    
                    // 본인 캐릭터 설정
                    Transform target = playerSeats[mySeatIndex].seatRoot.Find("Target");
                    if (target == null) target = playerSeats[mySeatIndex].seatRoot.Find("Target 1");
                    if (target != null)
                    {
                        Transform character = target.Find("urp for me");
                        if (character != null)
                        {
                            SetupLocalPlayer(character.gameObject);
                        }
                    }
                    
                    isCameraInitialized = true;
                }
                break;
            }
            index++;
        }
    }

    void UpdateHPUIAtSeat(int seatIdx, int currentHP, bool isLocal)
    {
        if (isLocal && myHpText2D != null)
        {
            myHpText2D.text = $"HP: {currentHP}";
            myHpText2D.color = (currentHP <= 2) ? UnityEngine.Color.red : UnityEngine.Color.white;
        }

        if (hpTextCache.TryGetValue(seatIdx, out TextMeshProUGUI hpText) && hpText != null)
        {
            hpText.text = $"x{currentHP}";
            hpText.color = (currentHP <= 2) ? UnityEngine.Color.red : UnityEngine.Color.white;
        }
    }

    void SpawnWeaponAtSeat(int seatIdx, Friend member, Lobby lobby)
    {
        // 캐릭터의 손 본 찾기
        Transform target = playerSeats[seatIdx].seatRoot.Find("Target");
        if (target == null) target = playerSeats[seatIdx].seatRoot.Find("Target 1");
        if (target == null) return;
        
        Transform character = target.Find("urp for me");
        if (character == null) return;
        
        Transform root = character.Find("root");
        if (root == null) return;
        
        Transform rightHand = FindBoneRecursive(root, "hand_r");
        if (rightHand == null) return;
        
        // 이미 총이 붙어있으면 스킵
        if (rightHand.Find("Weapon") != null) return;
        
        string weaponIDStr = lobby.GetMemberData(member, "SelectedWeapon");
        if (string.IsNullOrEmpty(weaponIDStr)) return;

        int weaponID = int.Parse(weaponIDStr);
        GameObject prefab = (weaponID == 0) ? weaponAutoPrefab : (weaponID == 1) ? weaponRevolverPrefab : weaponBreakPrefab;

        if (prefab != null)
        {
            GameObject gun = Instantiate(prefab, rightHand);
            gun.name = "Weapon";
            gun.transform.localPosition = new Vector3(0.1462f, -0.0707f, 0.0471f);
            gun.transform.localRotation = Quaternion.Euler(20f, 90f, 90f);
            gun.transform.localScale = Vector3.one;
            gun.tag = "Weapon";

            if (member.Id == SteamClient.SteamId && ammoTextUI != null && MyWeapon != null)
                ammoTextUI.text = MyWeapon.GetAmmoText();
        }
    }

    // ==================== 타이브레이커 ====================

    void CheckTiebreakerStatus(Lobby lobby)
    {
        string deadStr = lobby.GetData($"TiebreakerDead_{SteamClient.SteamId}");
        if (deadStr == "true")
        {
            if (isTiebreakerActive)
            {
                isTiebreakerActive = false;
                ApplyDeathEffect();
            }
            return;
        }

        string participantsStr = lobby.GetData("TiebreakerParticipants");
        if (!string.IsNullOrEmpty(participantsStr))
        {
            tiebreakerParticipants.Clear();
            foreach (var idStr in participantsStr.Split(','))
                if (ulong.TryParse(idStr, out ulong id)) tiebreakerParticipants.Add(id);
        }

        string chamberStr = lobby.GetData("TiebreakerChamber");
        if (!string.IsNullOrEmpty(chamberStr))
        {
            tiebreakerChamber.Clear();
            foreach (var b in chamberStr.Split(',')) tiebreakerChamber.Add(b == "1");
        }

        if (tiebreakerParticipants.Contains(SteamClient.SteamId) && !isTiebreakerActive)
        {
            isTiebreakerActive = true;
            isProcessingResults = false;
        }
    }

    void InitializeTiebreaker(Lobby lobby)
    {
        string participantsStr = lobby.GetData("TiebreakerParticipants");
        if (string.IsNullOrEmpty(participantsStr)) return;

        tiebreakerParticipants.Clear();
        foreach (var idStr in participantsStr.Split(','))
            if (ulong.TryParse(idStr, out ulong id)) tiebreakerParticipants.Add(id);

        string chamberStr = lobby.GetData("TiebreakerChamber");
        tiebreakerChamber.Clear();
        if (!string.IsNullOrEmpty(chamberStr))
            foreach (var b in chamberStr.Split(',')) tiebreakerChamber.Add(b == "1");

        if (tiebreakerParticipants.Contains(SteamClient.SteamId))
        {
            if (deathOverlayPanel != null) deathOverlayPanel.SetActive(false);
            isDead = false;
            isTiebreakerActive = true;
        }
        else isTiebreakerActive = false;

        SpawnTiebreakerRevolver();
        if (tiebreakerUI != null) tiebreakerUI.SetActive(true);
        else CreateTiebreakerUI();
    }

    void SpawnTiebreakerRevolver()
    {
        if (tiebreakerRevolverInstance != null) Destroy(tiebreakerRevolverInstance);
        if (weaponRevolverPrefab == null) return;

        Vector3 pos = tiebreakerRevolverAnchor != null ? tiebreakerRevolverAnchor.position : new Vector3(0, 1f, 0);
        Quaternion rot = tiebreakerRevolverAnchor != null ? tiebreakerRevolverAnchor.rotation : Quaternion.identity;

        tiebreakerRevolverInstance = Instantiate(weaponRevolverPrefab, pos, rot);
        tiebreakerRevolverInstance.tag = "TiebreakerRevolver";
        if (tiebreakerRevolverInstance.GetComponent<Collider>() == null)
            tiebreakerRevolverInstance.AddComponent<BoxCollider>();
        tiebreakerRevolverInstance.transform.localScale = Vector3.one * 1.5f;
    }

    void CreateTiebreakerUI()
    {
        if (hudCanvas == null) return;
        GameObject uiObj = new GameObject("TiebreakerUI");
        uiObj.transform.SetParent(hudCanvas.transform, false);
        RectTransform rt = uiObj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.8f);
        rt.anchorMax = new Vector2(0.5f, 0.8f);
        rt.sizeDelta = new Vector2(600, 100);
        rt.anchoredPosition = Vector2.zero;
        TextMeshProUGUI text = uiObj.AddComponent<TextMeshProUGUI>();
        text.text = "TIEBREAKER!\n리볼버를 클릭하세요!";
        text.fontSize = 36;
        text.alignment = TextAlignmentOptions.Center;
        text.color = new UnityEngine.Color(1f, 0.8f, 0f, 1f);
        tiebreakerUI = uiObj;
    }

    void HandleTiebreakerClick()
    {
        if (!isTiebreakerActive) return;

        Ray ray = gameCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            GameObject hitObj = hit.collider.gameObject;
            if (hitObj.CompareTag("TiebreakerRevolver") ||
                (hitObj.transform.parent != null && hitObj.transform.parent.CompareTag("TiebreakerRevolver")))
            {
                isTiebreakerActive = false;
                if (SteamLobby.Instance.CurrentLobby.HasValue)
                {
                    var lobby = SteamLobby.Instance.CurrentLobby.Value;
                    lobby.SetMemberData("TiebreakerClicked", "true");
                    if (lobby.Owner.Id == SteamClient.SteamId)
                        StartCoroutine(ProcessTiebreakerShot(lobby, SteamClient.SteamId));
                }
            }
        }
    }

    private void OnMemberDataUpdate(Lobby lobby, Friend member)
    {
        UpdateAllPlayersSpatialUI(lobby);

        if (lobby.Owner.Id == SteamClient.SteamId && currentPhase == GamePhase.Tiebreaker)
        {
            string clicked = lobby.GetMemberData(member, "TiebreakerClicked");
            if (clicked == "true" && !isProcessingResults)
            {
                isProcessingResults = true;
                StartCoroutine(ProcessTiebreakerShot(lobby, member.Id));
            }
        }

        if (lobby.Owner.Id == SteamClient.SteamId)
        {
            string gameState = lobby.GetData("GameState");
            if (gameState == "GameReady" && !isProcessingResults)
                CheckAllTurnsSubmitted(lobby);
        }
    }

    IEnumerator ProcessTiebreakerShot(Lobby lobby, ulong clickerId)
    {
        lobby.SetData("TiebreakerLastClicker", clickerId.ToString());
        yield return new WaitForSeconds(1.0f);

        if (tiebreakerChamber.Count == 0) yield break;

        bool isLive = tiebreakerChamber[0];
        tiebreakerChamber.RemoveAt(0);
        lobby.SetData("TiebreakerChamber", string.Join(",", tiebreakerChamber.Select(b => b ? "1" : "0")));
        lobby.SetData("TiebreakerLastResult", isLive ? "live" : "blank");

        if (isLive)
        {
            tiebreakerParticipants.Remove(clickerId);
            lobby.SetData("TiebreakerParticipants", string.Join(",", tiebreakerParticipants));
            lobby.SetData($"TiebreakerDead_{clickerId}", "true");

            if (tiebreakerParticipants.Count == 1)
            {
                lobby.SetData("WinnerId", tiebreakerParticipants[0].ToString());
                lobby.SetData("GameState", "GameOver");
            }
            else if (tiebreakerParticipants.Count == 0)
            {
                lobby.SetData("GameState", "GameOver");
            }
            else
            {
                isProcessingResults = false;
                lobby.SetData("TiebreakerNextRound", System.DateTime.Now.Ticks.ToString());
            }
        }
        else
        {
            lobby.SetData("WinnerId", clickerId.ToString());
            lobby.SetData("GameState", "GameOver");
        }
    }

    void HandleGameOver(Lobby lobby)
    {
        currentPhase = GamePhase.ResultsShowing;
        isTiebreakerActive = false;

        if (tiebreakerRevolverInstance != null) Destroy(tiebreakerRevolverInstance);
        if (tiebreakerUI != null) Destroy(tiebreakerUI);

        string winnerIdStr = lobby.GetData("WinnerId");
        ulong winnerId = ulong.TryParse(winnerIdStr, out ulong id) ? id : 0;
        bool isWinner = (winnerId == SteamClient.SteamId);

        CreateGameOverUI(isWinner, winnerId, lobby);
    }

    void CreateGameOverUI(bool isWinner, ulong winnerId, Lobby lobby)
    {
        if (hudCanvas == null) return;
        if (deathOverlayPanel != null) Destroy(deathOverlayPanel);

        GameObject overlay = new GameObject("GameOverOverlay");
        overlay.transform.SetParent(hudCanvas.transform, false);
        RectTransform rt = overlay.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var img = overlay.AddComponent<UnityEngine.UI.Image>();
        img.color = isWinner ? new UnityEngine.Color(0.1f, 0.2f, 0.1f, 0.8f) : new UnityEngine.Color(0.2f, 0.1f, 0.1f, 0.8f);
        img.raycastTarget = false;

        GameObject textObj = new GameObject("GameOverText");
        textObj.transform.SetParent(overlay.transform, false);
        RectTransform textRt = textObj.AddComponent<RectTransform>();
        textRt.anchorMin = new Vector2(0.5f, 0.6f);
        textRt.anchorMax = new Vector2(0.5f, 0.6f);
        textRt.sizeDelta = new Vector2(800, 200);

        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        text.text = isWinner ? "VICTORY!" : "DEFEAT";
        text.fontSize = 96;
        text.alignment = TextAlignmentOptions.Center;
        text.color = isWinner ? new UnityEngine.Color(1f, 0.84f, 0f, 1f) : new UnityEngine.Color(0.8f, 0.2f, 0.2f, 1f);

        string winnerName = "Unknown";
        foreach (var m in lobby.Members)
            if (m.Id == winnerId) { winnerName = m.Name; break; }

        GameObject subObj = new GameObject("WinnerName");
        subObj.transform.SetParent(overlay.transform, false);
        RectTransform subRt = subObj.AddComponent<RectTransform>();
        subRt.anchorMin = new Vector2(0.5f, 0.4f);
        subRt.anchorMax = new Vector2(0.5f, 0.4f);
        subRt.sizeDelta = new Vector2(600, 100);

        TextMeshProUGUI subText = subObj.AddComponent<TextMeshProUGUI>();
        subText.text = $"Winner: {winnerName}";
        subText.fontSize = 48;
        subText.alignment = TextAlignmentOptions.Center;
        subText.color = UnityEngine.Color.white;
    }

    // ==================== 사격 애니메이션 시퀀스 ====================
    
    // 테스트용 조준 시퀀스
    IEnumerator TestAim(int seatIdx, int direction)
    {
        // direction: 1=왼쪽, 2=정면, 3=오른쪽
        Vector3 aimPos;
        Vector3 aimRot;
        
        switch (direction)
        {
            case 1:
                aimPos = handPoseAimLeft;
                aimRot = handRotAimLeft;
                break;
            case 3:
                aimPos = handPoseAimRight;
                aimRot = handRotAimRight;
                break;
            default:
                aimPos = handPoseAimFront;
                aimRot = handRotAim;
                break;
        }
        
        // 1. 총 들어올림 + 조준
        yield return StartCoroutine(MoveHandToPosition(seatIdx, aimPos, aimRot, 0.4f));
        
        // 2. 잠시 유지
        yield return new WaitForSeconds(1f);
        
        // 3. 바로 원위치 (가운데 안 거침)
        yield return StartCoroutine(MoveHandToPosition(seatIdx, handPoseTable, handRotTable, 0.4f));
    }

    // 타겟 인덱스에 따른 조준 포즈 반환
    (Vector3 pos, Vector3 rot) GetAimPoseForTarget(int shooterSeatIdx, int targetSeatIdx)
    {
        // 좌석 배치: 0=South, 1=North, 2=West, 3=East
        // 각 좌석에서 다른 좌석의 방향 (1=왼쪽, 2=정면, 3=오른쪽)
        int[,] directionMap = new int[4, 4]
        {
            // shooter 0 (South): target 0,1,2,3
            { 0, 2, 1, 3 },  // North=정면, West=왼쪽, East=오른쪽
            // shooter 1 (North): target 0,1,2,3
            { 2, 0, 3, 1 },  // South=정면, West=오른쪽, East=왼쪽
            // shooter 2 (West): target 0,1,2,3
            { 3, 1, 0, 2 },  // South=오른쪽, North=왼쪽, East=정면
            // shooter 3 (East): target 0,1,2,3
            { 1, 3, 2, 0 },  // South=왼쪽, North=오른쪽, West=정면
        };
        
        int direction = directionMap[shooterSeatIdx, targetSeatIdx];
        
        return direction switch
        {
            1 => (handPoseAimLeft, handRotAimLeft),      // 왼쪽
            2 => (handPoseAimFront, handRotAim),         // 정면
            3 => (handPoseAimRight, handRotAimRight),    // 오른쪽
            _ => (handPoseAimFront, handRotAim)          // 기본 (자기 자신)
        };
    }
    
    // 손 포즈 부드럽게 이동 (특정 좌석)
    IEnumerator MoveHandToPosition(int seatIdx, Vector3 targetPos, Vector3 targetRot, float duration)
    {
        if (seatIdx < 0 || seatIdx >= playerSeats.Length) yield break;
        Transform handTarget = playerSeats[seatIdx].rightHandTarget;
        if (handTarget == null) yield break;
        
        Vector3 startPos = handTarget.localPosition;
        Quaternion startRot = handTarget.localRotation;
        Quaternion endRot = Quaternion.Euler(targetRot);
        
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            
            handTarget.localPosition = Vector3.Lerp(startPos, targetPos, t);
            handTarget.localRotation = Quaternion.Slerp(startRot, endRot, t);
            
            yield return null;
        }
        
        handTarget.localPosition = targetPos;
        handTarget.localRotation = endRot;
    }
    
    // 왼손 포즈 부드럽게 이동 (특정 좌석)
    IEnumerator MoveLeftHandToPosition(int seatIdx, Vector3 targetPos, Vector3 targetRot, float duration)
    {
        if (seatIdx < 0 || seatIdx >= playerSeats.Length) yield break;
        Transform handTarget = playerSeats[seatIdx].leftHandTarget;
        if (handTarget == null) yield break;
        
        Vector3 startPos = handTarget.localPosition;
        Quaternion startRot = handTarget.localRotation;
        Quaternion endRot = Quaternion.Euler(targetRot);
        
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            
            handTarget.localPosition = Vector3.Lerp(startPos, targetPos, t);
            handTarget.localRotation = Quaternion.Slerp(startRot, endRot, t);
            
            yield return null;
        }
        
        handTarget.localPosition = targetPos;
        handTarget.localRotation = endRot;
    }
    
    // 양손 동시에 이동 (특정 좌석)
    IEnumerator MoveBothHandsToPosition(int seatIdx, Vector3 rightPos, Vector3 rightRot, Vector3 leftPos, Vector3 leftRot, float duration)
    {
        Coroutine rightHand = StartCoroutine(MoveHandToPosition(seatIdx, rightPos, rightRot, duration));
        Coroutine leftHand = StartCoroutine(MoveLeftHandToPosition(seatIdx, leftPos, leftRot, duration));
        
        yield return rightHand;
        yield return leftHand;
    }
    
    // HeadAim Weight 조절 (특정 좌석)
    IEnumerator SetHeadAimWeight(int seatIdx, float targetWeight, float duration)
    {
        if (seatIdx < 0 || seatIdx >= playerSeats.Length) yield break;
        MultiAimConstraint constraint = playerSeats[seatIdx].headAimConstraint;
        if (constraint == null) yield break;
        
        float startWeight = constraint.weight;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            constraint.weight = Mathf.Lerp(startWeight, targetWeight, t);
            yield return null;
        }
        
        constraint.weight = targetWeight;
    }
    
    // HeadTarget을 타겟 플레이어의 머리 위치로 이동 (특정 좌석)
    void SetHeadTargetToPlayer(int seatIdx, int targetSeatIdx)
    {
        if (seatIdx < 0 || seatIdx >= playerSeats.Length) return;
        if (targetSeatIdx < 0 || targetSeatIdx >= playerSeats.Length) return;
        
        Transform headTarget = playerSeats[seatIdx].headTarget;
        if (headTarget == null) return;
        
        // 타겟의 head 본 찾기
        GameObject targetChar = playerSeats[targetSeatIdx].character;
        if (targetChar == null) return;
        
        Transform targetHead = FindBoneRecursive(targetChar.transform, "head");
        if (targetHead != null)
        {
            headTarget.position = targetHead.position;
        }
    }
    
    // 사격 시퀀스 (특정 좌석의 플레이어가 타겟을 쏨)
    IEnumerator PlayShootSequence(int shooterSeatIdx, int targetSeatIdx)
    {
        // 1. HeadAim 활성화 + 타겟 설정
        SetHeadTargetToPlayer(shooterSeatIdx, targetSeatIdx);
        StartCoroutine(SetHeadAimWeight(shooterSeatIdx, 1f, 0.3f));
        
        // 2. 타겟 방향으로 조준
        var (aimPos, aimRot) = GetAimPoseForTarget(shooterSeatIdx, targetSeatIdx);
        yield return StartCoroutine(MoveHandToPosition(shooterSeatIdx, aimPos, aimRot, 0.4f));
        
        // 3. 긴장감 일시정지
        yield return new WaitForSeconds(0.5f);
        
        // 4. 발사! (반동 - 살짝 위로)
        Vector3 recoilPos = aimPos + new Vector3(0f, 0.05f, -0.02f);
        yield return StartCoroutine(MoveHandToPosition(shooterSeatIdx, recoilPos, aimRot, 0.05f));
        yield return StartCoroutine(MoveHandToPosition(shooterSeatIdx, aimPos, aimRot, 0.1f));
        
        // 5. 잠시 유지
        yield return new WaitForSeconds(0.3f);
        
        // 6. 바로 원위치 (가운데 안 거침)
        yield return StartCoroutine(MoveHandToPosition(shooterSeatIdx, handPoseTable, handRotTable, 0.4f));
        
        // 7. HeadAim 비활성화
        StartCoroutine(SetHeadAimWeight(shooterSeatIdx, 0f, 0.3f));
    }
    
    // 재장전 시퀀스 (특정 좌석)
    IEnumerator PlayReloadSequence(int seatIdx)
    {
        // 1. 양손을 몸쪽으로 당기면서 위로 올림
        Vector3 rightPullBack = new Vector3(handPoseTable.x, handPoseTable.y + 0.2f, handPoseTable.z - 0.5f);
        Vector3 leftPullBack = new Vector3(leftHandPoseTable.x, leftHandPoseTable.y + 0.2f, leftHandPoseTable.z - 0.5f);
        yield return StartCoroutine(MoveBothHandsToPosition(seatIdx, rightPullBack, handRotTable, leftPullBack, leftHandRotTable, 0.4f));
        
        // 2. 양손을 가운데 아래로 모음
        Vector3 centerPos = new Vector3(0f, 0.5f, 0.1f);
        Vector3 rightCenter = centerPos + new Vector3(0.1f, 0f, 0f);
        Vector3 leftCenter = centerPos + new Vector3(-0.1f, 0f, 0f);
        yield return StartCoroutine(MoveBothHandsToPosition(seatIdx, rightCenter, handRotTable, leftCenter, leftHandRotTable, 0.4f));
        
        // 3. 주섬주섬 (양손 같이 위아래로)
        Vector3 rightUp = rightCenter + new Vector3(0f, 0.05f, 0f);
        Vector3 leftUp = leftCenter + new Vector3(0f, 0.05f, 0f);
        Vector3 rightDown = rightCenter + new Vector3(0f, -0.05f, 0f);
        Vector3 leftDown = leftCenter + new Vector3(0f, -0.05f, 0f);
        
        yield return StartCoroutine(MoveBothHandsToPosition(seatIdx, rightUp, handRotTable, leftUp, leftHandRotTable, 0.2f));
        yield return StartCoroutine(MoveBothHandsToPosition(seatIdx, rightDown, handRotTable, leftDown, leftHandRotTable, 0.2f));
        yield return StartCoroutine(MoveBothHandsToPosition(seatIdx, rightCenter, handRotTable, leftCenter, leftHandRotTable, 0.2f));
        
        // 4. 양손 다시 올림
        yield return StartCoroutine(MoveBothHandsToPosition(seatIdx, rightPullBack, handRotTable, leftPullBack, leftHandRotTable, 0.4f));
        
        // 5. 양손 원위치
        yield return StartCoroutine(MoveBothHandsToPosition(seatIdx, handPoseTable, handRotTable, leftHandPoseTable, leftHandRotTable, 0.5f));
    }

    // ==================== 피격 효과 ====================
    
    // 화면 빨개지기 (본인 피격 시)
    IEnumerator PlayHitFlash()
    {
        if (hitFlashImage == null) yield break;
        
        // 빨간색으로 페이드 인
        hitFlashImage.gameObject.SetActive(true);
        UnityEngine.Color flashColor = new UnityEngine.Color(1f, 0f, 0f, 0f);
        hitFlashImage.color = flashColor;
        
        float fadeInDuration = 0.1f;
        float elapsed = 0f;
        
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(0f, 0.5f, elapsed / fadeInDuration);
            hitFlashImage.color = new UnityEngine.Color(1f, 0f, 0f, alpha);
            yield return null;
        }
        
        // 페이드 아웃
        float fadeOutDuration = 0.4f;
        elapsed = 0f;
        
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(0.5f, 0f, elapsed / fadeOutDuration);
            hitFlashImage.color = new UnityEngine.Color(1f, 0f, 0f, alpha);
            yield return null;
        }
        
        hitFlashImage.gameObject.SetActive(false);
    }
    
    // 캐릭터 피격 (팔 움츠러들기 + 몸 밀리기)
    IEnumerator PlayHitFlinch(int seatIdx)
    {
        if (seatIdx < 0 || seatIdx >= playerSeats.Length) yield break;
        
        Transform rightHand = playerSeats[seatIdx].rightHandTarget;
        Transform leftHand = playerSeats[seatIdx].leftHandTarget;
        GameObject character = playerSeats[seatIdx].character;
        
        if (rightHand == null && leftHand == null && character == null) yield break;
        
        // 현재 위치 저장
        Vector3 rightOriginal = rightHand != null ? rightHand.localPosition : Vector3.zero;
        Vector3 leftOriginal = leftHand != null ? leftHand.localPosition : Vector3.zero;
        Vector3 bodyOriginal = character != null ? character.transform.localPosition : Vector3.zero;
        
        // 움츠러든 위치 (몸쪽으로 + 살짝 위로)
        Vector3 rightFlinch = rightOriginal + new Vector3(0f, 0.1f, -0.15f);
        Vector3 leftFlinch = leftOriginal + new Vector3(0f, 0.1f, -0.15f);
        // 몸은 뒤로 절반만 (0.05)
        Vector3 bodyFlinch = character != null ? bodyOriginal - character.transform.forward * 0.05f : Vector3.zero;
        
        // 빠르게 움츠러들기
        float flinchDuration = 0.08f;
        float elapsed = 0f;
        
        while (elapsed < flinchDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / flinchDuration;
            
            if (rightHand != null)
                rightHand.localPosition = Vector3.Lerp(rightOriginal, rightFlinch, t);
            if (leftHand != null)
                leftHand.localPosition = Vector3.Lerp(leftOriginal, leftFlinch, t);
            if (character != null)
                character.transform.localPosition = Vector3.Lerp(bodyOriginal, bodyFlinch, t);
            
            yield return null;
        }
        
        // 천천히 원위치
        float returnDuration = 0.3f;
        elapsed = 0f;
        
        while (elapsed < returnDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / returnDuration);
            
            if (rightHand != null)
                rightHand.localPosition = Vector3.Lerp(rightFlinch, rightOriginal, t);
            if (leftHand != null)
                leftHand.localPosition = Vector3.Lerp(leftFlinch, leftOriginal, t);
            if (character != null)
                character.transform.localPosition = Vector3.Lerp(bodyFlinch, bodyOriginal, t);
            
            yield return null;
        }
        
        if (rightHand != null) rightHand.localPosition = rightOriginal;
        if (leftHand != null) leftHand.localPosition = leftOriginal;
        if (character != null) character.transform.localPosition = bodyOriginal;
    }
    
    // 피격 연출 통합 (특정 좌석)
    IEnumerator PlayHitEffect(int seatIdx)
    {
        // 본인이면 화면 빨개지기
        if (seatIdx == mySeatIndex)
        {
            StartCoroutine(PlayHitFlash());
        }
        
        // 캐릭터 움찔
        yield return StartCoroutine(PlayHitFlinch(seatIdx));
    }

    // ==================== 스킬 연출 ====================
    
    // 직업카드 점멸
    IEnumerator PlayJobCardBlink(float duration = 1.5f)
    {
        if (jobCardUI == null) yield break;
        
        float elapsed = 0f;
        float blinkInterval = 0.15f;
        bool isVisible = true;
        float lastBlink = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            
            if (elapsed - lastBlink >= blinkInterval)
            {
                isVisible = !isVisible;
                jobCardUI.SetActive(isVisible);
                lastBlink = elapsed;
            }
            
            yield return null;
        }
        
        jobCardUI.SetActive(true);
    }
    
    // 봉인 오버레이 켜기
    IEnumerator ShowBlockOverlay()
    {
        if (blockOverlayImage == null) yield break;
        
        blockOverlayImage.gameObject.SetActive(true);
        UnityEngine.Color overlayColor = new UnityEngine.Color(0f, 0f, 0f, 0f);
        blockOverlayImage.color = overlayColor;
        
        float fadeDuration = 0.3f;
        float elapsed = 0f;
        
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(0f, 0.6f, elapsed / fadeDuration);
            blockOverlayImage.color = new UnityEngine.Color(0f, 0f, 0f, alpha);
            yield return null;
        }
        
        blockOverlayImage.color = new UnityEngine.Color(0f, 0f, 0f, 0.6f);
    }
    
    // 봉인 오버레이 끄기
    IEnumerator HideBlockOverlay()
    {
        if (blockOverlayImage == null) yield break;
        
        float fadeDuration = 0.3f;
        float elapsed = 0f;
        float startAlpha = blockOverlayImage.color.a;
        
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(startAlpha, 0f, elapsed / fadeDuration);
            blockOverlayImage.color = new UnityEngine.Color(0f, 0f, 0f, alpha);
            yield return null;
        }
        
        blockOverlayImage.gameObject.SetActive(false);
    }

    // ==================== 페이즈 처리 ====================
    
    void HandlePhaseChange(Lobby lobby, string phase)
    {
        switch (phase)
        {
            case "Skill":
                StartCoroutine(HandleSkillPhase(lobby));
                break;
            case "Reload":
                StartCoroutine(HandleReloadPhase(lobby));
                break;
            case "Fire":
                StartCoroutine(HandleFirePhase(lobby));
                break;
            case "Result":
                StartCoroutine(HideBlockOverlay());
                break;
        }
    }
    
    IEnumerator HandleSkillPhase(Lobby lobby)
    {
        var members = lobby.Members.ToList();
        
        // 내가 봉인당했는지 확인
        string blockedStr = lobby.GetData($"Blocked_{SteamClient.SteamId}");
        if (blockedStr == "true")
        {
            StartCoroutine(ShowBlockOverlay());
        }
        
        // 내가 스킬 사용했으면 직업카드 점멸
        string useSkillStr = lobby.GetData($"Turn_UseSkill_{SteamClient.SteamId}");
        if (useSkillStr == "True" || useSkillStr == "true")
        {
            StartCoroutine(PlayJobCardBlink(1.5f));
        }
        
        yield return null;
    }
    
    IEnumerator HandleReloadPhase(Lobby lobby)
    {
        var members = lobby.Members.ToList();
        
        // 봉인당했으면 재장전 안 함
        string blockedStr = lobby.GetData($"Blocked_{SteamClient.SteamId}");
        if (blockedStr == "true") yield break;
        
        // 테스트 모드: 모든 좌석에서 재장전 체크
        int totalSeats = isTestMode ? 4 : members.Count;
        
        for (int i = 0; i < totalSeats && i < playerSeats.Length; i++)
        {
            ulong memberId;
            bool isFake = false;
            
            if (i < members.Count)
            {
                memberId = members[i].Id;
            }
            else if (isTestMode && (i - members.Count) < fakePlayerIds.Count)
            {
                memberId = fakePlayerIds[i - members.Count];
                isFake = true;
            }
            else
            {
                continue;
            }
            
            // 사망 체크
            if (!isFake)
            {
                string deadStr = lobby.GetMemberData(members[i], "IsDead");
                if (deadStr == "true" || deadStr == "True") continue;
            }
            else
            {
                string deadStr = lobby.GetData($"IsDead_{memberId}");
                if (deadStr == "true" || deadStr == "True") continue;
            }
            
            string reloadStr = lobby.GetData($"Turn_Reload_{memberId}");
            string useSkillStr = lobby.GetData($"Turn_UseSkill_{memberId}");
            bool isReloading = reloadStr == "True" || reloadStr == "true";
            bool usedSkill = useSkillStr == "True" || useSkillStr == "true";
            
            // 스피드슈터는 발포 페이즈에서 처리
            string jobStr = lobby.GetData($"Job_{memberId}");
            bool isSpeedShooter = usedSkill && int.TryParse(jobStr, out int jobIdx) && (JobType)jobIdx == JobType.SpeedShooter;
            
            string memberBlockedStr = lobby.GetData($"Blocked_{memberId}");
            bool isBlocked = memberBlockedStr == "true";
            
            if (isReloading && !isSpeedShooter && !isBlocked)
            {
                StartCoroutine(PlayReloadSequence(i));
            }
        }
        
        yield return null;
    }
    
    IEnumerator HandleFirePhase(Lobby lobby)
    {
        var members = lobby.Members.ToList();
        
        // 잠시 대기 후 발포 (조준 시간)
        yield return new WaitForSeconds(0.5f);
        
        // 발포한 사람들의 조준 + 발사 애니메이션
        List<(int shooter, int target)> shootList = new List<(int, int)>();
        
        // 테스트 모드: 모든 좌석에서 발포 체크
        int totalSeats = isTestMode ? 4 : members.Count;
        
        for (int i = 0; i < totalSeats && i < playerSeats.Length; i++)
        {
            ulong memberId;
            bool isFake = false;
            
            if (i < members.Count)
            {
                memberId = members[i].Id;
            }
            else if (isTestMode && (i - members.Count) < fakePlayerIds.Count)
            {
                memberId = fakePlayerIds[i - members.Count];
                isFake = true;
            }
            else
            {
                continue;
            }
            
            // 사망 체크
            if (!isFake)
            {
                string deadStr = lobby.GetMemberData(members[i], "IsDead");
                if (deadStr == "true" || deadStr == "True") continue;
            }
            else
            {
                string deadStr = lobby.GetData($"IsDead_{memberId}");
                if (deadStr == "true" || deadStr == "True") continue;
            }
            
            string memberBlockedStr = lobby.GetData($"Blocked_{memberId}");
            if (memberBlockedStr == "true") continue;
            
            string targetStr = lobby.GetData($"Turn_Target_{memberId}");
            if (!string.IsNullOrEmpty(targetStr) && int.TryParse(targetStr, out int targetIdx) && targetIdx != -1)
            {
                shootList.Add((i, targetIdx));
            }
        }
        
        // 모든 사람 동시에 조준
        foreach (var (shooter, target) in shootList)
        {
            StartCoroutine(PlayShootSequence(shooter, target));
        }
        
        // 발사 시점에 피격 (조준 0.4초 + 긴장감 0.5초 = 0.9초)
        yield return new WaitForSeconds(0.9f);
        
        foreach (var (shooter, target) in shootList)
        {
            if (target < playerSeats.Length)
            {
                StartCoroutine(PlayHitEffect(target));
            }
        }
        
        yield return null;
    }

    // ==================== 캐릭터 초기화 및 커스터마이즈 ====================
    
    void InitializeAllCharacters(Lobby lobby)
    {
        var members = lobby.Members.ToList();
        int totalSeats = isTestMode ? 4 : members.Count;
        
        for (int seatIdx = 0; seatIdx < totalSeats && seatIdx < playerSeats.Length; seatIdx++)
        {
            if (seatIdx < members.Count)
            {
                InitializeCharacterAtSeat(seatIdx, members[seatIdx], lobby);
            }
            else if (isTestMode)
            {
                // 가짜 플레이어 캐릭터 초기화
                InitializeFakeCharacterAtSeat(seatIdx);
            }
        }
    }
    
    void InitializeFakeCharacterAtSeat(int seatIdx)
    {
        // 캐릭터 찾기
        Transform target = playerSeats[seatIdx].seatRoot.Find("Target");
        if (target == null) target = playerSeats[seatIdx].seatRoot.Find("Target 1");
        if (target == null) return;
        
        Transform character = target.Find("urp for me");
        if (character == null) return;
        
        playerSeats[seatIdx].character = character.gameObject;
        
        // HeadAim weight 초기화 (0으로)
        if (playerSeats[seatIdx].headAimConstraint != null)
        {
            playerSeats[seatIdx].headAimConstraint.weight = 0f;
        }
        
        // CharacterCustomizer 찾기 또는 추가
        CharacterCustomizer customizer = character.GetComponent<CharacterCustomizer>();
        if (customizer == null)
        {
            customizer = character.gameObject.AddComponent<CharacterCustomizer>();
        }
        playerSeats[seatIdx].customizer = customizer;
        
        // 랜덤 커스터마이즈 적용
        CharacterCustomData randomData = new CharacterCustomData
        {
            facePresetIndex = UnityEngine.Random.Range(0, 5),
            hairIndex = UnityEngine.Random.Range(0, 8),
            hairColorIndex = UnityEngine.Random.Range(0, 5),
            glassesIndex = UnityEngine.Random.Range(-1, 2),
            beltType = UnityEngine.Random.Range(0, 2),
            jacketType = UnityEngine.Random.Range(0, 3),
            jacketColorIndex = UnityEngine.Random.Range(0, 10),
            shirtColorIndex = UnityEngine.Random.Range(0, 8),
            tieType = UnityEngine.Random.Range(0, 3),
            tieColorIndex = UnityEngine.Random.Range(0, 10),
            waistcoatType = UnityEngine.Random.Range(0, 2),
            waistcoatColorIndex = UnityEngine.Random.Range(0, 10),
            pantsColorIndex = UnityEngine.Random.Range(0, 10),
            shoesColorIndex = UnityEngine.Random.Range(0, 4)
        };
        customizer.ApplyCustomization(randomData);
    }
    
    void InitializeCharacterAtSeat(int seatIdx, Friend member, Lobby lobby)
    {
        // 캐릭터 찾기
        Transform target = playerSeats[seatIdx].seatRoot.Find("Target");
        if (target == null) target = playerSeats[seatIdx].seatRoot.Find("Target 1");
        if (target == null) return;
        
        Transform character = target.Find("urp for me");
        if (character == null) return;
        
        playerSeats[seatIdx].character = character.gameObject;
        
        // HeadAim weight 초기화 (0으로)
        if (playerSeats[seatIdx].headAimConstraint != null)
        {
            playerSeats[seatIdx].headAimConstraint.weight = 0f;
        }
        
        // CharacterCustomizer 찾기 또는 추가
        CharacterCustomizer customizer = character.GetComponent<CharacterCustomizer>();
        if (customizer == null)
        {
            customizer = character.gameObject.AddComponent<CharacterCustomizer>();
        }
        playerSeats[seatIdx].customizer = customizer;
        
        // 커스터마이즈 데이터 적용
        ApplyCustomizationToSeat(seatIdx, member, lobby);
    }
    
    void ApplyCustomizationToSeat(int seatIdx, Friend member, Lobby lobby)
    {
        if (playerSeats[seatIdx].customizer == null) return;
        
        // 멤버 데이터에서 커스터마이즈 정보 가져오기
        string customDataStr = lobby.GetMemberData(member, "CharacterCustom");
        
        if (!string.IsNullOrEmpty(customDataStr))
        {
            CharacterCustomData customData = CharacterCustomData.Deserialize(customDataStr);
            if (customData != null)
            {
                playerSeats[seatIdx].customizer.ApplyCustomization(customData);
                Debug.Log($"<color=cyan>{member.Name} 커스터마이즈 적용 완료</color>");
            }
        }
        else
        {
            Debug.Log($"<color=yellow>{member.Name} 커스터마이즈 데이터 없음</color>");
        }
    }
}
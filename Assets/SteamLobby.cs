using UnityEngine;
using Steamworks;
using Steamworks.Data;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.SceneManagement;

public class SteamLobby : MonoBehaviour
{
    [Header("Lobby UI Elements")]
    public GameObject[] FixedPlayerSlots; 
    public GameObject StartButton;        
    public GameObject ReadyButton;

    [Header("Weapon Select UI")]
    public GameObject WeaponSelectPanel;
    public GameObject WeaponConfirmButton;
    public GameObject WeaponWaitingUI;

    [Header("Job UI (무기 선택 화면)")]
    public GameObject JobCardUI;        // 왼쪽 아래 직업 카드
    public TextMeshProUGUI JobNameText; // 직업 이름
    public TextMeshProUGUI JobDescText; // 직업 설명 (호버/클릭 시)

    [Header("Settings")]
    public float SceneTransitionTimeout = 10f;
    public CharacterCustomData MyCustomData = new CharacterCustomData();
    public Lobby? CurrentLobby;
    public static SteamLobby Instance;

    private float sceneTransitionTimeout = 10f;
    private bool isTransitioning = false;
    private Coroutine transitionCoroutine;

    // 무기 선택 관련
    private int selectedWeaponID = -1;
    private bool isWeaponConfirmed = false;

    // 직업 관련
    public Job MyJob { get; private set; }

    // 이벤트
    public System.Action OnLobbyJoined;
    public System.Action OnLobbyLeft;
    public System.Action OnWeaponSelectPhase;

    void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); }
    }

    void Start()
    {
        if (!SteamClient.IsValid) return;

        SteamMatchmaking.OnLobbyEntered += OnLobbyEntered;
        SteamMatchmaking.OnLobbyMemberJoined += (lobby, friend) => RefreshLobbyMembers();
        SteamMatchmaking.OnLobbyMemberDisconnected += (lobby, friend) => RefreshLobbyMembers();
        SteamMatchmaking.OnLobbyMemberLeave += (lobby, friend) => RefreshLobbyMembers();
        SteamMatchmaking.OnLobbyMemberDataChanged += (lobby, friend) => OnMemberDataChanged(lobby, friend);
        SteamMatchmaking.OnLobbyDataChanged += OnLobbyDataUpdate;
        SteamFriends.OnGameLobbyJoinRequested += OnLobbyJoinRequested;
        
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SteamMatchmaking.OnLobbyEntered -= OnLobbyEntered;
        SteamMatchmaking.OnLobbyDataChanged -= OnLobbyDataUpdate;
        SteamFriends.OnGameLobbyJoinRequested -= OnLobbyJoinRequested;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private async void OnLobbyJoinRequested(Lobby lobby, SteamId id)
    {
        await lobby.Join();
    }

    public async void CreateLobby()
    {
        var lobby = await SteamMatchmaking.CreateLobbyAsync(4);
        if (lobby.HasValue)
        {
            CurrentLobby = lobby.Value;
            CurrentLobby.Value.SetPublic();
            CurrentLobby.Value.SetJoinable(true);
            CurrentLobby.Value.SetData("GameState", "Lobby");
            CurrentLobby.Value.SetMemberData("status", "ready");
            CurrentLobby.Value.SetMemberData("loaded", "false");
            CurrentLobby.Value.SetMemberData("WeaponReady", "false");
        }
    }

    private void OnLobbyEntered(Lobby lobby)
    {
        CurrentLobby = lobby;
        isTransitioning = false;
        isWeaponConfirmed = false;
        selectedWeaponID = -1;
        MyJob = null;

        bool isOwner = lobby.Owner.Id == SteamClient.SteamId;
        lobby.SetMemberData("status", isOwner ? "ready" : "not_ready");
        lobby.SetMemberData("loaded", "false");
        lobby.SetMemberData("WeaponReady", "false");

        RefreshLobbyMembers();
        
        OnLobbyJoined?.Invoke();

        string gameState = lobby.GetData("GameState");
        if (gameState == "Starting" || gameState == "InGame")
        {
            StartCoroutine(HandleLateJoin());
        }
    }

    private void OnLobbyDataUpdate(Lobby lobby)
    {
        string gameState = lobby.GetData("GameState");
        
        if (gameState == "WeaponSelecting")
        {
            // 무기 선택 페이즈 진입
            OnWeaponSelectPhase?.Invoke();
            
            // 모든 클라이언트가 자기 직업 로드
            LoadMyJob(lobby);
            
            ShowWeaponSelectUI();
        }
        else if (gameState == "Starting" && !isTransitioning)
        {
            isTransitioning = true;
            StartCoroutine(LoadGameSceneAsync());
        }
        else if (gameState == "InGame" && SceneManager.GetActiveScene().name != "GameScene")
        {
            SceneManager.LoadScene("GameScene");
        }
    }

    private void OnMemberDataChanged(Lobby lobby, Friend friend)
    {
        RefreshLobbyMembers();
        
        if (lobby.Owner.Id == SteamClient.SteamId)
        {
            string gameState = lobby.GetData("GameState");
            if (gameState == "WeaponSelecting")
            {
                CheckAllWeaponsSelectedAndStart(lobby);
            }
        }
        
        if (isTransitioning && lobby.Owner.Id == SteamClient.SteamId)
        {
            CheckAllPlayersLoaded();
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "GameScene" && CurrentLobby.HasValue)
        {
            CurrentLobby.Value.SetMemberData("loaded", "true");
            HideWeaponSelectUI();
        }
    }

    // ==================== 직업 배정 관련 ====================

    void AssignJobsToAllPlayers(Lobby lobby)
    {
        var members = lobby.Members.ToList();
        
        var jobTypes = System.Enum.GetValues(typeof(JobType)).Cast<JobType>().ToList();
        
        for (int i = 0; i < jobTypes.Count; i++)
        {
            int rand = UnityEngine.Random.Range(i, jobTypes.Count);
            var temp = jobTypes[i];
            jobTypes[i] = jobTypes[rand];
            jobTypes[rand] = temp;
        }
        
        for (int i = 0; i < members.Count; i++)
        {
            lobby.SetData($"Job_{members[i].Id}", ((int)jobTypes[i]).ToString());
            Debug.Log($"<color=yellow>직업 배정: {members[i].Name} -> {jobTypes[i]}</color>");
        }
    }

    void LoadMyJob(Lobby lobby)
    {
        string jobStr = lobby.GetData($"Job_{SteamClient.SteamId}");
        
        if (int.TryParse(jobStr, out int jobIndex))
        {
            MyJob = Job.Create((JobType)jobIndex);
            Debug.Log($"<color=green>내 직업: {MyJob.Name} - {MyJob.Description}</color>");
            
            UpdateJobUI();
        }
        else
        {
            Debug.LogWarning("직업 데이터를 찾을 수 없음");
        }
    }

    void UpdateJobUI()
    {
        if (MyJob == null) return;
        
        if (JobCardUI != null) JobCardUI.SetActive(true);
        if (JobNameText != null) JobNameText.text = MyJob.Name;
        if (JobDescText != null) JobDescText.text = MyJob.Description;
    }

    // ==================== 무기 선택 관련 ====================

    void ShowWeaponSelectUI()
    {
        if (WeaponSelectPanel != null) WeaponSelectPanel.SetActive(true);
        if (WeaponConfirmButton != null) WeaponConfirmButton.SetActive(false);
        if (WeaponWaitingUI != null) WeaponWaitingUI.SetActive(false);
        
        isWeaponConfirmed = false;
        selectedWeaponID = -1;
    }

    void HideWeaponSelectUI()
    {
        if (WeaponSelectPanel != null) WeaponSelectPanel.SetActive(false);
        if (WeaponConfirmButton != null) WeaponConfirmButton.SetActive(false);
        if (WeaponWaitingUI != null) WeaponWaitingUI.SetActive(false);
        if (JobCardUI != null) JobCardUI.SetActive(false);
    }

    public void OnWeaponCardClick(int weaponID)
    {
        if (isWeaponConfirmed) return;
        
        selectedWeaponID = weaponID;
        if (WeaponConfirmButton != null)
        {
            WeaponConfirmButton.SetActive(true);
        }
        Debug.Log($"<color=cyan>무기 선택: {weaponID}</color>");
    }

    public void OnWeaponConfirmClick()
    {
        if (selectedWeaponID == -1 || isWeaponConfirmed) return;
        if (!CurrentLobby.HasValue) return;

        isWeaponConfirmed = true;
        
        if (WeaponSelectPanel != null) WeaponSelectPanel.SetActive(false);
        if (WeaponConfirmButton != null) WeaponConfirmButton.SetActive(false);
        if (WeaponWaitingUI != null) WeaponWaitingUI.SetActive(true);

        var lobby = CurrentLobby.Value;
        lobby.SetMemberData("SelectedWeapon", selectedWeaponID.ToString());
        lobby.SetMemberData("WeaponReady", "true");

        Debug.Log($"<color=green>무기 확정: {selectedWeaponID}</color>");

        if (lobby.Owner.Id == SteamClient.SteamId)
        {
            CheckAllWeaponsSelectedAndStart(lobby);
        }
    }

    void CheckAllWeaponsSelectedAndStart(Lobby lobby)
    {
        if (lobby.GetData("GameState") != "WeaponSelecting") return;

        #if UNITY_EDITOR
        int memberCount = lobby.Members.Count();
        if (memberCount <= 1 && isWeaponConfirmed)
        {
            Debug.Log("<color=yellow>[디버그] 혼자 테스트 - 무기 선택 완료, 게임 시작</color>");
            StartGameTransition();
            return;
        }
        #endif

        bool allReady = true;
        foreach (var m in lobby.Members)
        {
            string ready = lobby.GetMemberData(m, "WeaponReady");
            if (ready != "true")
            {
                allReady = false;
                break;
            }
        }

        if (allReady)
        {
            Debug.Log("<color=green>모든 플레이어 무기 선택 완료. 게임 시작!</color>");
            StartGameTransition();
        }
    }

    void StartGameTransition()
    {
        if (!CurrentLobby.HasValue) return;
        if (isTransitioning) return;
        
        isTransitioning = true;
        CurrentLobby.Value.SetData("GameState", "Starting");
        
        if (transitionCoroutine != null) StopCoroutine(transitionCoroutine);
        transitionCoroutine = StartCoroutine(SceneTransitionWithTimeout());
    }

    // ==================== 로비 관련 ====================

    bool AreAllMembersReady()
    {
        if (!CurrentLobby.HasValue) return false;
        var lobby = CurrentLobby.Value;

        foreach (var member in lobby.Members)
        {
            if (member.Id == lobby.Owner.Id) continue;
            if (lobby.GetMemberData(member, "status") != "ready")
                return false;
        }
        return true;
    }

    bool AreAllMembersLoaded()
    {
        if (!CurrentLobby.HasValue) return false;
        var lobby = CurrentLobby.Value;

        foreach (var member in lobby.Members)
        {
            if (lobby.GetMemberData(member, "loaded") != "true")
                return false;
        }
        return true;
    }

    void CheckAllPlayersLoaded()
    {
        if (!CurrentLobby.HasValue) return;
        
        if (AreAllMembersLoaded())
        {
            Debug.Log("[SteamLobby] 모든 플레이어 로드 완료, 게임 시작");
            CurrentLobby.Value.SetData("GameState", "DistributingItems");
        }
    }

    public void RefreshLobbyMembers()
    {
        if (!CurrentLobby.HasValue) return;
        if (FixedPlayerSlots == null || FixedPlayerSlots.Length == 0) return;
        
        var lobby = CurrentLobby.Value;
        var members = lobby.Members.ToList();

        bool isOwner = lobby.Owner.Id == SteamClient.SteamId;

        if (ReadyButton != null) ReadyButton.SetActive(!isOwner);
        
        if (StartButton != null)
        {
            bool allReady = AreAllMembersReady();
            #if UNITY_EDITOR
            StartButton.SetActive(isOwner && (members.Count < 2 || allReady)); 
            #else
            StartButton.SetActive(isOwner && members.Count >= 2 && allReady);
            #endif
        }

        for (int i = 0; i < FixedPlayerSlots.Length; i++)
        {
            var texts = FixedPlayerSlots[i].GetComponentsInChildren<TextMeshProUGUI>();
            var avatarLoader = FixedPlayerSlots[i].GetComponentInChildren<SteamAvatarLoader>();
            
            if (texts.Length < 2) continue;

            if (i < members.Count)
            {
                Friend member = members[i];
                texts[0].text = member.Name + (member.Id == SteamClient.SteamId ? " (나)" : "");
                
                if (member.Id == lobby.Owner.Id) {
                    texts[1].text = "<color=yellow>방장 (준비완료)</color>";
                } else {
                    string status = lobby.GetMemberData(member, "status");
                    texts[1].text = (status == "ready") ? "<color=green>준비 완료</color>" : "<color=red>준비 중</color>";
                }
                
                // 스팀 아바타 로드
                if (avatarLoader != null)
                {
                    avatarLoader.avatarImage.gameObject.SetActive(true);
                    avatarLoader.LoadAvatar(member.Id);
                }
            }
            else {
                texts[0].text = "대기 중...";
                texts[1].text = "";
                
                // 빈 자리는 아바타 숨기기
                if (avatarLoader != null)
                {
                    avatarLoader.avatarImage.gameObject.SetActive(false);
                }
            }
        }
    }

    public void StartGame()
    {
        if (!CurrentLobby.HasValue) return;
        var lobby = CurrentLobby.Value;
        
        if (lobby.Owner.Id == SteamClient.SteamId)
        {
            Debug.Log("[SteamLobby] 직업 배정 시작");
            AssignJobsToAllPlayers(lobby);
        }
        
        Debug.Log("[SteamLobby] 무기 선택 페이즈 시작");
        lobby.SetData("GameState", "WeaponSelecting");
    }

    IEnumerator LoadGameSceneAsync()
    {
        Debug.Log("[SteamLobby] 씬 로드 시작");
        
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("GameScene");
        asyncLoad.allowSceneActivation = false;

        while (asyncLoad.progress < 0.9f)
        {
            yield return null;
        }

        Debug.Log("[SteamLobby] 씬 로드 완료, 활성화 대기");
        
        if (CurrentLobby.HasValue)
        {
            CurrentLobby.Value.SetMemberData("loaded", "true");
        }

        yield return new WaitForSeconds(0.3f);
        asyncLoad.allowSceneActivation = true;
    }

    IEnumerator SceneTransitionWithTimeout()
    {
        StartCoroutine(LoadGameSceneAsync());

        float elapsed = 0f;
        while (elapsed < sceneTransitionTimeout)
        {
            if (AreAllMembersLoaded())
            {
                CurrentLobby.Value.SetData("GameState", "DistributingItems");
                yield break;
            }
            elapsed += 0.5f;
            yield return new WaitForSeconds(0.5f);
        }

        Debug.LogWarning("[SteamLobby] 타임아웃 - 강제 게임 시작");
        CurrentLobby.Value.SetData("GameState", "DistributingItems");
    }

    IEnumerator HandleLateJoin()
    {
        yield return new WaitForSeconds(1f);
        
        if (CurrentLobby.HasValue)
        {
            string gameState = CurrentLobby.Value.GetData("GameState");
            if (gameState == "Starting" || gameState == "InGame")
            {
                SceneManager.LoadScene("GameScene");
            }
        }
    }

    public void SaveCustomization()
    {
        PlayerPrefs.SetString("MyCharacterCustom", MyCustomData.Serialize());
        PlayerPrefs.Save();
        
        if (CurrentLobby.HasValue)
        {
            CurrentLobby.Value.SetMemberData("CharacterCustom", MyCustomData.Serialize());
        }
    }

    public void LoadCustomization()
    {
        string saved = PlayerPrefs.GetString("MyCharacterCustom", "");
        MyCustomData = CharacterCustomData.Deserialize(saved);
    }

    public void ToggleReady()
    {
        if (!CurrentLobby.HasValue) return;
        var lobby = CurrentLobby.Value;

        if (lobby.Owner.Id == SteamClient.SteamId) return;

        var me = lobby.Members.FirstOrDefault(m => m.Id == SteamClient.SteamId);
        string currentStatus = lobby.GetMemberData(me, "status");
        string nextStatus = (currentStatus == "ready") ? "not_ready" : "ready";
        lobby.SetMemberData("status", nextStatus);
    }

    public void OpenSteamInvite()
    {
        if (CurrentLobby.HasValue)
        {
            SteamFriends.OpenGameInviteOverlay(CurrentLobby.Value.Id);
        }
    }

    public void LeaveLobby()
    {
        if (CurrentLobby.HasValue)
        {
            CurrentLobby.Value.Leave();
            CurrentLobby = null;
            isWeaponConfirmed = false;
            selectedWeaponID = -1;
            MyJob = null;
            OnLobbyLeft?.Invoke();
        }
    }

    public int GetSelectedWeaponID()
    {
        return selectedWeaponID;
    }
}
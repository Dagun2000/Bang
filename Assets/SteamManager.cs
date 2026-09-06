using UnityEngine;
using Steamworks;
using System;

public class SteamManager : MonoBehaviour
{
    private static SteamManager _instance;
    public uint AppId = 480; // 테스트용 ID

    void Awake()
    {
        if (_instance != null)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);

        try
        {
            // 스팀 클라이언트 초기화
            SteamClient.Init(AppId);
            Debug.Log($"스팀 초기화 성공: {SteamClient.Name}");
        }
        catch (Exception e)
        {
            Debug.LogError("스팀 초기화 실패: " + e.Message);
            // 실제 출시 때는 여기서 게임을 종료시키는 처리가 필요합니다.
        }
    }

    void OnApplicationQuit()
    {
        // 게임 종료 시 스팀 연결 해제
        SteamClient.Shutdown();
    }

    void Update()
    {
        // 스팀 콜백(이벤트)을 처리하기 위해 매 프레임 호출
        SteamClient.RunCallbacks();
    }
}
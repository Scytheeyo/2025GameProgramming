using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using TMPro;

public class OptionPage : MonoBehaviour
{
    [Header("Audio Settings")]
    public AudioMixer audioMixer;
    public Slider bgmSlider;
    public Slider sfxSlider;

    [Header("Video Settings")]
    public TMP_Dropdown resolutionDropdown;
    public Toggle fullscreenToggle;

    [Header("References")]
    public GameObject startPanel;
    public GameObject titleObj;
    public Animator uiAnimator; // ★ 인스펙터에서 자기 자신(OptionCanvas)을 연결하세요

    List<Resolution> resolutions = new List<Resolution>();

    // 1. 원래 값 (취소 눌렀을 때 돌아갈 값)
    private int originResIndex;
    private bool originFullscreen;
    private float originBGMVol;
    private float originSFXVol;

    // 2. 임시 값 (확인 눌렀을 때 적용할 값)
    private int tempResIndex;
    private bool tempFullscreen;
    private float tempBGMVol;
    private float tempSFXVol;

    void Awake()
    {
        // 해상도 리스트 초기화
        InitResolutionList();
    }

    // 옵션 창이 켜질 때마다 호출
    void OnEnable()
    {
        LoadCurrentSettings(); // 현재 설정 및 저장된 값 불러오기
        UpdateUI();            // UI를 불러온 값으로 갱신
    }

    // 1. 해상도 목록 생성
    void InitResolutionList()
    {
        resolutions.Clear();
        resolutionDropdown.ClearOptions();
        List<string> options = new List<string>();

        Resolution[] allResolutions = Screen.resolutions;

        for (int i = 0; i < allResolutions.Length; i++)
        {
            Resolution item = allResolutions[i];
            string option = item.width + " x " + item.height;
            options.Add(option);
            resolutions.Add(item);
        }

        resolutionDropdown.AddOptions(options);
        resolutionDropdown.RefreshShownValue();
    }

    // 2. 현재 설정값을 불러와서 저장
    void LoadCurrentSettings()
    {
        // (1) 소리 불러오기
        originBGMVol = PlayerPrefs.GetFloat("BGMVolume", 0.75f);
        originSFXVol = PlayerPrefs.GetFloat("SFXVolume", 0.75f);

        // (2) 전체화면 불러오기
        if (PlayerPrefs.HasKey("Fullscreen"))
        {
            originFullscreen = PlayerPrefs.GetInt("Fullscreen") == 1;
        }
        else
        {
            originFullscreen = Screen.fullScreen;
        }

        // (3) 해상도 불러오기
        if (PlayerPrefs.HasKey("ResolutionNum"))
        {
            originResIndex = PlayerPrefs.GetInt("ResolutionNum");

            // 유효성 검사 (리스트 범위 초과 방지)
            if (originResIndex >= resolutions.Count) originResIndex = 0;
        }
        else
        {
            // 저장된 값이 없으면 현재 화면 크기와 일치하는 해상도 찾기
            originResIndex = 0;
            for (int i = 0; i < resolutions.Count; i++)
            {
                if (resolutions[i].width == Screen.width && resolutions[i].height == Screen.height)
                {
                    originResIndex = i;
                    break;
                }
            }
        }

        // 임시 변수 초기화
        tempBGMVol = originBGMVol;
        tempSFXVol = originSFXVol;
        tempFullscreen = originFullscreen;
        tempResIndex = originResIndex;
    }

    // 3. UI 갱신
    void UpdateUI()
    {
        bgmSlider.value = tempBGMVol;
        sfxSlider.value = tempSFXVol;
        resolutionDropdown.value = tempResIndex;
        fullscreenToggle.isOn = tempFullscreen;
    }

    // --- UI 변경 이벤트 (Dynamic 연결 필수) ---

    public void OnResolutionChanged(int index)
    {
        tempResIndex = index;
    }

    public void OnFullscreenChanged(bool isFull)
    {
        tempFullscreen = isFull;
    }

    public void OnBGMChanged(float volume)
    {
        tempBGMVol = volume;
        SetAudioMixer("MyBGM", tempBGMVol);
    }

    public void OnSFXChanged(float volume)
    {
        tempSFXVol = volume;
        SetAudioMixer("MySFX", tempSFXVol);
    }

    // 믹서 볼륨 조절용 내부 함수
    private void SetAudioMixer(string paramName, float volume)
    {
        if (volume <= 0) volume = 0.0001f;
        audioMixer.SetFloat(paramName, Mathf.Log10(volume) * 20);
    }

    private void ApplyAudioSettings(float bgm, float sfx)
    {
        SetAudioMixer("MyBGM", bgm);
        SetAudioMixer("MySFX", sfx);
    }


    // --- [핵심] 버튼 기능 ---

    // [확인 버튼] : 저장하고 닫기
    public void ClickConfirm()
    {
        // 1. 비디오 적용
        Screen.SetResolution(resolutions[tempResIndex].width, resolutions[tempResIndex].height, tempFullscreen);

        // 2. 데이터 저장 (PlayerPrefs)
        PlayerPrefs.SetInt("ResolutionNum", tempResIndex);
        PlayerPrefs.SetInt("Fullscreen", tempFullscreen ? 1 : 0);
        PlayerPrefs.SetFloat("BGMVolume", tempBGMVol);
        PlayerPrefs.SetFloat("SFXVolume", tempSFXVol);
        PlayerPrefs.Save();

        // 3. 현재 상태를 '원본'으로 갱신 (다시 열었을 때 유지를 위해)
        originBGMVol = tempBGMVol;
        originSFXVol = tempSFXVol;
        originResIndex = tempResIndex;
        originFullscreen = tempFullscreen;

        // 4. 창 닫기 시작
        CloseOptionWindow();
    }

    // [취소 버튼] : 되돌리고 닫기
    public void ClickCancel()
    {
        // 소리 되돌리기 (미리 듣기로 바뀐 소리 복구)
        ApplyAudioSettings(originBGMVol, originSFXVol);

        // 창 닫기 시작
        CloseOptionWindow();
    }

    // 1단계: 애니메이션 트리거 발동
    void CloseOptionWindow()
    {
        if (uiAnimator != null)
        {
            uiAnimator.SetTrigger("doClose");
        }
        else
        {
            // 애니메이터가 없으면 바로 끄기
            DisableWindow();
        }
    }

    // 2단계: 진짜로 창 끄기 (Animation Event로 호출됨)
    public void DisableWindow()
    {
        gameObject.SetActive(false);

        // Start Scene용 타이틀 복구
        if (startPanel != null) startPanel.SetActive(true);
        if (titleObj != null) titleObj.SetActive(true);

        // Game Scene용 일시정지 해제
        Player player = FindObjectOfType<Player>();
        if (player != null)
        {
            player.UpdateGamePauseState();
        }
    }
}
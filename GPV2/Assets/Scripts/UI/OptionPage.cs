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


    // 해상도 목록
    List<Resolution> resolutions = new List<Resolution>();

    // --- 변수 선언 ---
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

    public GameObject startPanel;
    public GameObject titleObj;

    void Awake()
    {
        // 해상도 리스트 초기화는 한 번만 수행
        InitResolutionList();
    }

    // 옵션 창이 켜질 때마다(SetActive true) 호출됨
    void OnEnable()
    {
        LoadCurrentSettings(); // 현재 설정 불러오기 & 원래 값 기억
        UpdateUI();            // UI를 기억된 값으로 맞춤
    }

    // 1. 해상도 목록 생성
    void InitResolutionList()
    {
        resolutions.Clear();
        resolutionDropdown.ClearOptions();
        List<string> options = new List<string>();

        int currentResIndex = 0;
        Resolution[] allResolutions = Screen.resolutions;

        for (int i = 0; i < allResolutions.Length; i++)
        {
            Resolution item = allResolutions[i];
            string option = item.width + " x " + item.height;
            options.Add(option);

            if (item.width == Screen.width && item.height == Screen.height)
                currentResIndex = i;

            resolutions.Add(item);
        }

        resolutionDropdown.AddOptions(options);
        resolutionDropdown.RefreshShownValue();
    }

    // 2. 현재 설정값을 불러와서 '원래 값'과 '임시 값'에 저장
    void LoadCurrentSettings()
    {
        // 저장된 데이터가 없으면 기본값 가져오기
        originBGMVol = PlayerPrefs.GetFloat("BGMVolume", 1f);
        originSFXVol = PlayerPrefs.GetFloat("SFXVolume", 1f);

        // 해상도는 현재 스크린 설정 기준
        originFullscreen = Screen.fullScreen;

        // 현재 해상도 인덱스 찾기
        originResIndex = 0;
        for (int i = 0; i < resolutions.Count; i++)
        {
            if (resolutions[i].width == Screen.width && resolutions[i].height == Screen.height)
            {
                originResIndex = i;
                break;
            }
        }

        // 임시 변수들도 초기화 (아직 변경 안 했으므로 원래 값과 동일)
        tempBGMVol = originBGMVol;
        tempSFXVol = originSFXVol;
        tempFullscreen = originFullscreen;
        tempResIndex = originResIndex;
    }

    // 3. UI 갱신 (임시 변수 기준)
    void UpdateUI()
    {
        bgmSlider.value = tempBGMVol;
        sfxSlider.value = tempSFXVol;
        resolutionDropdown.value = tempResIndex;
        fullscreenToggle.isOn = tempFullscreen;
    }

    // --- UI 변경 이벤트 (임시 변수만 바꿈) ---

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
        // 오디오는 미리 듣기를 위해 믹서에는 즉시 적용 (저장은 안 함)
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


    // --- [핵심] 버튼 기능 ---

    // [확인 버튼] : 임시 값을 진짜 저장하고 적용
    public void ClickConfirm()
    {
        // 1. 비디오 적용
        Screen.SetResolution(resolutions[tempResIndex].width, resolutions[tempResIndex].height, tempFullscreen);

        // 2. 데이터 저장 (PlayerPrefs)
        PlayerPrefs.SetInt("ResolutionNum", tempResIndex);
        PlayerPrefs.SetFloat("BGMVolume", tempBGMVol);
        PlayerPrefs.SetFloat("SFXVolume", tempSFXVol);
        PlayerPrefs.Save();

        // 3. 옵션 창 닫기
        CloseOptionWindow();
    }

    // [취소 버튼] : 변경 사항 무시하고 되돌리기
    public void ClickCancel()
    {
        // 1. 소리 되돌리기 (미리 듣기로 바뀐 소리를 원상복구)
        SetAudioMixer("MyBGM", originBGMVol);
        SetAudioMixer("MySFX", originSFXVol);

        // 2. 옵션 창 닫기
        CloseOptionWindow();
    }

    void CloseOptionWindow()
    {
        gameObject.SetActive(false); 
        if (startPanel != null) startPanel.SetActive(true);
        if (titleObj != null) titleObj.SetActive(true);
    }
}
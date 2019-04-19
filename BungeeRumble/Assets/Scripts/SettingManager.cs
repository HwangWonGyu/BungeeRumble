using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using UnityEngine.SceneManagement;

public class SettingManager : MonoBehaviour {

#region public variable

    public Toggle fullscreenToggle;
    public Dropdown resolutionDropdown;
    public Slider textureQualitySlider;
    public Slider antialiasingSlider;
    public Slider vSyncSlider;
    public Slider musicVolumeSlider;
    public Button applyButton;
    public AudioManager audioManager;

    public Resolution[] resolutions;
    public GameSettings gameSettings;

    #endregion

    private void OnEnable()
    {
        gameSettings = new GameSettings();

        fullscreenToggle.onValueChanged.AddListener(delegate { OnFullscreenToggle(); });
        resolutionDropdown.onValueChanged.AddListener(delegate { OnResolutionChange(); });
        textureQualitySlider.onValueChanged.AddListener(delegate { OnTextureQualityChange(); });
        antialiasingSlider.onValueChanged.AddListener(delegate { OnAntialiasingChange(); });
        vSyncSlider.onValueChanged.AddListener(delegate { OnVSyncChange(); });
        musicVolumeSlider.onValueChanged.AddListener(delegate { OnMusicVolumechange(); });
        applyButton.onClick.AddListener(delegate { OnApplyButtonClick(); });

        resolutions = Screen.resolutions;

        resolutionDropdown.options.Clear();

        foreach (Resolution resolution in resolutions)
        {
            resolutionDropdown.options.Add(new Dropdown.OptionData(resolution.ToString()));
        }

        if (File.Exists(Application.persistentDataPath + "/gamesettings.json") == true)
        {
            LoadSettings();
        }
    }

    public void OnFullscreenToggle()
    {
        gameSettings.fullscreen = Screen.fullScreen = fullscreenToggle.isOn;
    }

    public void OnResolutionChange()
    {
        Screen.SetResolution(resolutions[resolutionDropdown.value].width, resolutions[resolutionDropdown.value].height, Screen.fullScreen);
        gameSettings.resolutionIndex = resolutionDropdown.value;
    }

    public void OnTextureQualityChange()
    {
        int temp = Slerp(textureQualitySlider.value, "Texture");
        QualitySettings.masterTextureLimit = temp;
        gameSettings.textureQuality = textureQualitySlider.value;
    }

    public void OnAntialiasingChange()
    {
        int temp = Slerp(antialiasingSlider.value, "AA");
        QualitySettings.antiAliasing = temp;
        gameSettings.antialiasing = antialiasingSlider.value;
    }

    public void OnVSyncChange()
    {
        int temp = Slerp(vSyncSlider.value, "Vsync");
        QualitySettings.vSyncCount = temp;
        gameSettings.vSync = vSyncSlider.value;
    }

    public void OnMusicVolumechange()
    {
        audioManager.audioValue = gameSettings.musicVolume = musicVolumeSlider.value;

		if(musicVolumeSlider.value * 100 >= 0.0f && musicVolumeSlider.value * 100 < 10.0f)
		{
	        musicVolumeSlider.GetComponentInChildren<Text>().text = (musicVolumeSlider.value * 100).ToString("0");
		}
		else if (musicVolumeSlider.value * 100 >= 10.0f && musicVolumeSlider.value * 100 < 100.0f)
		{
			musicVolumeSlider.GetComponentInChildren<Text>().text = (musicVolumeSlider.value * 100).ToString("00");
		}
		else if (musicVolumeSlider.value * 100 == 100.0f)
		{
			musicVolumeSlider.GetComponentInChildren<Text>().text = (musicVolumeSlider.value * 100).ToString("000");
		}
		audioManager.AudioSet();
    }

    public void OnApplyButtonClick()
    {
        SaveSettings();
    }

    public void SaveSettings()
    {
        string jsonData = JsonUtility.ToJson(gameSettings, true);
        File.WriteAllText(Application.persistentDataPath + "/gamesettings.json", jsonData);
    }

    public void LoadSettings()
    {
        gameSettings = JsonUtility.FromJson<GameSettings>(File.ReadAllText(Application.persistentDataPath + "/gamesettings.json"));

        musicVolumeSlider.value = gameSettings.musicVolume;
        antialiasingSlider.value = gameSettings.antialiasing;
        vSyncSlider.value = gameSettings.vSync;
        textureQualitySlider.value = gameSettings.textureQuality;
        resolutionDropdown.value = gameSettings.resolutionIndex;
        fullscreenToggle.isOn = gameSettings.fullscreen;

        Screen.fullScreen = gameSettings.fullscreen;

        resolutionDropdown.RefreshShownValue();

    }

    int Slerp(float value,string name)
    {
        if (name != "AA")   
        {
            if (0 <= value && value < 0.33f)
            {
                if(name != "Vsync")
                {
                    textureQualitySlider.value = 0;
                    textureQualitySlider.GetComponentInChildren<Text>().text = "낮음";
                    textureQualitySlider.GetComponentInChildren<Text>().color = Color.yellow;
                }
                else
                {
                    vSyncSlider.value = 0;
                    vSyncSlider.GetComponentInChildren<Text>().text = "낮음";
                    vSyncSlider.GetComponentInChildren<Text>().color = Color.yellow;

					QualitySettings.vSyncCount = 2;
					Application.targetFrameRate = 30;
				}
                return 2;
            }
            else if (0.33f <= value && value < 0.66f)
            {
                if (name != "Vsync")
                {
                    textureQualitySlider.value = 0.5f;
                    textureQualitySlider.GetComponentInChildren<Text>().text = "중간";
                    textureQualitySlider.GetComponentInChildren<Text>().color = new Color(1f, 0.49f, 0);
                }
                else
                {
                    vSyncSlider.value = 0.5f;
                    vSyncSlider.GetComponentInChildren<Text>().text = "높음";
                    vSyncSlider.GetComponentInChildren<Text>().color = new Color(1f, 0.49f, 0);

					QualitySettings.vSyncCount = 1;
					Application.targetFrameRate = 60;
				}

                return 1;
            }
            else if (0.66f <= value && value <= 1)
            {
                if (name != "Vsync")
                {
                    textureQualitySlider.value = 1;
                    textureQualitySlider.GetComponentInChildren<Text>().text = "높음";
                    textureQualitySlider.GetComponentInChildren<Text>().color = Color.red;
				}
                else
                {
                    vSyncSlider.value = 1;
                    vSyncSlider.GetComponentInChildren<Text>().text = "없음";
                    vSyncSlider.GetComponentInChildren<Text>().color = Color.red;

					QualitySettings.vSyncCount = 0;
					Application.targetFrameRate = 60;
				}

                return 0;
            }
        }
        else if(name == "AA")
        {
            if (0 <= value && value < 0.33f)
            {
                antialiasingSlider.value = 0f;
                antialiasingSlider.GetComponentInChildren<Text>().text = "없음";
                antialiasingSlider.GetComponentInChildren<Text>().color = Color.yellow;
                return 1;
            }
            else if (0.33f <= value && value < 0.66f)
            {
                antialiasingSlider.value = 0.5f;
                antialiasingSlider.GetComponentInChildren<Text>().text = "중간";
                antialiasingSlider.GetComponentInChildren<Text>().color = new Color(1f,0.49f, 0);
                return 2;
            }
            else if (0.66f <= value && value <= 1)
            {
                antialiasingSlider.value = 1;
                antialiasingSlider.GetComponentInChildren<Text>().text = "높음";
                antialiasingSlider.GetComponentInChildren<Text>().color = Color.red;
                return 4;
            }
        }
        return 0;
    }
}

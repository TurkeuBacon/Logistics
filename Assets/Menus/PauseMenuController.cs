using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PauseMenuController : MonoBehaviour
{
    //UI Objects
    public GameObject PauseMenu, SettingsMenu,
        GameplaySettingsMenu, ControlsSettingsMenu, AudioSettingsMenu;
    public Button defaultSettingsMenuButton, ApplyButton;

    //Setting Input Labels
    public Text renderDistanceLabel, cameraSensitivityLabel;

    //Setting Inputs
    public Slider renderDistanceSlider, cameraSensitivitySlider;
    
    //Terrain Data
    private int Terrain_Changes = 0;
    private int renderDistanceSetting; // 0
    //Player Data
    private int Player_Changes = 0;
    private int cameraSensitivitySetting; // 0

    //Delegates
    public delegate void ApplySettingsDelegate(params int[] settings);
    //Events
    public event ApplySettingsDelegate gameplaySettingsApply, playerSettingsApply;


    private bool paused;
    // Start is called before the first frame update
    void Start()
    {
        PauseMenu.SetActive(false);
        SettingsMenu.SetActive(false);
        renderDistanceSetting = 30;
        cameraSensitivitySetting = 65;
        revertSettingsMenu();
        Apply();
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Escape))
        {
            if(paused)
            {
                paused = false;
                Time.timeScale = 1;
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
                SettingsMenu.SetActive(false);
                PauseMenu.SetActive(false);
                revertSettingsMenu();
            }
            else
            {
                paused = true;
                Time.timeScale = 0;
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
                PauseMenu.SetActive(true);
            }
        }
    }

    public void Apply()
    {
        renderDistanceSetting = (int)renderDistanceSlider.value;
        cameraSensitivitySetting = (int)cameraSensitivitySlider.value;

        ApplyButton.interactable = false;
        if(gameplaySettingsApply != null)
        {
            gameplaySettingsApply(renderDistanceSetting);
        }
        if(playerSettingsApply != null)
        {
            playerSettingsApply(cameraSensitivitySetting);
        }
    }
    private void revertSettingsMenu()
    {
        //Terrain Settings
        renderDistanceLabel.text = "Render Distance: " + renderDistanceSetting;
        renderDistanceSlider.value = renderDistanceSetting;
        //Player Settings
        cameraSensitivityLabel.text = "Camera Sensitivity: " + renderDistanceSetting;
        cameraSensitivitySlider.value = cameraSensitivitySetting;

        ApplyButton.interactable = false;
    }
    public void renderDistanceChange(System.Single value)
    {
        if (renderDistanceSetting != value)
        {
            Terrain_Changes |= 1 << 0;
        }
        else
        {
            Terrain_Changes &= ~(1 << 0);
        }
        renderDistanceLabel.text = "Render Distance: " + value;
        ApplyButton.interactable = Terrain_Changes > 0 || Player_Changes > 0;
    }
    public void cameraSensitivityChange(System.Single value)
    {
        if (cameraSensitivitySetting != value)
        {
            Player_Changes |= 1 << 0;
        }
        else
        {
            Player_Changes &= ~(1 << 0);
        }
        cameraSensitivityLabel.text = "Camera Sensitivity: " + value;
        ApplyButton.interactable = Player_Changes > 0 || Terrain_Changes > 0;
    }

    public void openSettingsMenu()
    {
        SettingsMenu.SetActive(true);
        defaultSettingsMenuButton.Select();
        openGameplayMenu();
    }
    public void openGameplayMenu()
    {
        GameplaySettingsMenu.SetActive(true);
        ControlsSettingsMenu.SetActive(false);
        AudioSettingsMenu.SetActive(false);
    }
    public void openControlsMenu()
    {
        GameplaySettingsMenu.SetActive(false);
        ControlsSettingsMenu.SetActive(true);
        AudioSettingsMenu.SetActive(false);
    }
    public void openAudioMenu()
    {
        GameplaySettingsMenu.SetActive(false);
        ControlsSettingsMenu.SetActive(false);
        AudioSettingsMenu.SetActive(true);
    }
}

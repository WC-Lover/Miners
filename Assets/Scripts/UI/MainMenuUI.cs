using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{

    [SerializeField] private Button startButton; 
    [SerializeField] private Button optionsButton; 
    [SerializeField] private Button quitButton;

    [SerializeField] private PopupWindowUI optionsWindow;

    private void Awake()
    {
        startButton.onClick.AddListener(() => {
            Loader.Load(Loader.Scene.LobbyMenuScene);
        });

        optionsButton.onClick.AddListener(() => {
            optionsWindow.Show();
        });

        quitButton.onClick.AddListener(() => {
            Application.Quit();
        });

        Time.timeScale = 1f;
    }
}

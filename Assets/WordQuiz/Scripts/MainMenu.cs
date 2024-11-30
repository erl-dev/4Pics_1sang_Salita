using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public GameObject mainMenuUI;
    public GameObject aboutUI;
    public GameObject musicOnButton;
    public GameObject musicOffButton;

    public void PlayGame ()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    public void QuitGame ()
    {
        Application.Quit();
    }

    public void BackToMenu(){
        aboutUI.SetActive(false);
        mainMenuUI.SetActive(true);
    }

    public void AboutButton(){
        aboutUI.SetActive(true);
        mainMenuUI.SetActive(false);
    }
    
    public void MusicOff(){
        musicOffButton.SetActive(false);
        musicOnButton.SetActive(true);
    }

     public void MusicOn(){
        musicOffButton.SetActive(true);
        musicOnButton.SetActive(false);
    }
}

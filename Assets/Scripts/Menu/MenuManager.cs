using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    public void MainMenu()
    {
        SceneManager.LoadScene(0);
    }

    public void NewGame()
    {
        SceneManager.LoadScene(2);
    }

    public void Settings()
    {
        SceneManager.LoadScene(1);
    }

    public void ExitGame()
    {
        Application.Quit(); 
    }

    [Header("Audio")]
    [SerializeField] private AudioClip musicForThisScene;
    [SerializeField] private float musicFadeDuration = 1.0f;

    void Start()
    {
        AudioManager.Instance.PlayMusic(musicForThisScene, musicFadeDuration);
    }
}
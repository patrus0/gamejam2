using UnityEngine;

public class LevelMusicPlayer : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioClip musicForThisScene;
    [SerializeField] private float musicFadeDuration = 1.0f;

    void Start()
    {
        AudioManager.Instance.PlayMusic(musicForThisScene, musicFadeDuration);
    }
}

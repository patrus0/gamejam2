using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class VolumeSettings : MonoBehaviour
{
    [SerializeField] private AudioMixer mixer;
    [SerializeField] private Slider musicSlider;
    private const float MIN_VOLUME = 0.0001f;

    void Start()
    {
        if (musicSlider == null)
            return;

        musicSlider.onValueChanged.AddListener(SetMusicVolume);
        // Подгружаем сохранённое значение из AudioManager, если он уже существует.
        float savedVolume = AudioManager.Instance != null
            ? AudioManager.Instance.GetMusicVolume()
            : PlayerPrefs.GetFloat("MusicVolume", 0.75f);
        musicSlider.value = savedVolume;
        SetMusicVolume(savedVolume);
    }

    private void OnDestroy()
    {
        if (musicSlider != null)
            musicSlider.onValueChanged.RemoveListener(SetMusicVolume);
    }

    public void SetMusicVolume(float value)
    {
        value = Mathf.Clamp01(value);

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMusicVolume(value);
            return;
        }

        // Фолбэк, если AudioManager отсутствует в сцене.
        if (mixer != null)
            mixer.SetFloat("MusicVolume", Mathf.Log10(Mathf.Max(MIN_VOLUME, value)) * 20f);

        PlayerPrefs.SetFloat("MusicVolume", value);
        PlayerPrefs.Save();
    }
}
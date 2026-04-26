using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class VolumeSettings : MonoBehaviour
{
    [SerializeField] private AudioMixer mixer;
    [SerializeField] private Slider musicSlider;

    void Start()
    {
        musicSlider.onValueChanged.AddListener(SetMusicVolume);
        // Подгружаем сохранённое значение
        float savedVolume = PlayerPrefs.GetFloat("MusicVolume", 0.75f);
        musicSlider.value = savedVolume;
        SetMusicVolume(savedVolume);
    }

    public void SetMusicVolume(float value)
    {
        // преобразуем линейное значение (0-1) в децибелы: Mathf.Log10(value) * 20 
        mixer.SetFloat("MusicVolume", Mathf.Log10(value) * 20);
        PlayerPrefs.SetFloat("MusicVolume", value);
    }
}
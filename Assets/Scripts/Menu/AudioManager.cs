using UnityEngine;
using UnityEngine.Audio; // Не забудьте эту библиотеку!

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance; // 1. Singleton для доступа из любого места

    [SerializeField] private AudioMixer mixer; // 2. Ссылка на MasterMixer
    [SerializeField] private AudioSource musicSource; // 3. Источник для музыки

    private const string MUSIC_VOLUME_KEY = "MusicVolume"; // Ключ для сохранения
    private const float MIN_VOLUME = 0.0001f;
    private float currentMusicVolume = 0.75f; // Значение по умолчанию
    private Coroutine musicTransitionRoutine;

    void Awake()
    {
        // --- Реализация Singleton ---
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); // Объект будет жить вечно между сценами
        // --- Конец реализации Singleton ---

        // Загрузка сохранённой громкости музыки
        currentMusicVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(MUSIC_VOLUME_KEY, 0.75f));
        ApplyMusicVolume(currentMusicVolume);
    }

    // Публичный метод для смены музыки
    public void PlayMusic(AudioClip newClip, float fadeDuration = 1.0f)
    {
        if (musicSource == null || newClip == null)
            return;

        // Если та же музыка уже играет, ничего не делаем
        if (musicSource.isPlaying && musicSource.clip == newClip) return;

        if (musicTransitionRoutine != null)
            StopCoroutine(musicTransitionRoutine);

        // Запускаем корутину для плавной смены
        musicTransitionRoutine = StartCoroutine(CrossfadeMusic(newClip, Mathf.Max(0f, fadeDuration)));
    }

    private System.Collections.IEnumerator CrossfadeMusic(AudioClip newClip, float duration)
    {
        float startVolume = currentMusicVolume;
        float timer = 0;

        if (duration <= 0f)
        {
            musicSource.clip = newClip;
            musicSource.Play();
            ApplyMusicVolume(startVolume);
            musicTransitionRoutine = null;
            yield break;
        }

        // --- 1. Затихание текущей музыки (если она играет) ---
        if (musicSource.isPlaying)
        {
            while (timer < duration / 2) // Затихаем за первую половину времени
            {
                timer += Time.deltaTime;
                float newVolume = Mathf.Lerp(startVolume, 0, timer / (duration / 2));
                ApplyMusicVolume(newVolume);
                yield return null;
            }
            musicSource.Stop();
        }

        // --- 2. Смена трека ---
        musicSource.clip = newClip;
        musicSource.Play();

        // --- 3. Нарастание громкости ---
        timer = 0;
        while (timer < duration / 2) // Нарастаем за вторую половину времени
        {
            timer += Time.deltaTime;
            float newVolume = Mathf.Lerp(0, startVolume, timer / (duration / 2));
            ApplyMusicVolume(newVolume);
            yield return null;
        }

        // Убедимся, что громкость в конце точно равна нужной
        ApplyMusicVolume(startVolume);
        musicTransitionRoutine = null;
    }

    // Метод для установки громкости через AudioMixer
    public void SetMusicVolume(float volume)
    {
        currentMusicVolume = Mathf.Clamp01(volume);
        ApplyMusicVolume(currentMusicVolume);
        PlayerPrefs.SetFloat(MUSIC_VOLUME_KEY, currentMusicVolume); // Сохраняем настройку
        PlayerPrefs.Save();
    }

    public float GetMusicVolume() => currentMusicVolume;

    private void ApplyMusicVolume(float volume)
    {
        if (mixer == null)
            return;

        // Преобразуем (0-1) в децибелы: MIN_VOLUME примерно соответствует -80dB.
        mixer.SetFloat("MusicVolume", Mathf.Log10(Mathf.Max(MIN_VOLUME, volume)) * 20f);
    }
}
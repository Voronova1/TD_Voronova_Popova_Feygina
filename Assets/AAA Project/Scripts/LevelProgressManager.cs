using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelProgressManager : MonoBehaviour
{
    public static LevelProgressManager Instance { get; private set; }

    private const string UNLOCKED_LEVEL_KEY = "UnlockedLevel";
    private int _unlockedLevel = 1;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadProgress();
            Debug.Log($"Progress initialized. Unlocked up to level {_unlockedLevel}");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public bool IsLevelUnlocked(int level) => level <= _unlockedLevel;

    public void MarkLevelCompleted()
    {
        string sceneName = SceneManager.GetActiveScene().name;

        if (sceneName.Contains("_2")) 
        {
            string levelStr = sceneName.Split('_')[0].Replace("IntroDialogue", "");
            if (int.TryParse(levelStr, out int completedLevel))
            {
                if (completedLevel >= _unlockedLevel)
                {
                    _unlockedLevel = completedLevel + 1;
                    SaveProgress();
                    Debug.Log($"Level {completedLevel} completed! Unlocked level {_unlockedLevel}");
                }
            }
        }
    }

    public int GetCurrentLevelNumber()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        if (sceneName.Contains("IntroDialogue") && sceneName.Contains("_"))
        {
            string levelStr = sceneName.Split('_')[0].Replace("IntroDialogue", "");
            if (int.TryParse(levelStr, out int level))
                return level;
        }
        return -1;
    }

    private void LoadProgress() => _unlockedLevel = PlayerPrefs.GetInt(UNLOCKED_LEVEL_KEY, 1);

    public void SaveProgress()
    {
        PlayerPrefs.SetInt(UNLOCKED_LEVEL_KEY, _unlockedLevel);
        PlayerPrefs.Save();
    }
    public void ResetProgress()
    {
        _unlockedLevel = 1;
        PlayerPrefs.DeleteKey(UNLOCKED_LEVEL_KEY);
        PlayerPrefs.Save();
        Debug.Log("Progress reset to level 1");
    }
}
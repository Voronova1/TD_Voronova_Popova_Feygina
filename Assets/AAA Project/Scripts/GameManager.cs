using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections.Generic;
using System;
using UnityEngine.UI;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public enum LevelType { Normal, Desert }
    public LevelType currentLevelType = LevelType.Normal;

    public static GameManager Instance { get; private set; }
    public static event Action<bool> OnGameEnded;

    public int playerHealth;
    public TMP_Text healthText;
    public int money;
    public TMP_Text moneyText;

    [Header("UI Elements")]
    public GameObject levelFailedPanel;
    public GameObject levelCompletePanel;

    [Header("Scene Names")]
    public string mainMenuScene = "MainMenu";
    public string dialogueScene = "DialogueScene";

    [System.NonSerialized]
    public List<MovementMobs> EnemyList = new List<MovementMobs>();

    private WaveSpawner waveSpawner;
    private bool isQuitting = false;
    private bool gameEnded = false;

    [Header("Input Blocking")]
    public static bool IsInputBlocked { get; private set; }

    public enum GameSpeed { Paused, Normal, Fast, VeryFast }
    private GameSpeed currentGameSpeed = GameSpeed.Normal;
    private GameSpeed lastSpeedBeforePause = GameSpeed.Normal; // Запоминаем последнюю скорость

    [Header("UI Elements")]
    public Button pauseButton;
    public Image pauseButtonImage;
    public Sprite pauseIcon;
    public Sprite playIcon;

    public Button speedButton;
    public Image speedButtonImage;
    public Sprite normalSpeedIcon;
    public Sprite fastSpeedIcon;
    public Sprite veryFastSpeedIcon;
    private bool isPaused = false;
    private bool isProcessingClick = false;


    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        IsInputBlocked = false;
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public void TogglePause()
    {
        if (gameEnded || isProcessingClick) return;

        isProcessingClick = true;

        if (gameEnded) return;

        isPaused = !isPaused;

        if (isPaused)
        {
            lastSpeedBeforePause = currentGameSpeed;
            Time.timeScale = 0f;
            currentGameSpeed = GameSpeed.Paused;
            pauseButtonImage.sprite = playIcon;
        }
        else
        {
            Time.timeScale = lastSpeedBeforePause switch
            {
                GameSpeed.Normal => 1f,
                GameSpeed.Fast => 1.5f,
                GameSpeed.VeryFast => 2f,
                _ => 1f
            };
            currentGameSpeed = lastSpeedBeforePause;
            pauseButtonImage.sprite = pauseIcon;
        }

        UpdateSpeedUI();
        StartCoroutine(ResetClickFlag());
    }

    public void CycleSpeed()
    {
        if (isProcessingClick) return;

        isProcessingClick = true;
        if (currentGameSpeed == GameSpeed.Paused)
        {
            SetGameSpeed(GameSpeed.Normal, true);
            return;
        }

        GameSpeed nextSpeed = currentGameSpeed switch
        {
            GameSpeed.Normal => GameSpeed.Fast,
            GameSpeed.Fast => GameSpeed.VeryFast,
            GameSpeed.VeryFast => GameSpeed.Normal,
            _ => GameSpeed.Normal
        };

        SetGameSpeed(nextSpeed, true);
        StartCoroutine(ResetClickFlag());
    }

    private IEnumerator ResetClickFlag()
    {
        yield return new WaitForEndOfFrame();
        isProcessingClick = false;
    }

    private void SetGameSpeed(GameSpeed speed, bool updateUI = false)
    {
        if (gameEnded || isPaused) return;

        currentGameSpeed = speed;

        Time.timeScale = speed switch
        {
            GameSpeed.Normal => 1f,
            GameSpeed.Fast => 1.5f,
            GameSpeed.VeryFast => 2f,
            _ => 1f
        };

        if (updateUI)
        {
            UpdateSpeedUI();
        }

    }

    private void UpdateSpeedUI()
    {
        if (pauseButtonImage != null)
        {
            pauseButtonImage.sprite = currentGameSpeed == GameSpeed.Paused ? playIcon : pauseIcon;
        }

        if (speedButtonImage != null)
        {
            speedButtonImage.sprite = currentGameSpeed switch
            {
                GameSpeed.Normal => normalSpeedIcon,
                GameSpeed.Fast => fastSpeedIcon,
                GameSpeed.VeryFast => veryFastSpeedIcon,
                _ => normalSpeedIcon
            };

            var color = speedButtonImage.color;
            color.a = currentGameSpeed == GameSpeed.Paused ? 0.5f : 1f;
            speedButtonImage.color = color;
        }
    }


    public static void SetInputBlock(bool blocked)
    {
        IsInputBlocked = blocked;
    }
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        InitializeUI();
    }

    void Start()
    {
        if (pauseButton == null || speedButton == null)
        {
            Debug.LogError("Кнопки не назначены в инспекторе!");
            return;
        }

        pauseButton.onClick.AddListener(TogglePause);
        speedButton.onClick.AddListener(CycleSpeed);

        isPaused = false;
        Time.timeScale = 1f;
        currentGameSpeed = GameSpeed.Normal;
        UpdateSpeedUI();

        SetGameSpeed(GameSpeed.Normal, updateUI: true);

        waveSpawner = FindObjectOfType<WaveSpawner>();
        DetermineLevelType();
    }

    private void DetermineLevelType()
    {
        currentLevelType = SceneManager.GetActiveScene().name.ToLower().Contains("desert")
            ? LevelType.Desert
            : LevelType.Normal;
    }

    void InitializeUI()
    {
        healthText.text = playerHealth.ToString();
        moneyText.text = money.ToString();
        levelFailedPanel.SetActive(false);
        levelCompletePanel.SetActive(false);
    }

    public void ChangeMoney(int count)
    {
        money += count;
        moneyText.text = money.ToString();
    }

    public void ChangeHealth(int count)
    {
        playerHealth -= count;
        healthText.text = playerHealth.ToString();

        if (playerHealth <= 0)
        {
            LevelFailed();
        }
    }

    public void AddEnemyOnList(MovementMobs enemy)
    {
        if (enemy == null) return;
        if (!EnemyList.Contains(enemy))
        {
            EnemyList.Add(enemy);
        }
    }

    public void SafeRemoveEnemyFromList(MovementMobs enemy)
    {
        if (isQuitting || enemy == null) return;

        if (EnemyList.Contains(enemy))
        {
            EnemyList.Remove(enemy);
            CheckLevelCompletion();
        }
    }

    public void CheckLevelCompletion()
    {
        if (gameEnded) return;
        if (waveSpawner == null) waveSpawner = FindObjectOfType<WaveSpawner>();
        if (waveSpawner == null) return;

        bool wavesDone = waveSpawner.AllWavesCompleted;
        bool noActiveEnemies = EnemyList.Count == 0;
        bool notSpawning = !waveSpawner.IsSpawning;

        if (wavesDone && noActiveEnemies && notSpawning && playerHealth > 0)
        {
            LevelComplete();
        }
    }

    private void LevelFailed()
    {
        if (!gameEnded && levelFailedPanel != null)
        {
            gameEnded = true;
            Time.timeScale = 0f;
            levelFailedPanel.SetActive(true);
            SetInputBlock(true);
            OnGameEnded?.Invoke(true);
        }
    }

    private void LevelComplete()
    {
        if (!gameEnded && levelCompletePanel != null)
        {
            gameEnded = true;
            Time.timeScale = 0f;
            levelCompletePanel.SetActive(true);
            SetInputBlock(true);
            OnGameEnded?.Invoke(true);
        }
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f;
        gameEnded = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        gameEnded = false;
        SceneManager.LoadScene(mainMenuScene);
    }

    public void ContinueToDialogue()
    {
        Time.timeScale = 1f;
        gameEnded = false;
        SceneManager.LoadScene(dialogueScene);
    }

    void OnApplicationQuit()
    {
        isQuitting = true;
    }
}
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;


[System.Serializable]
public class DialogueLine
{
    public enum SlideType { TextAndCharacter, ImageOnly }

    public SlideType slideType = SlideType.TextAndCharacter;

    [TextArea(3, 5)]
    public string text;
    public Sprite characterSprite;
    public Sprite fullscreenImage;
    public AudioClip soundEffect;
}

public class DialogueManager : MonoBehaviour
{
    
    public DialogueLine[] dialogueLines;
    public GameObject textPanel;
    public TMP_Text dialogueText;
    public Image characterImage;
    public GameObject tapPrompt;
    public string nextSceneName = "level_1";
    public AudioSource audioSource;
    public AudioClip defaultSlideSound;

    private int currentLine = 0;
    private float timeSinceLastTap = 0f;
    private bool promptActive = false;

    private PlayerInput playerInput;
    private InputAction touchPressAction;

    public Image fullscreenImage;

    public enum DialogueType { Intro, Outro }
    public DialogueType dialogueType = DialogueType.Intro;
    public int associatedLevel = 1; 

    void Start()
    {
        playerInput = GetComponent<PlayerInput>();
        if (playerInput == null)
        {
            playerInput = gameObject.AddComponent<PlayerInput>();
        }

        touchPressAction = playerInput.actions.FindAction("Fire", true);
        if (touchPressAction == null)
        {
            Debug.LogError("Не найдено действие Fire в Input System");
        }

        if (audioSource == null)
        {
            audioSource = gameObject.GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
            }
        }

        if (dialogueText.GetComponent<CanvasGroup>() == null)
        {
            dialogueText.gameObject.AddComponent<CanvasGroup>();
        }

        ShowLine(0);
        if (tapPrompt != null) tapPrompt.SetActive(false);
    }

    void Update()
    {
        timeSinceLastTap += Time.deltaTime;

        if (timeSinceLastTap > 5f && !promptActive)
        {
            ShowTapPrompt();
        }
        if (touchPressAction != null && touchPressAction.WasPressedThisFrame())
        {
            HandleTap();
        }
    }

    void ShowLine(int index)
    {
        if (dialogueLines == null || index >= dialogueLines.Length || index < 0)
        {
            Debug.LogError("Invalid dialogue line configuration");
            return;
        }

        var line = dialogueLines[index];
        if (line == null)
        {
            Debug.LogError($"Dialogue line at index {index} is null");
            return;
        }

        if (textPanel != null) textPanel.SetActive(false);
        if (characterImage != null) characterImage.gameObject.SetActive(false);
        if (fullscreenImage != null) fullscreenImage.gameObject.SetActive(false);

        switch (line.slideType)
        {
            case DialogueLine.SlideType.TextAndCharacter:
                if (textPanel != null) textPanel.SetActive(true);

                if (dialogueText != null)
                {
                    dialogueText.text = line.text ?? "";
                    dialogueText.gameObject.SetActive(true);
                    FadeInElement(dialogueText);
                }

                if (characterImage != null)
                {
                    characterImage.sprite = line.characterSprite;
                    characterImage.gameObject.SetActive(line.characterSprite != null);
                    if (line.characterSprite != null) FadeInElement(characterImage);
                }
                break;

            case DialogueLine.SlideType.ImageOnly:
                if (fullscreenImage != null)
                {
                    fullscreenImage.sprite = line.fullscreenImage;
                    fullscreenImage.gameObject.SetActive(line.fullscreenImage != null);
                    if (line.fullscreenImage != null) FadeInElement(fullscreenImage);
                }
                break;
        }

        if (audioSource != null && line.soundEffect != null)
        {
            audioSource.PlayOneShot(line.soundEffect);
        }

        timeSinceLastTap = 0f;
        if (promptActive) HideTapPrompt();
    }

    void FadeInElement(Graphic element)
    {
        CanvasGroup cg = element.GetComponent<CanvasGroup>();
        if (cg == null) cg = element.gameObject.AddComponent<CanvasGroup>();

        cg.alpha = 0;
        LeanTween.alphaCanvas(cg, 1f, 0.3f);
    }

    void ShowTapPrompt()
    {
        if (tapPrompt == null) return;

        promptActive = true;
        tapPrompt.SetActive(true);

        CanvasGroup cg = tapPrompt.GetComponent<CanvasGroup>();
        if (cg == null) cg = tapPrompt.AddComponent<CanvasGroup>();

        LeanTween.alphaCanvas(cg, 0.3f, 0.5f)
            .setLoopPingPong();
    }

    void HideTapPrompt()
    {
        if (tapPrompt == null) return;

        LeanTween.cancel(tapPrompt);
        promptActive = false;
        tapPrompt.SetActive(false);
    }

    void HandleTap()
    {
        timeSinceLastTap = 0f;

        if (tapPrompt != null)
            tapPrompt.SetActive(false);

        if (currentLine < dialogueLines.Length - 1)
        {
            currentLine++;
            ShowLine(currentLine);
        }
        else
        {
            string sceneName = SceneManager.GetActiveScene().name;

            if (sceneName.EndsWith("_2"))
            {
                LevelProgressManager.Instance.MarkLevelCompleted();

                int currentLevel = LevelProgressManager.Instance.GetCurrentLevelNumber();
                Debug.Log($"Completed level {currentLevel}. Loading MainMenu");
            }

            if (!string.IsNullOrEmpty(nextSceneName))
            {
                SceneManager.LoadScene(nextSceneName);
            }
            else
            {
                SceneManager.LoadScene("MainMenu");
                Debug.LogWarning("Next scene name not specified! Loading MainMenu by default");
            }
        }
    }
}
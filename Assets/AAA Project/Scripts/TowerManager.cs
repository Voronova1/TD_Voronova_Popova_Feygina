using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


[RequireComponent(typeof(PlayerInput))]
public class TowerManager : MonoBehaviour
{
    [Header("Context Menus")]
    public GameObject contextMenu;
    public GameObject contextMenuUp;
    public GameObject contextMenuMaxUp;
    public RectTransform menuParent;

    private GameObject selectedBuildPoint;
    private GameObject selectedTower;
    private Camera mainCamera;
    private PlayerInput playerInput;
    private InputAction touchPositionAction;
    private InputAction touchPressAction;
    private bool inputBlocked = false;

    [Header("Level Settings")]
    public bool isLevel1 = false;
    public bool isLevel2 = false;

    [Header("Debug")]
    [SerializeField] private bool debugMode = true;

    private static TowerManager _instance;
    void Start()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        isLevel1 = sceneName.Contains("level_1");
        isLevel2 = sceneName.Contains("level_2");

        mainCamera = Camera.main;
        playerInput = GetComponent<PlayerInput>();

        InitializeInputActions();
        HideAllContextMenus();

        GameManager.OnGameEnded += HandleGameEnded;
    }

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        FullReset();
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        GameManager.OnGameEnded += HandleGameEnded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        GameManager.OnGameEnded -= HandleGameEnded;
    }

    private void FullReset()
    {
        inputBlocked = false;
        selectedBuildPoint = null;
        selectedTower = null;
        mainCamera = null;

        if (playerInput != null)
        {
            playerInput.actions = null;
            playerInput.enabled = false;
            playerInput.enabled = true;
        }

        HideAllContextMenus();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        FullReset();
        InitializeForNewScene();
        EnsureEventSystemExists();
    }

    private void InitializeForNewScene()
    {
        mainCamera = Camera.main;
        playerInput = GetComponent<PlayerInput>();

        string sceneName = SceneManager.GetActiveScene().name;
        isLevel1 = sceneName.Contains("level_1");
        isLevel2 = sceneName.Contains("level_2");

        if (debugMode)
            Debug.Log($"Инициализация для {sceneName}. isLevel1: {isLevel1}, isLevel2: {isLevel2}");
    }

    private void EnsureEventSystemExists()
    {
        if (FindObjectOfType<EventSystem>() == null)
        {
            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();

            if (debugMode)
                Debug.Log("Создан новый EventSystem для сцены");
        }
    }

    void OnDestroy()
    {
        GameManager.OnGameEnded -= HandleGameEnded;
    }

    void InitializeInputActions()
    {
        touchPressAction = playerInput.actions.FindAction("Fire");
        touchPositionAction = playerInput.actions.FindAction("Point");

        if (touchPressAction == null || touchPositionAction == null)
        {
            Debug.LogError("Required Input Actions not found!");
            enabled = false;
        }
    }

    void HandleGameEnded(bool gameEnded)
    {
        inputBlocked = gameEnded;
        HideAllContextMenus();
        SetButtonsInteractable(contextMenu, !gameEnded);
        SetButtonsInteractable(contextMenuUp, !gameEnded);
        SetButtonsInteractable(contextMenuMaxUp, !gameEnded);
    }

    void SetButtonsInteractable(GameObject menu, bool interactable)
    {
        if (menu == null) return;

        var buttons = menu.GetComponentsInChildren<Button>(true);
        foreach (var button in buttons)
        {
            button.interactable = interactable;
        }
    }

    void HideAllContextMenus()
    {
        if (contextMenu != null) contextMenu.SetActive(false);
        if (contextMenuUp != null) contextMenuUp.SetActive(false);
        if (contextMenuMaxUp != null) contextMenuMaxUp.SetActive(false);
    }

    void Update()
    {
        if (inputBlocked || GameManager.IsInputBlocked) return;

        HandleTouchInput();
    }

    void HandleTouchInput()
    {
        if (inputBlocked || GameManager.IsInputBlocked) return;

        if (touchPressAction.WasPressedThisFrame())
        {
            Vector2 touchPosition = touchPositionAction.ReadValue<Vector2>();

            if (IsPointerOverUI(touchPosition)) return;

            ProcessRaycast(touchPosition);
            CheckForMenuClose(touchPosition);
        }
    }

    private bool IsPointerOverUI(Vector2 screenPosition)
    {
        if (EventSystem.current.IsPointerOverGameObject())
            return true;

        return IsClickInsideAnyMenu(screenPosition) ||
               (GameManager.Instance != null &&
                (IsClickOnPanel(GameManager.Instance.levelFailedPanel, screenPosition) ||
                 IsClickOnPanel(GameManager.Instance.levelCompletePanel, screenPosition)));
    }



    bool IsClickOnPanel(GameObject panel, Vector2 position)
    {
        return panel != null && panel.activeSelf &&
               RectTransformUtility.RectangleContainsScreenPoint(
                   panel.GetComponent<RectTransform>(),
                   position
               );
    }

    bool IsClickInsideAnyMenu(Vector2 touchPosition)
    {
        return (contextMenu != null && contextMenu.activeSelf && IsPositionInMenu(contextMenu, touchPosition)) ||
               (contextMenuUp != null && contextMenuUp.activeSelf && IsPositionInMenu(contextMenuUp, touchPosition)) ||
               (contextMenuMaxUp != null && contextMenuMaxUp.activeSelf && IsPositionInMenu(contextMenuMaxUp, touchPosition));
    }

    bool IsPositionInMenu(GameObject menu, Vector2 position)
    {
        return RectTransformUtility.RectangleContainsScreenPoint(
            menu.GetComponent<RectTransform>(),
            position
        );
    }

    void ProcessRaycast(Vector2 touchPosition)
    {
        if (inputBlocked) return;

        Ray ray = mainCamera.ScreenPointToRay(touchPosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (hit.collider.CompareTag("BuildPoint"))
            {
                selectedBuildPoint = hit.collider.gameObject;
                ShowContextMenu(touchPosition);
            }
            else if (hit.collider.CompareTag("Tower"))
            {
                selectedTower = hit.collider.gameObject;
                ShowContextMenuUp(touchPosition);
            }
            else if (hit.collider.CompareTag("TowerMaxUp"))
            {
                selectedTower = hit.collider.gameObject;
                ShowContextMenuMaxUp(touchPosition);
            }
        }
    }

    void CheckForMenuClose(Vector2 touchPosition)
    {
        if (contextMenu != null && contextMenu.activeSelf && !IsPositionInMenu(contextMenu, touchPosition))
            contextMenu.SetActive(false);

        if (contextMenuUp != null && contextMenuUp.activeSelf && !IsPositionInMenu(contextMenuUp, touchPosition))
            contextMenuUp.SetActive(false);

        if (contextMenuMaxUp != null && contextMenuMaxUp.activeSelf && !IsPositionInMenu(contextMenuMaxUp, touchPosition))
            contextMenuMaxUp.SetActive(false);
    }

    private Vector2 GetAdjustedMenuPosition(Vector2 screenPosition, RectTransform menuRect)
    {
        Vector2 menuSize = menuRect.rect.size;
        Vector2 canvasSize = menuParent.rect.size;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            menuParent,
            screenPosition,
            null,
            out Vector2 localPoint
        );

        float halfMenuWidth = menuSize.x / 2;
        float halfMenuHeight = menuSize.y / 2;

        if (localPoint.x + halfMenuWidth > canvasSize.x / 2)
        {
            localPoint.x = canvasSize.x / 2 - halfMenuWidth;
        }
        else if (localPoint.x - halfMenuWidth < -canvasSize.x / 2)
        {
            localPoint.x = -canvasSize.x / 2 + halfMenuWidth;
        }

        if (localPoint.y + halfMenuHeight > canvasSize.y / 2)
        {
            localPoint.y = canvasSize.y / 2 - halfMenuHeight;
        }
        else if (localPoint.y - halfMenuHeight < -canvasSize.y / 2)
        {
            localPoint.y = -canvasSize.y / 2 + halfMenuHeight;
        }

        return localPoint;
    }
    void ShowContextMenu(Vector2 screenPosition)
    {
        if (contextMenu == null || inputBlocked) return;

        if (isLevel1 || isLevel2)
        {
            Transform firstButton = contextMenu.transform.GetChild(0);
            Button button = firstButton.GetComponent<Button>();

            if (contextMenu.transform.childCount > 1)
            {
                TMP_Text priceText = contextMenu.transform.GetChild(1).GetComponent<TMP_Text>();

                if (priceText != null && button != null)
                {
                    if (button.onClick.GetPersistentEventCount() > 0)
                    {
                        GameObject towerPrefab = button.onClick.GetPersistentTarget(0) as GameObject;
                        if (towerPrefab != null)
                        {
                            Tower tower = towerPrefab.GetComponent<Tower>();
                            if (tower != null)
                            {
                                priceText.text = tower.cost.ToString();
                            }
                        }
                    }
                }
                else
                {
                    Debug.LogError("Не найден TextCost или компонент Button!");
                }
            }

            for (int i = 2; i < contextMenu.transform.childCount; i++)
            {
                contextMenu.transform.GetChild(i).gameObject.SetActive(false);
            }


        }

        contextMenu.SetActive(true);
        RectTransform menuRect = contextMenu.GetComponent<RectTransform>();
        Vector2 adjustedPosition = GetAdjustedMenuPosition(screenPosition, menuRect);
        menuRect.localPosition = adjustedPosition;
    }

    void ShowContextMenuUp(Vector2 screenPosition)
    {
        if (contextMenuUp == null || selectedTower == null || inputBlocked) return;

        Tower tower = selectedTower.GetComponent<Tower>();
        if (tower == null) return;

        if (isLevel1)
        {
            ShowContextMenuMaxUp(screenPosition);
            return;
        }

        if (isLevel2)
        {
            contextMenuUp.transform.GetChild(0).gameObject.SetActive(true); 
            contextMenuUp.transform.GetChild(1).gameObject.SetActive(true); 

            TMP_Text upgradePriceText = contextMenuUp.transform.Find("TextCost")?.GetComponent<TMP_Text>();
            TMP_Text sellPriceText = contextMenuUp.transform.Find("TextCostSell")?.GetComponent<TMP_Text>();

            if (upgradePriceText != null)
                upgradePriceText.text = Mathf.RoundToInt(tower.cost * 1.3f).ToString();

            if (sellPriceText != null)
                sellPriceText.text = Mathf.RoundToInt(tower.cost * 0.6f).ToString();
        }
        else
        {
            TMP_Text upgradePriceText = contextMenuUp.transform.Find("TextCost")?.GetComponent<TMP_Text>();
            TMP_Text sellPriceText = contextMenuUp.transform.Find("TextCostSell")?.GetComponent<TMP_Text>();

            if (upgradePriceText != null)
                upgradePriceText.text = Mathf.RoundToInt(tower.cost * 1.3f).ToString();

            if (sellPriceText != null)
                sellPriceText.text = Mathf.RoundToInt(tower.cost * 0.6f).ToString();
        }

        contextMenuUp.SetActive(true);
        Vector2 adjustedPosition = GetAdjustedMenuPosition(screenPosition, contextMenuUp.GetComponent<RectTransform>());
        contextMenuUp.GetComponent<RectTransform>().localPosition = adjustedPosition;
    }

    void ShowContextMenuMaxUp(Vector2 screenPosition)
    {
        if (contextMenuMaxUp == null || selectedTower == null || inputBlocked) return;

        var tower = selectedTower.GetComponent<Tower>();
        if (tower == null) return;

        contextMenuMaxUp.transform.GetChild(1).GetComponent<TMP_Text>().text = ((int)Mathf.Round(tower.cost * 0.6f)).ToString();
        contextMenuMaxUp.SetActive(true);
        Vector2 adjustedPosition = GetAdjustedMenuPosition(screenPosition, contextMenuMaxUp.GetComponent<RectTransform>());
        contextMenuMaxUp.GetComponent<RectTransform>().localPosition = adjustedPosition;
    }

    public void BuildTower(GameObject towerPrefab)
    {
        if (inputBlocked || towerPrefab == null || selectedBuildPoint == null) return;

        var tower = towerPrefab.GetComponent<Tower>();
        if (tower == null) return;

        if (GameManager.Instance.money - tower.cost >= 0)
        {
            GameManager.Instance.ChangeMoney(-tower.cost);

            GameObject newTower = Instantiate(towerPrefab, selectedBuildPoint.transform.position, Quaternion.identity);

            if (isLevel1)
            {
                newTower.tag = "TowerMaxUp";
                Tower towerComponent = newTower.GetComponent<Tower>();
                if (towerComponent != null)
                {
                    towerComponent.nextTower = null;
                }
            }

            Destroy(selectedBuildPoint);
            contextMenu.SetActive(false);
        }
    }

    public void SellTower(GameObject BuildPoint)
    {
        if (inputBlocked || selectedTower == null || BuildPoint == null) return;

        var tower = selectedTower.GetComponent<Tower>();
        if (tower == null) return;

        GameManager.Instance.ChangeMoney((int)Mathf.Round(tower.cost * 0.6f));
        contextMenuUp.SetActive(false);
        contextMenuMaxUp.SetActive(false);
        Destroy(selectedTower);
        Instantiate(BuildPoint, selectedTower.transform.position, Quaternion.identity);
    }

    public void TowerUp()
    {
        if (inputBlocked || selectedTower == null) return;

        var tower = selectedTower.GetComponent<Tower>();
        if (tower == null || tower.nextTower == null) return;

        int upgradeCost = (int)Mathf.Round(tower.cost * 1.3f);
        if (GameManager.Instance.money - upgradeCost >= 0)
        {
            GameManager.Instance.ChangeMoney(-upgradeCost);
            contextMenuUp.SetActive(false);
            Destroy(selectedTower);
            Instantiate(tower.nextTower, selectedTower.transform.position, Quaternion.identity);
        }
    }
}
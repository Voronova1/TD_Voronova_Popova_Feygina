using UnityEngine;
using TMPro;

public class LevelTowerInfo : MonoBehaviour
{
    [Header("Настройки для этого уровня")]
    public bool showPanel = true;
    [TextArea(3, 10)]
    public string infoText = "Тут описание башни...";

    [Header("Ссылки (перетащите вручную)")]
    public GameObject towerInfoPanel;
    public TMP_Text textUI;

    void Start()
    {
        if (showPanel)
        {
            textUI.text = infoText;
            towerInfoPanel.SetActive(true);
            Time.timeScale = 0f; 
        }
    }

    public void ClosePanel()
    {
        towerInfoPanel.SetActive(false);
        Time.timeScale = 1f;
    }
}
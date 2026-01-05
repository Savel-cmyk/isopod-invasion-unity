using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Database;

public class PlayerNameManager : MonoBehaviour
{
    public static string PlayerNickname { get; private set; }

    [Header("Name Input UI")]
    public GameObject nameInputPanel;
    public TMP_InputField nameInputField;
    public Button confirmNameButton;

    private const string PlayerNameKey = "PlayerNickname";

    void Start()
    {
        // Пробуем загрузить сохраненное имя
        PlayerNickname = PlayerPrefs.GetString(PlayerNameKey, "");

        if (string.IsNullOrEmpty(PlayerNickname))
        {
            // Если имени нет, показываем панель ввода
            ShowNameInputPanel();
        }
        else
        {
            // Имя уже есть, можно спрятать панель
            Debug.Log("Loaded player name: " + PlayerNickname);
            if (nameInputPanel != null) nameInputPanel.SetActive(false);
        }

        // Настраиваем кнопку подтверждения
        if (confirmNameButton != null)
        {
            confirmNameButton.onClick.AddListener(ConfirmPlayerName);
        }
    }

    void ShowNameInputPanel()
    {
        if (nameInputPanel != null)
        {
            nameInputPanel.SetActive(true);
            if (nameInputField != null) nameInputField.text = "";
        }
    }

    void ConfirmPlayerName()
    {
        string newName = nameInputField?.text.Trim();

        if (!string.IsNullOrEmpty(newName))
        {
            PlayerNickname = newName;
            PlayerPrefs.SetString(PlayerNameKey, PlayerNickname);
            PlayerPrefs.Save();

            Debug.Log("Player name saved: " + PlayerNickname);

            // Скрываем панель
            if (nameInputPanel != null) nameInputPanel.SetActive(false);
        }
        else
        {
            // Можно показать предупреждение, что имя не может быть пустым
            Debug.LogWarning("Please enter a valid name.");
        }
    }

    // Используйте этот метод для отправки счета
    public void SubmitScoreToFirebase(int score)
    {
        if (FirebaseLeaderboardManager.Instance != null)
        {
            FirebaseLeaderboardManager.Instance.SubmitScore(score, PlayerNickname);
        }
    }
}
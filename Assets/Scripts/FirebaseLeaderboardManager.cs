using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Firebase.Database;
using Firebase.Extensions; // Для работы с задачами (Task) в Unity

[Serializable]
public class LeaderboardEntry
{
    public string playerName;
    public int score;
    public string timestamp; // Для уникальности и сортировки по времени

    public LeaderboardEntry(string name, int playerScore)
    {
        playerName = name;
        score = playerScore;
        timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
    }
    // Пустой конструктор нужен для десериализации Firebase
    public LeaderboardEntry() { }
}

public class FirebaseLeaderboardManager : MonoBehaviour
{
    public static FirebaseLeaderboardManager Instance; // Синглтон для удобного доступа

    private DatabaseReference databaseReference;
    private bool isFirebaseInitialized = false;

    // Событие для уведомления об успешной загрузке данных
    public event Action<List<LeaderboardEntry>> OnLeaderboardLoaded;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // НЕ инициализируем Firebase здесь!
            // Ждем, когда FirebaseManager сделает это
            StartCoroutine(WaitForFirebaseInitialization());
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    IEnumerator WaitForFirebaseInitialization()
    {
        // Ждем пока FirebaseManager будет готов
        while (FirebaseManager.Instance == null || !FirebaseManager.Instance.IsFirebaseReady())
        {
            yield return null;
        }
        
        // Теперь можем использовать Firebase
        InitializeDatabase();
    }
    
    void InitializeDatabase()
    {
        // Ваш существующий код инициализации базы данных
        databaseReference = FirebaseDatabase.DefaultInstance.RootReference;
        isFirebaseInitialized = true;
        Debug.Log("Firebase Database ready");
    }

    //void InitializeFirebase()
    //{
    //    FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task => {
    //        var dependencyStatus = task.Result;
    //        if (dependencyStatus == DependencyStatus.Available)
    //        {
    //            // Firebase готов к использованию
    //            FirebaseApp app = FirebaseApp.DefaultInstance;
    //            databaseReference = FirebaseDatabase.DefaultInstance.RootReference;
    //            isFirebaseInitialized = true;
    //            Debug.Log("Firebase успешно инициализирован.");
    //        }
    //        else
    //        {
    //            Debug.LogError($"Не удалось разрешить все зависимости Firebase: {dependencyStatus}");
    //        }
    //    });
    //}

    public void SubmitScore(int score, string playerName = "Player")
    {
        if (!isFirebaseInitialized) return;

        string playerId = SystemInfo.deviceUniqueIdentifier;

        // Сначала загружаем текущий лучший результат
        databaseReference.Child("leaderboard").Child(playerId).GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result;
                int bestScore = 0;

                // Если запись уже существует, получаем текущий счёт
                if (snapshot.Exists && snapshot.HasChild("score"))
                {
                    string json = snapshot.GetRawJsonValue();
                    LeaderboardEntry existingEntry = JsonUtility.FromJson<LeaderboardEntry>(json);
                    bestScore = existingEntry?.score ?? 0;
                }

                // Сохраняем только если новый результат лучше
                if (score > bestScore)
                {
                    LeaderboardEntry entry = new LeaderboardEntry(playerName, score);

                    databaseReference.Child("leaderboard").Child(playerId)
                        .SetRawJsonValueAsync(JsonUtility.ToJson(entry))
                        .ContinueWithOnMainThread(setTask =>
                        {
                            if (setTask.IsCompleted)
                            {
                                Debug.Log($"New best score saved: {playerName} - {score} (was: {bestScore})");
                            }
                        });
                }
                else
                {
                    Debug.Log($"Score {score} not saved (best is {bestScore})");
                }
            }
        });
    }

    public void LoadTop10Leaderboard()
    {
        if (!isFirebaseInitialized) return;

        // Запрос: отсортировать по полю "score" по убыванию и взять первые 10
        Query top10Query = databaseReference.Child("leaderboard").OrderByChild("score").LimitToLast(10);

        top10Query.GetValueAsync().ContinueWithOnMainThread(task => {
            if (task.IsFaulted)
            {
                Debug.LogError("Ошибка загрузки таблицы лидеров.");
            }
            else if (task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result;
                List<LeaderboardEntry> leaderboardList = new List<LeaderboardEntry>();

                // Данные приходят в порядке возрастания, поэтому проходим с конца
                // Также можно сразу использовать OrderByChild("score").LimitToLast(10) в запросе,
                // но затем их нужно будет обратить для отображения (1 место = наибольший счёт).
                foreach (DataSnapshot childSnapshot in snapshot.Children)
                {
                    string json = childSnapshot.GetRawJsonValue();
                    LeaderboardEntry entry = JsonUtility.FromJson<LeaderboardEntry>(json);
                    if (entry != null)
                    {
                        leaderboardList.Add(entry);
                    }
                }

                // Сортируем список по убыванию счёта
                leaderboardList.Sort((a, b) => b.score.CompareTo(a.score));

                // Ограничиваем 10 записями
                int count = Mathf.Min(leaderboardList.Count, 10);
                List<LeaderboardEntry> top10List = leaderboardList.GetRange(0, count);

                Debug.Log($"Загружено {top10List.Count} записей.");
                // Вызываем событие с загруженным списком
                OnLeaderboardLoaded?.Invoke(top10List);
            }
        });
    }
}
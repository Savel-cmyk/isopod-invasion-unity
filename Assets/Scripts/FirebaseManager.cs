using UnityEngine;
using Firebase;
using Firebase.Crashlytics;
using Firebase.Extensions; // Важно для работы с Task в Unity

public class FirebaseManager : MonoBehaviour
{
    public static FirebaseManager Instance { get; private set; }

    [Header("Initialization Settings")]
    public bool enableCrashlytics = true;
    public bool enableAnalytics = true;

    private bool isFirebaseReady = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeFirebase();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void InitializeFirebase()
    {
        Debug.Log("Initializing Firebase...");

        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            var dependencyStatus = task.Result;

            if (dependencyStatus == DependencyStatus.Available)
            {
                // Firebase готов к использованию
                FirebaseApp app = FirebaseApp.DefaultInstance;
                isFirebaseReady = true;

                // Инициализируем Crashlytics
                InitializeCrashlytics();

                Debug.Log("Firebase initialized successfully");

                // Уведомляем другие системы, что Firebase готов
                OnFirebaseInitialized();
            }
            else
            {
                Debug.LogError($"Could not resolve all Firebase dependencies: {dependencyStatus}");
            }
        });
    }

    void InitializeCrashlytics()
    {
        if (!enableCrashlytics) return;

        // Включаем сбор крашей
        Crashlytics.IsCrashlyticsCollectionEnabled = true;

        // Устанавливаем ID пользователя (можно использовать device ID)
        string userId = SystemInfo.deviceUniqueIdentifier;
        Crashlytics.SetUserId(userId);

        // Логируем версию игры и устройство
        Crashlytics.SetCustomKey("Game_Version", Application.version);
        Crashlytics.SetCustomKey("Device_Model", SystemInfo.deviceModel);
        Crashlytics.SetCustomKey("OS", SystemInfo.operatingSystem);

        Debug.Log("Crashlytics initialized");
    }

    void OnFirebaseInitialized()
    {
        // Здесь можно уведомить другие системы
        // Например, можно запустить загрузку таблицы лидеров
    }

    public bool IsFirebaseReady()
    {
        return isFirebaseReady;
    }
}
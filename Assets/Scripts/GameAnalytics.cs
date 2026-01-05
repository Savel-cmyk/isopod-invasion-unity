using UnityEngine;
using Firebase.Crashlytics;

public static class GameAnalytics
{
    // === ОСНОВНЫЕ СОБЫТИЯ ИГРЫ ===

    public static void LogGameStart()
    {
        Crashlytics.Log("Game session started");
        Crashlytics.SetCustomKey("Last_Game_Start", System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
    }

    public static void LogBugKilled(int bugId, int totalKills)
    {
        Crashlytics.Log($"Bug killed: ID={bugId}, Total={totalKills}");
        Crashlytics.SetCustomKey("Total_Bugs_Killed", totalKills.ToString());
        Crashlytics.SetCustomKey("Last_Bug_Kill_Time", GetTimestamp());
    }

    public static void LogGameEnd(int score, string endReason)
    {
        string message = $"Game ended. Score: {score}, Reason: {endReason}";
        Crashlytics.Log(message);

        Crashlytics.SetCustomKey("Final_Score", score.ToString());
        Crashlytics.SetCustomKey("Game_End_Reason", endReason);
        Crashlytics.SetCustomKey("Game_Duration", GetTimestamp());

        // Статистика по лучшему результату
        int bestScore = PlayerPrefs.GetInt("BestScore", 0);
        if (score > bestScore)
        {
            Crashlytics.SetCustomKey("New_Best_Score", score.ToString());
        }
    }

    // === СОБЫТИЯ КОРОБОК ===

    public static void LogBoxDamaged(int boxId, float healthPercentage)
    {
        Crashlytics.Log($"Box damaged: ID={boxId}, Health={healthPercentage}%");
    }

    public static void LogBoxDestroyed(int boxId)
    {
        Crashlytics.Log($"Box destroyed: ID={boxId}");
    }

    // === ОШИБКИ И ПРОБЛЕМЫ ===

    public static void LogWarning(string warningMessage, string context)
    {
        Crashlytics.Log($"[WARNING] {warningMessage} | Context: {context}");
        Crashlytics.SetCustomKey($"Last_Warning_{context}", warningMessage);
    }

    public static void LogError(string errorMessage, string context)
    {
        Crashlytics.Log($"[ERROR] {errorMessage} | Context: {context}");
        Crashlytics.SetCustomKey($"Last_Error_{context}", errorMessage);

        // Можно отправить нефатальное исключение
        // Crashlytics.LogException(new System.Exception(errorMessage));
    }

    // === ПОЛЬЗОВАТЕЛЬСКИЕ ДЕЙСТВИЯ ===

    public static void LogButtonClick(string buttonName)
    {
        Crashlytics.Log($"Button clicked: {buttonName}");
    }

    public static void LogForceGameEnd()
    {
        Crashlytics.Log("Player forced game end");
        Crashlytics.SetCustomKey("Game_Force_Ended", "true");
    }

    // === ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ===

    private static string GetTimestamp()
    {
        return System.DateTime.Now.ToString("HH:mm:ss");
    }
}
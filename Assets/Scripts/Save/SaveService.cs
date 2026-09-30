using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

public enum SaveLoadStatus
{
    Ok,
    NotFound,
    Corrupted,          // JSON 파싱 실패 / 필수 필드 누락
    UnsupportedVersion,
    IoError
}

/// <summary>
/// 진행 세이브 파일(단일 슬롯) 입출력.
/// 쓰기는 임시 파일에 끝까지 기록한 뒤 교체하므로, 쓰기 도중 실패해도 기존 정상 파일은 그대로 남는다.
/// 읽기 실패를 새 게임으로 취급하거나 손상 파일을 자동으로 덮어쓰지 않는다. 판단은 호출자가 한다.
/// </summary>
public static class SaveService
{
    private const string FileName = "savegame.json";

    private static bool _busy;

    /// <summary>테스트용. 설정하면 persistentDataPath 대신 이 폴더를 사용한다.</summary>
    public static string DirectoryOverride { get; set; }

    public static string FilePath => Path.Combine(
        string.IsNullOrEmpty(DirectoryOverride) ? Application.persistentDataPath : DirectoryOverride, FileName);
    private static string TempPath => FilePath + ".tmp";
    private static string BackupPath => FilePath + ".bak";

    public static bool IsBusy => _busy;

    /// <summary>정상 파일(또는 교체 도중 남은 백업)이 존재하는지. 유효성은 확인하지 않는다.</summary>
    public static bool HasSaveFile()
    {
        return File.Exists(FilePath) || File.Exists(BackupPath);
    }

    public static SaveLoadStatus TryLoad(out GameSaveData data, out string error)
    {
        data = null;
        error = null;

        if (_busy)
        {
            error = "저장 작업이 진행 중입니다.";
            return SaveLoadStatus.IoError;
        }

        // 교체 도중 종료되어 정상 파일이 없고 백업만 남은 경우 백업을 사용한다.
        string path = File.Exists(FilePath) ? FilePath : (File.Exists(BackupPath) ? BackupPath : null);
        if (path == null)
            return SaveLoadStatus.NotFound;

        string json;
        try
        {
            json = File.ReadAllText(path, Encoding.UTF8);
        }
        catch (Exception e)
        {
            error = e.Message;
            return SaveLoadStatus.IoError;
        }

        return TryParse(json, out data, out error);
    }

    public static SaveLoadStatus TryParse(string json, out GameSaveData data, out string error)
    {
        data = null;
        error = null;

        if (string.IsNullOrWhiteSpace(json))
        {
            error = "세이브 파일이 비어 있습니다.";
            return SaveLoadStatus.Corrupted;
        }

        GameSaveData parsed;
        try
        {
            parsed = JsonUtility.FromJson<GameSaveData>(json);
        }
        catch (Exception e)
        {
            error = "세이브 파일을 읽을 수 없습니다: " + e.Message;
            return SaveLoadStatus.Corrupted;
        }

        if (parsed == null)
        {
            error = "세이브 파일을 읽을 수 없습니다.";
            return SaveLoadStatus.Corrupted;
        }

        if (parsed.schemaVersion <= 0 || parsed.schemaVersion > GameSaveData.CurrentSchemaVersion)
        {
            error = $"지원하지 않는 세이브 버전입니다. (파일 {parsed.schemaVersion}, 지원 {GameSaveData.CurrentSchemaVersion})";
            return SaveLoadStatus.UnsupportedVersion;
        }

        string validationError = Validate(parsed);
        if (validationError != null)
        {
            error = validationError;
            return SaveLoadStatus.Corrupted;
        }

        data = parsed;
        return SaveLoadStatus.Ok;
    }

    private static string Validate(GameSaveData data)
    {
        if (string.IsNullOrEmpty(data.runId)) return "세이브 식별자(runId)가 없습니다.";
        if (data.day < 1) return "일차 정보가 올바르지 않습니다.";
        if (data.checkpoint != GameSaveData.CheckpointDayStart) return $"지원하지 않는 재개 지점입니다: {data.checkpoint}";
        if (!data.introCompleted) return "시작 연출 완료 전의 세이브입니다.";
        if (data.wallets == null || data.ingredients == null || data.employees == null
            || data.menuSlots == null || data.placedTables == null || data.unlockedRecipeIds == null)
            return "필수 데이터가 누락되었습니다.";
        return null;
    }

    /// <summary>
    /// 세이브를 기록한다. 성공했을 때만 true.
    /// 임시 파일 기록 → 기존 파일을 백업으로 교체 → 백업 삭제 순서라, 어느 단계에서 실패해도 정상 파일 하나는 남는다.
    /// </summary>
    public static bool TryWrite(GameSaveData data, out string error)
    {
        error = null;

        if (data == null)
        {
            error = "저장할 데이터가 없습니다.";
            return false;
        }

        if (_busy)
        {
            error = "이미 저장 중입니다.";
            return false;
        }

        _busy = true;
        try
        {
            string json = JsonUtility.ToJson(data, true);
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath));

            using (FileStream stream = new FileStream(TempPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                byte[] bytes = new UTF8Encoding(false).GetBytes(json);
                stream.Write(bytes, 0, bytes.Length);
                stream.Flush(true);
            }

            if (File.Exists(FilePath))
            {
                try
                {
                    File.Replace(TempPath, FilePath, BackupPath, true);
                }
                catch (PlatformNotSupportedException)
                {
                    ReplaceByMove();
                }
            }
            else
            {
                File.Move(TempPath, FilePath);
            }

            if (File.Exists(BackupPath))
                File.Delete(BackupPath);

            return true;
        }
        catch (Exception e)
        {
            error = e.Message;
            TryDelete(TempPath);
            return false;
        }
        finally
        {
            _busy = false;
        }
    }

    // File.Replace를 지원하지 않는 플랫폼용. 정상 파일을 백업으로 옮긴 뒤 임시 파일을 올린다.
    private static void ReplaceByMove()
    {
        if (File.Exists(BackupPath))
            File.Delete(BackupPath);

        File.Move(FilePath, BackupPath);
        File.Move(TempPath, FilePath);
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
            // 임시 파일 정리 실패는 다음 저장 때 덮어쓰므로 무시한다.
        }
    }

    public static bool TryGetSummary(out SaveSummary summary, string goldCurrencyId = GameSession.GoldCurrencyId)
    {
        summary = default;
        if (TryLoad(out GameSaveData data, out _) != SaveLoadStatus.Ok)
            return false;

        summary.Day = data.day;
        summary.PlayTimeSeconds = data.playTimeSeconds;

        if (DateTime.TryParse(data.savedAtUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime saved))
            summary.SavedAtLocal = saved.ToLocalTime();

        foreach (WalletEntry wallet in data.wallets)
        {
            if (wallet != null && wallet.currencyId == goldCurrencyId)
                summary.Gold = wallet.amount;
        }

        return true;
    }
}

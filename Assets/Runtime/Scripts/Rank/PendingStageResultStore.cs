using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// 아직 서버에 못 보낸 판 결과를 판 번호마다 JSON 파일로 보관한다.
/// 서버가 계약 오류로 거절한 결과는 rejected 폴더로 옮겨 다시 보내지 않는다.
/// </summary>
internal sealed class PendingStageResultStore
{
    private const string PENDING_FOLDER = "pending_results";
    private const string REJECTED_FOLDER = "rejected_results";
    private const string EXTENSION = ".json";

    private string _pendingPath;
    private string _rejectedPath;

    // persistentDataPath는 MonoBehaviour 생성 중에 읽을 수 없어서 처음 쓸 때 계산한다.
    private string PendingPath => _pendingPath ??= Path.Combine(Application.persistentDataPath, PENDING_FOLDER);

    private string RejectedPath => _rejectedPath ??= Path.Combine(Application.persistentDataPath, REJECTED_FOLDER);

    public void Save(string runId, string json)
    {
        if (string.IsNullOrEmpty(runId))
        {
            return;
        }

        try
        {
            Directory.CreateDirectory(PendingPath);
            File.WriteAllText(GetPendingFile(runId), json);
        }
        catch (Exception exception)
        {
            Debug.LogError("판 결과를 보관하지 못했습니다. " + exception.Message);
        }
    }

    /// <summary>
    /// 보관 중인 판 번호 목록.
    /// </summary>
    public void CollectRunIds(List<string> runIds)
    {
        runIds.Clear();
        if (!Directory.Exists(PendingPath))
        {
            return;
        }

        foreach (var file in Directory.GetFiles(PendingPath, "*" + EXTENSION))
        {
            runIds.Add(Path.GetFileNameWithoutExtension(file));
        }
    }

    public bool TryLoad(string runId, out string json)
    {
        json = null;
        var path = GetPendingFile(runId);
        if (!File.Exists(path))
        {
            return false;
        }

        try
        {
            json = File.ReadAllText(path);
            return !string.IsNullOrEmpty(json);
        }
        catch (Exception exception)
        {
            Debug.LogError("보관한 판 결과를 읽지 못했습니다. " + exception.Message);
            return false;
        }
    }

    public void Delete(string runId)
    {
        var path = GetPendingFile(runId);
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    public void MoveToRejected(string runId)
    {
        var path = GetPendingFile(runId);
        if (!File.Exists(path))
        {
            return;
        }

        Directory.CreateDirectory(RejectedPath);
        var target = Path.Combine(RejectedPath, runId + EXTENSION);
        if (File.Exists(target))
        {
            File.Delete(target);
        }

        File.Move(path, target);
    }

    private string GetPendingFile(string runId)
    {
        return Path.Combine(PendingPath, runId + EXTENSION);
    }
}

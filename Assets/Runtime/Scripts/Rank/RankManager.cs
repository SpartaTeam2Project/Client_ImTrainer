using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 판 결과를 리더보드 서버로 보내는 유일한 창구. 먼저 로컬에 보관하고, 보내기에 성공하면 지운다.
/// 서버 경로가 없거나 보내지 못하면 남겨 두고 다음 제출이나 다음 실행 때 다시 보낸다.
/// </summary>
public class RankManager : BaseManager
{
    private readonly PendingStageResultStore _store = new PendingStageResultStore();
    private readonly ApiClient _apiClient = new ApiClient();
    private readonly List<string> _runIds = new List<string>();
    private bool _sending;

    public override UniTask InitializeAsync()
    {
        FlushPending();
        return base.InitializeAsync();
    }

    /// <summary>
    /// 판 결과를 요청 DTO로 바꿔 보관하고 보내기를 시도한다.
    /// </summary>
    public void Submit(StageResult result)
    {
        if (result == null)
        {
            return;
        }

        Managers.Instance.TryGetManager<ItemManager>(out var itemManager);
        var request = StageResultMapper.ToRequest(result, itemManager);
        _store.Save(request.runId, JsonUtility.ToJson(request));
        FlushPending();
    }

    /// <summary>
    /// 보관 중인 결과를 보낸다. 이미 보내는 중이거나 서버 경로가 없으면 넘어간다.
    /// </summary>
    public void FlushPending()
    {
        if (_sending || !ApiClient.HasStageResultPath)
        {
            return;
        }

        StartCoroutine(SendPendingRoutine());
    }

    private IEnumerator SendPendingRoutine()
    {
        _sending = true;
        _store.CollectRunIds(_runIds);
        for (var i = 0; i < _runIds.Count; i++)
        {
            var runId = _runIds[i];
            if (!_store.TryLoad(runId, out var json))
            {
                continue;
            }

            if (!TokenStorage.TryLoad(out var accessToken, out _))
            {
                break;
            }

            ApiResult response = null;
            yield return _apiClient.PostStageResult(accessToken, json, result => response = result);
            if (!HandleResponse(runId, response))
            {
                break;
            }
        }

        _sending = false;
    }

    /// <summary>
    /// 응답에 따라 보관 파일을 정리한다. 다음 결과를 계속 보내도 되면 true.
    /// </summary>
    private bool HandleResponse(string runId, ApiResult response)
    {
        if (response == null)
        {
            return false;
        }

        if (response.StatusCode >= 200 && response.StatusCode < 300)
        {
            _store.Delete(runId);
            return true;
        }

        if (response.StatusCode == 400 || response.StatusCode == 422)
        {
            Debug.LogError($"서버가 판 결과를 거절했습니다. runId={runId} body={response.Body}");
            _store.MoveToRejected(runId);
            return true;
        }

        // 네트워크 오류, 인증 만료, 서버 오류는 남겨 두고 다음에 다시 보낸다.
        Debug.LogWarning($"판 결과를 보내지 못해 보관합니다. status={response.StatusCode} runId={runId}");
        return false;
    }
}

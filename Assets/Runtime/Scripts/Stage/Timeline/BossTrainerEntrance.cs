using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 보스 클립이 트레이너 등장에 쓰는 그림.
/// </summary>
public struct BossTrainerEntranceCast
{
    public Sprite[] DownFrames;
    public Sprite SideFrame;
    public Sprite StandingFrame;
    public Sprite Exclamation;
    public BossTrainerVersusCast Versus;
}

/// <summary>
/// 카메라 위 트레이너가 네 걸음 반 멈춘 뒤 느낌표를 띄우고, 플레이어 높이까지 내려온다.
/// </summary>
public static class BossTrainerEntrance
{
    private const float STEP_COUNT = 4.5f;
    private const float STEP_DISTANCE = 0.55f;
    private const float SIDE_OFFSET = 5f;
    private const float OFFSCREEN_MARGIN = 1.2f;
    private const float TRAINER_SCALE = 1.5f;
    private const float CAMERA_RIGHT = 2.2f;
    private const float CAMERA_LIFT = 1.1f;
    private const float WALK_SPEED = 1.8f;
    private const float FRAME_SECONDS = 0.12f;
    private const float REVEAL_SECONDS = 0.6f;
    private const float EXCLAIM_SECONDS = 1.6f;
    private const float MARK_LIFT = 0.5f;
    private const int TRAINER_SORTING_ORDER = 11;
    private const int MARK_SORTING_ORDER = 14;
    private const string RIVAL_MUSIC_NAME = "RivalAppears";

    private static GameObject _root;
    private static SpriteRenderer _trainer;
    private static GameObject _mark;

    /// <summary>
    /// 1단계 연출을 한 번 재생하고, 트레이너가 플레이어를 보는 자세로 남긴다.
    /// </summary>
    public static async UniTask PlayAsync(int token, BossTrainerEntranceCast cast)
    {
        Stop();
        if (!HasFrames(cast.DownFrames) || cast.SideFrame == null || cast.Exclamation == null)
        {
            Debug.LogWarning("보스 트레이너 등장 그림이 비어 있습니다. 보스 SO에 아래 걷기, 왼쪽 아이들, 느낌표를 넣으세요.");
            return;
        }

        if (!TryGetPlayer(out var playerManager) || !TryGetCamera(out var cameraManager) || !cameraManager.HasView)
        {
            return;
        }

        var playerPosition = (Vector2)playerManager.PlayerTransform.position;
        playerManager.SetMovementLocked(true);
        playerManager.SetLookDirection(Vector2.up);
        SetWeaponsPaused(true);
        var start = new Vector2(playerPosition.x + SIDE_OFFSET, cameraManager.TopBound + OFFSCREEN_MARGIN);
        CreateVisuals(cast, start);
        var reveal = new Vector2(playerPosition.x + CAMERA_RIGHT, start.y - cameraManager.HalfHeight * 0.5f);
        await PanAsync(token, cameraManager, cameraManager.Position, reveal, REVEAL_SECONDS);
        if (!IsCurrent(token))
        {
            return;
        }

        var stopped = start + Vector2.down * (STEP_COUNT * STEP_DISTANCE);
        await WalkAsync(token, cast.DownFrames, start, stopped);
        if (!IsCurrent(token))
        {
            return;
        }

        _trainer.sprite = StandingSprite(cast);
        _mark.SetActive(true);
        PlayMusic(RIVAL_MUSIC_NAME);
        await UniTask.Delay(System.TimeSpan.FromSeconds(EXCLAIM_SECONDS));
        if (!IsCurrent(token))
        {
            return;
        }

        _mark.SetActive(false);
        var beside = new Vector2(playerPosition.x + SIDE_OFFSET, playerPosition.y);
        var heldLook = new Vector2(playerPosition.x + CAMERA_RIGHT, playerPosition.y + CAMERA_LIFT);
        var walkSeconds = Vector2.Distance(stopped, beside) / WALK_SPEED;
        var pan = PanAsync(token, cameraManager, reveal, heldLook, Mathf.Max(0.2f, walkSeconds));
        await WalkAsync(token, cast.DownFrames, stopped, beside);
        await pan;
        if (!IsCurrent(token))
        {
            return;
        }

        _trainer.sprite = cast.SideFrame;
        _trainer.flipX = false;
        playerManager.SetLookDirection(Vector2.right);
        cameraManager.SetLookPoint(heldLook);
        var startBattle = await BossTrainerVersus.PlayAsync(token, cast.Versus);
        if (!startBattle || !IsCurrent(token))
        {
            return;
        }

        Stop();
        BossArenaPlayback.StartFight(token);
    }

    /// <summary>
    /// 연출 그림과 이동 잠금, 카메라 고정을 치운다.
    /// </summary>
    public static void Stop()
    {
        BossTrainerVersus.Stop();
        if (_root != null)
        {
            Object.Destroy(_root);
        }

        _root = null;
        _trainer = null;
        _mark = null;
        if (TryGetPlayer(out var playerManager))
        {
            playerManager.SetMovementLocked(false);
        }

        if (TryGetCamera(out var cameraManager))
        {
            cameraManager.ClearLookPoint();
        }

        SetWeaponsPaused(false);
    }

    private static void CreateVisuals(BossTrainerEntranceCast cast, Vector2 position)
    {
        _root = new GameObject("BossTrainerEntrance");
        _trainer = _root.AddComponent<SpriteRenderer>();
        _trainer.sprite = StandingSprite(cast);
        _trainer.sortingOrder = TRAINER_SORTING_ORDER;
        _root.transform.position = position;
        _root.transform.localScale = new Vector3(TRAINER_SCALE, TRAINER_SCALE, 1f);

        var markObject = new GameObject("BossTrainerExclamation");
        markObject.transform.SetParent(_root.transform, false);
        markObject.transform.localPosition = new Vector3(0f, MARK_LIFT, 0f);
        var markScale = 0.5f / TRAINER_SCALE;
        markObject.transform.localScale = new Vector3(markScale, markScale, 1f);
        var mark = markObject.AddComponent<SpriteRenderer>();
        mark.sprite = cast.Exclamation;
        mark.sortingOrder = MARK_SORTING_ORDER;
        markObject.SetActive(false);
        _mark = markObject;
    }

    private static async UniTask PanAsync(int token, CameraManager cameraManager, Vector2 from, Vector2 to, float seconds)
    {
        var duration = Mathf.Max(0.05f, seconds);
        var elapsed = 0f;
        while (elapsed < duration)
        {
            if (!IsCurrent(token))
            {
                return;
            }

            elapsed += Time.deltaTime;
            var blend = Mathf.Clamp01(elapsed / duration);
            cameraManager.SetLookPoint(Vector2.Lerp(from, to, blend));
            await UniTask.Yield();
        }

        if (IsCurrent(token))
        {
            cameraManager.SetLookPoint(to);
        }
    }

    private static async UniTask WalkAsync(int token, Sprite[] frames, Vector2 from, Vector2 to)
    {
        var distance = Vector2.Distance(from, to);
        var duration = Mathf.Max(FRAME_SECONDS, distance / WALK_SPEED);
        var elapsed = 0f;
        while (elapsed < duration)
        {
            if (!IsCurrent(token) || _trainer == null)
            {
                return;
            }

            elapsed += Time.deltaTime;
            var blend = Mathf.Clamp01(elapsed / duration);
            _root.transform.position = Vector2.Lerp(from, to, blend);
            var frame = Mathf.FloorToInt(elapsed / FRAME_SECONDS) % frames.Length;
            _trainer.sprite = frames[frame];
            _trainer.flipX = false;
            await UniTask.Yield();
        }

        if (IsCurrent(token) && _root != null)
        {
            _root.transform.position = to;
        }
    }

    private static void PlayMusic(string musicName)
    {
        if (Managers.Instance == null || !Managers.Instance.TryGetManager<AudioManager>(out var audioManager))
        {
            return;
        }

        audioManager.PlayMusic(musicName);
    }

    private static void SetWeaponsPaused(bool paused)
    {
        if (Managers.Instance == null || !Managers.Instance.TryGetManager<WeaponAbilityManager>(out var abilityManager))
        {
            return;
        }

        abilityManager.SetWeaponsPaused(paused);
    }

    private static bool IsCurrent(int token)
    {
        return BossArenaPlayback.IsCurrent(token);
    }

    private static Sprite StandingSprite(BossTrainerEntranceCast cast)
    {
        if (cast.StandingFrame != null)
        {
            return cast.StandingFrame;
        }

        return cast.DownFrames[0];
    }

    private static bool HasFrames(Sprite[] frames)
    {
        return frames != null && frames.Length > 0 && frames[0] != null;
    }

    private static bool TryGetPlayer(out PlayerManager playerManager)
    {
        playerManager = null;
        if (Managers.Instance == null || !Managers.Instance.TryGetManager(out playerManager))
        {
            return false;
        }

        return playerManager.PlayerTransform != null;
    }

    private static bool TryGetCamera(out CameraManager cameraManager)
    {
        cameraManager = null;
        return Managers.Instance != null && Managers.Instance.TryGetManager(out cameraManager);
    }
}

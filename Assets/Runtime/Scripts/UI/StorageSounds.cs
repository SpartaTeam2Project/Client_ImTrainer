/// <summary>
/// 스토리지 창의 배경음과 메뉴 효과음을 낸다.
/// </summary>
public static class StorageSounds
{
    private const string MENU_MOVE_SOUND = "cursor";
    private const string FILTER_APPLY_SOUND = "select";
    private const string ENTRY_LOCKED_SOUND = "error";
    private const string ENTRY_PICK_UP_SOUND = "storage_entry_pick_up";
    private const string ENTRY_PICK_DOWN_SOUND = "storage_entry_pick_down";
    private const string GAME_START_SOUND = "game_start";
    private const string STORAGE_MUSIC_NAME = "storage";

    public static void PlayMusic()
    {
        if (TryGetAudio(out var audioManager))
        {
            audioManager.PlayMusic(STORAGE_MUSIC_NAME);
        }
    }

    public static void PlayCursor()
    {
        PlaySound(MENU_MOVE_SOUND);
    }

    public static void PlaySelect()
    {
        PlaySound(FILTER_APPLY_SOUND);
    }

    public static void PlayEntryLocked()
    {
        PlaySound(ENTRY_LOCKED_SOUND);
    }

    public static void PlayEntryPickUp()
    {
        PlaySound(ENTRY_PICK_UP_SOUND);
    }

    public static void PlayEntryPickDown()
    {
        PlaySound(ENTRY_PICK_DOWN_SOUND);
    }

    public static void PlayGameStart()
    {
        PlaySound(GAME_START_SOUND);
    }

    private static void PlaySound(string soundName)
    {
        if (TryGetAudio(out var audioManager))
        {
            audioManager.PlaySound(soundName);
        }
    }

    private static bool TryGetAudio(out AudioManager audioManager)
    {
        audioManager = null;
        return Managers.Instance != null && Managers.Instance.TryGetManager<AudioManager>(out audioManager);
    }
}

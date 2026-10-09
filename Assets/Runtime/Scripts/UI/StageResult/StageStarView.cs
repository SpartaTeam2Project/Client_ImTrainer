using UnityEngine.UI;

/// <summary>
/// 성 이미지 배열에서 앞쪽 count개만 켠다.
/// </summary>
public static class StageStarView
{
    public static void Show(Image[] stars, int count)
    {
        if (stars == null)
        {
            return;
        }

        for (var i = 0; i < stars.Length; i++)
        {
            if (stars[i] != null)
            {
                stars[i].enabled = i < count;
            }
        }
    }
}

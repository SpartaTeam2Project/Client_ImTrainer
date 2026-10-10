using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 셰이더 변형 묶음을 프레임마다 조금씩 데워 첫 사용 때 생기는 끊김을 없앤다.
/// </summary>
public static class ShaderWarmup
{
    private const int VARIANTS_PER_FRAME = 8;

    /// <summary>
    /// 묶음을 모두 데울 때까지 기다린다. 이미 데운 묶음은 바로 끝난다.
    /// </summary>
    public static async UniTask WarmUpAsync(ShaderVariantCollection variants, CancellationToken cancellationToken)
    {
        if (variants == null)
        {
            Debug.LogWarning("[ShaderWarmup] 셰이더 변형 묶음이 없어 워밍업을 건너뜀");
            return;
        }

        while (!variants.isWarmedUp)
        {
            // 남은 게 없으면 true를 돌려준다.
            if (variants.WarmUpProgressively(VARIANTS_PER_FRAME))
            {
                break;
            }

            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
        }
    }
}

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 배경 칸에 깔리는 장식. 펜스 안에서는 녹아서 사라진 뒤 비활성화한다.
/// </summary>
public class StageProp : MonoBehaviour
{
    private const float DISSOLVE_DURATION = 0.5f;
    private const string DISSOLVE_MATERIAL_PATH = "Materials/SpriteDissolve";

    private static readonly int DISSOLVE_ID = Shader.PropertyToID("_Dissolve");
    private static Material _dissolveTemplate;

    [SerializeField] private SpriteRenderer _sprite;

    private Material _sharedMaterial;
    private Material _effectsMaterial;
    private CancellationTokenSource _dissolveCancellation;

    #region Unity Methods

    private void Awake()
    {
        if (_sprite == null)
        {
            _sprite = GetComponent<SpriteRenderer>();
        }

        if (_sprite != null)
        {
            _sharedMaterial = _sprite.sharedMaterial;
        }
    }

    private void OnDisable()
    {
        CancelDissolve();
        RestoreSharedMaterial();
    }

    private void OnDestroy()
    {
        if (_effectsMaterial != null)
        {
            Destroy(_effectsMaterial);
            _effectsMaterial = null;
        }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// 가장자리부터 깎이며 사라진 뒤 끈다. 풀이 비활성 객체를 다시 쓴다.
    /// </summary>
    public void Dissolve()
    {
        CancelDissolve();
        _dissolveCancellation = new CancellationTokenSource();
        DissolveAsync(_dissolveCancellation.Token).Forget();
    }

    #endregion

    #region Private Methods

    private async UniTaskVoid DissolveAsync(CancellationToken cancellationToken)
    {
        if (_sprite == null || !TryGetEffectsMaterial(out var effectsMaterial))
        {
            gameObject.SetActive(false);
            return;
        }

        _sprite.material = effectsMaterial;
        effectsMaterial.SetFloat(DISSOLVE_ID, 0f);

        var elapsed = 0f;
        try
        {
            while (elapsed < DISSOLVE_DURATION)
            {
                cancellationToken.ThrowIfCancellationRequested();
                elapsed += Time.deltaTime;
                var progress = Mathf.SmoothStep(0f, 1f, elapsed / DISSOLVE_DURATION);
                effectsMaterial.SetFloat(DISSOLVE_ID, progress);
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }

            effectsMaterial.SetFloat(DISSOLVE_ID, 1f);
            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        effectsMaterial.SetFloat(DISSOLVE_ID, 0f);
        RestoreSharedMaterial();
        gameObject.SetActive(false);
    }

    private bool TryGetEffectsMaterial(out Material effectsMaterial)
    {
        if (_effectsMaterial == null)
        {
            var template = GetDissolveTemplate();
            _effectsMaterial = template == null ? null : new Material(template);
        }

        effectsMaterial = _effectsMaterial;
        return effectsMaterial != null;
    }

    private static Material GetDissolveTemplate()
    {
        if (_dissolveTemplate == null)
        {
            _dissolveTemplate = Resources.Load<Material>(DISSOLVE_MATERIAL_PATH);
        }

        return _dissolveTemplate;
    }

    private void RestoreSharedMaterial()
    {
        if (_sprite == null || _sharedMaterial == null)
        {
            return;
        }

        _sprite.sharedMaterial = _sharedMaterial;
    }

    private void CancelDissolve()
    {
        if (_dissolveCancellation == null)
        {
            return;
        }

        _dissolveCancellation.Cancel();
        _dissolveCancellation.Dispose();
        _dissolveCancellation = null;
    }

    #endregion
}

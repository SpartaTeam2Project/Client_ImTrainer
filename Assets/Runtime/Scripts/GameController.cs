using UnityEngine;

public class GameController : MonoBehaviour
{
    /// <summary>
    /// Playing 상태 동안 쌓인 초.
    /// </summary>
    public float ElapsedSeconds { get; private set; }

    private void Update()
    {
        if (Managers.Instance == null || !Managers.Instance.IsSimulationRunning)
        {
            return;
        }

        ElapsedSeconds += Time.deltaTime;
    }
}

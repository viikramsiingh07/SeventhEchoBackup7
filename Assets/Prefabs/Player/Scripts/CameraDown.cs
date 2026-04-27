using UnityEngine;
using Unity.Cinemachine;

public class CameraLowerRock : MonoBehaviour
{
    public float cameraLowerAmount = -3f;
    public float resetDelay = 1.5f;

    private CinemachineCamera vcam;
    private CinemachinePositionComposer composer;

    private void Start()
    {
        vcam = FindFirstObjectByType<CinemachineCamera>();
        if (vcam != null)
            composer = vcam.GetComponent<CinemachinePositionComposer>();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.gameObject.CompareTag("Player")) return;
        if (composer == null) return;

        composer.TargetOffset = new Vector3(0f, cameraLowerAmount, 0f);
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (!collision.gameObject.CompareTag("Player")) return;
        if (composer == null) return;

        composer.TargetOffset = Vector3.zero;
    }
}
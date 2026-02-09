using UnityEngine;

public class CameraFollowXY : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform player;

    [Header("X Follow")]
    [SerializeField] private float xSmoothTime = 0.15f;

    [Header("Y Follow")]
    [SerializeField] private float ySmoothTime = 0.3f;

    [Header("Y Thresholds")]
    [SerializeField] private float upwardThreshold = 0.8f;
    [SerializeField] private float downwardThreshold = 1.2f;

    private float xVelocity;
    private float yVelocity;

    private float cameraAnchorY;
    private float highestPlayerY;

    private void Start()
    {
        cameraAnchorY = transform.position.y;
        highestPlayerY = player.position.y;
    }

    private void LateUpdate()
    {
        if (player == null)
            return;

        Vector3 pos = transform.position;

        // ---------------- X FOLLOW ----------------
        pos.x = Mathf.SmoothDamp(
            pos.x,
            player.position.x,
            ref xVelocity,
            xSmoothTime
        );

        // ---------------- TRACK HIGHEST POINT ----------------
        if (player.position.y > highestPlayerY)
        {
            highestPlayerY = player.position.y;
        }

        // ---------------- UPWARD CAMERA MOVE ----------------
        if (highestPlayerY > cameraAnchorY + upwardThreshold)
        {
            cameraAnchorY = highestPlayerY - upwardThreshold;
        }

        // ---------------- DOWNWARD CAMERA MOVE (CONTROLLED) ----------------
        float playerBelowCamera = cameraAnchorY - player.position.y;

        if (playerBelowCamera > downwardThreshold)
        {
            cameraAnchorY = player.position.y + downwardThreshold;
            highestPlayerY = player.position.y;
        }

        pos.y = Mathf.SmoothDamp(
            pos.y,
            cameraAnchorY,
            ref yVelocity,
            ySmoothTime
        );

        transform.position = pos;
    }
}

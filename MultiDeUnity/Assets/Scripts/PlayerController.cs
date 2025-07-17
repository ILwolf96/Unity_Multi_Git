using Fusion;
using UnityEngine;

public class PlayerController : NetworkBehaviour
{
    [Networked] private Vector3 NetworkedPosition { get; set; }
    [Networked] private Quaternion NetworkedRotation { get; set; }

    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 360f;

    private Vector3 inputDirection;
    private Camera mainCamera;

    public override void Spawned()
    {
        NetworkedPosition = transform.position;
        NetworkedRotation = transform.rotation;

        if (Object.HasInputAuthority)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
                Debug.LogWarning("Main Camera not found!");
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority)
        {
            transform.position = NetworkedPosition;
            transform.rotation = NetworkedRotation;
            return;
        }

        if (Object.HasInputAuthority)
        {
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            Vector2 input = new Vector2(h, v);

            if (input.sqrMagnitude > 1f)
                input.Normalize();

            if (mainCamera != null)
            {
                Vector3 camForward = mainCamera.transform.forward;
                camForward.y = 0;
                camForward.Normalize();

                Vector3 camRight = mainCamera.transform.right;
                camRight.y = 0;
                camRight.Normalize();

                inputDirection = (camForward * input.y + camRight * input.x).normalized;
            }
            else
            {
                inputDirection = new Vector3(input.x, 0, input.y);
            }
        }

        Vector3 moveDelta = inputDirection * moveSpeed * Runner.DeltaTime;
        NetworkedPosition += moveDelta;

        if (inputDirection.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(inputDirection);
            NetworkedRotation = Quaternion.RotateTowards(NetworkedRotation, targetRot, rotationSpeed * Runner.DeltaTime);
        }

        transform.position = NetworkedPosition;
        transform.rotation = NetworkedRotation;
    }
}

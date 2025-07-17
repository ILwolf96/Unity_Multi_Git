using Fusion;
using UnityEngine;

public class Projectile : NetworkBehaviour
{
    [Networked] private Vector3 NetworkedPosition { get; set; }

    private Vector3 velocity;
    private float timer;
    public float lifetime = 3f;

    private bool hasInitialized = false;

    public void Initialize(Vector3 velocity)
    {
        this.velocity = velocity;
        timer = 0f;
        if (hasInitialized)
        {
            NetworkedPosition = transform.position;
        }
    }

    public override void Spawned()
    {
        hasInitialized = true;
        NetworkedPosition = transform.position;
    }

    public override void FixedUpdateNetwork()
    {
        if (!hasInitialized)
            return;

        if (Object.HasStateAuthority)
        {
            NetworkedPosition += velocity * Runner.DeltaTime;
            timer += Runner.DeltaTime;

            if (timer > lifetime)
            {
                Runner.Despawn(Object);
                return;
            }
        }

        transform.position = NetworkedPosition;
        transform.rotation = Quaternion.LookRotation(velocity);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (Object.HasStateAuthority)
        {
            Runner.Despawn(Object);
        }
    }
}

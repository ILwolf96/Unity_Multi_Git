using Fusion;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class Projectile : NetworkBehaviour // ye... so about that, it was removed, idc, too much trouble, left it in cuz afrid of breaking the project
{
    [Networked] private Vector3 NetworkedPosition { get; set; }

    [Networked]
    public int NetworkedCharacterIndex { get; set; }

    [SerializeField] private Material[] projectileMaterials;

    private Vector3 velocity;
    private float timer;
    public float lifetime = 3f;

    private bool hasInitialized = false;

    public void Initialize(Vector3 velocity, int characterIndex)
    {
        this.velocity = velocity;
        this.timer = 0f;

        if (Object.HasStateAuthority)
        {
            NetworkedCharacterIndex = characterIndex;
        }

        hasInitialized = true;
        NetworkedPosition = transform.position;
    }

    public override void Spawned()
    {
        NetworkedPosition = transform.position;
        hasInitialized = true;
        ApplyProjectileMaterial(NetworkedCharacterIndex);
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

    public override void Render()
    {
        ApplyProjectileMaterial(NetworkedCharacterIndex);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (Object.HasStateAuthority)
        {
            Runner.Despawn(Object);
        }
    }

    private void ApplyProjectileMaterial(int charIndex)
    {
        if (projectileMaterials == null || charIndex < 0 || charIndex >= projectileMaterials.Length)
        {
            return;
        }

        Renderer projRend = GetComponentInChildren<Renderer>();
        if (projRend != null)
        {
            projRend.material = projectileMaterials[charIndex];
        }
        else
        {
        }
    }
}
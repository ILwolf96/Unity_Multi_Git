using UnityEngine;
using Fusion;

[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(Collider))]
public class Projectile : NetworkBehaviour
{
    [SerializeField] private float speed = 10f;
    [SerializeField] private float lifetime = 3f;

    private float _spawnTime;
    private Vector3 _direction;
    private Material _projectileMaterial;

    private Renderer _renderer;

    public override void Spawned()
    {
        _spawnTime = Time.time;

        _renderer = GetComponent<Renderer>();
        if (_renderer != null && _projectileMaterial != null)
        {
            _renderer.material = _projectileMaterial;
        }
    }

    public void Initialize(Vector3 direction, Material inheritedMaterial)
    {
        _direction = direction.normalized;
        _projectileMaterial = inheritedMaterial;
    }

    private void Update()
    {
        if (Object.HasStateAuthority)
        {
            transform.position += _direction * speed * Time.deltaTime;

            if (Time.time - _spawnTime >= lifetime)
            {
                Runner.Despawn(Object);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!Object.HasStateAuthority) return;

        Runner.Despawn(Object);
    }
}

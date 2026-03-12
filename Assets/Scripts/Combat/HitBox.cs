using UnityEngine;

/// <summary>
/// Componente de hitbox para armas / ataques.
/// Ativa/desativa durante animações de ataque.
/// </summary>
[RequireComponent(typeof(Collider))]
public class HitBox : MonoBehaviour
{
    [Header("Configurações de Dano")]
    public float baseDamage = 25f;
    public LayerMask targetLayers;
    
    private Collider hitCollider;
    private bool isActive;

    private void Awake()
    {
        hitCollider = GetComponent<Collider>();
        hitCollider.isTrigger = true;
        DisableHitBox();
    }

    public void EnableHitBox()
    {
        isActive = true;
        hitCollider.enabled = true;
    }

    public void DisableHitBox()
    {
        isActive = false;
        hitCollider.enabled = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isActive) return;

        if (((1 << other.gameObject.layer) & targetLayers) != 0)
        {
            IDamageable damageable = other.GetComponent<IDamageable>();
            if (damageable == null)
                damageable = other.GetComponentInParent<IDamageable>();

            if (damageable != null)
            {
                DamageSystem.ApplyDamage(damageable, baseDamage);
            }
        }
    }
}

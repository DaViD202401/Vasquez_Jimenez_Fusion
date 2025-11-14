using Fusion;
using UnityEngine;

public class Ball : NetworkBehaviour
{
    [Networked] private TickTimer life { get; set; }
    [SerializeField] private float hitRadius = 0.5f;
    [SerializeField] private int damage = 1;    

    public void Init()
    {
        life = TickTimer.CreateFromSeconds(Runner, 5.0f);
    }

    public override void FixedUpdateNetwork()
    {
        if (life.Expired(Runner))
        {
            Runner.Despawn(Object);
            return;
        }

        if (HasStateAuthority)
        {
            var hits = Physics.OverlapSphere(transform.position, hitRadius);
            foreach (var col in hits)
            {
                var netObj = col.GetComponentInParent<NetworkObject>();
                if (netObj != null && netObj != Object && netObj.TryGetComponent<Player>(out Player player))
                {
                    player.RPC_ApplyDamage(damage);
                    Runner.Despawn(Object);
                    return;
                }
            }
        }

        transform.position += 5 * transform.forward * Runner.DeltaTime;
    }
}
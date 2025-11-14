using Fusion;
using UnityEngine;
using Cinemachine; // si lo usas para crear la camera local en Spawned

public class Player : NetworkBehaviour
{
    [SerializeField] private Ball _prefabBall;
    [SerializeField] private GameObject _cinemachineFreeLookPrefab;

    [Networked] private TickTimer delay { get; set; }
    [Networked] private Color playerColor { get; set; }
    [Networked] private int Health { get; set; }
    [Networked] public int HitCount { get; set; }

    private NetworkCharacterController _cc;
    private Vector3 _forward;
    private Material _material;
    private GameObject _camInstance;

    private void Awake()
    {
        _cc = GetComponent<NetworkCharacterController>();
        _forward = transform.forward;
        _material = GetComponentInChildren<MeshRenderer>().material;
    }

    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            playerColor = new Color(Random.value, Random.value, Random.value);
            Health = 3;
            HitCount = 0;
        }

        if (Object.HasInputAuthority && _cinemachineFreeLookPrefab != null)
        {
            _camInstance = Instantiate(_cinemachineFreeLookPrefab);
            var vcam = _camInstance.GetComponent<Cinemachine.CinemachineFreeLook>();
            if (vcam != null)
            {
                Transform target = transform.Find("CameraTarget");
                StartCoroutine(SetupCinemachineDelayed(vcam, target));
            }
        }
    }

    private System.Collections.IEnumerator SetupCinemachineDelayed(Cinemachine.CinemachineFreeLook vcam, Transform target)
    {
        // Esperar 1 frame para seguridad con Fusion
        yield return null;
        if (vcam != null && target != null)
        {
            vcam.Follow = target;
            vcam.LookAt = target;
        }
    }

    public override void Render()
    {
        if (_material != null)
            _material.color = playerColor;
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_ApplyDamage(int amount, RpcInfo info = default)
    {
        if (!HasStateAuthority) return;

        Health -= amount;
        HitCount += amount;

        if (HitCount >= 3)
            Runner.Shutdown();

        if (Health <= 0)
            Runner.Despawn(Object);
    }

    // --- FIX: no hacer early return por HasInputAuthority.
    public override void FixedUpdateNetwork()
    {
        if (!GetInput(out NetworkInputData data))
            return; // si no hay input, nada que hacer

        // data.direction viene ya en WORLD SPACE (cliente lo calculó)
        Vector3 move = data.direction;

        // seguridad: normalizar si es mayor a 1
        if (move.sqrMagnitude > 1f)
            move.Normalize();

        // si hay movimiento, actualizar forward para spawn/shot direction
        if (move.sqrMagnitude > 0.001f)
            _forward = move;

        // aplicar movimiento con NetworkCharacterController
        _cc.Move(5f * move * Runner.DeltaTime);

        // rotar suavemente hacia la dirección de movimiento (si hay)
        if (move.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(move),
                Runner.DeltaTime * 10f
            );
        }

        // disparo / cooldown (state authority)
        if (HasStateAuthority && delay.ExpiredOrNotRunning(Runner))
        {
            if (data.buttons.IsSet(NetworkInputData.MOUSEBUTTON0))
            {
                delay = TickTimer.CreateFromSeconds(Runner, 0.5f);

                Runner.Spawn(
                    _prefabBall,
                    transform.position + _forward,
                    Quaternion.LookRotation(_forward),
                    Object.InputAuthority,
                    (runner, o) =>
                    {
                        o.GetComponent<Ball>().Init();
                    }
                );
            }
        }
    }

    private void OnDestroy()
    {
        if (_camInstance != null)
            Destroy(_camInstance);
    }
}

using Fusion;
using UnityEngine;
using Cinemachine;

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
        // Solo el StateAuthority inicializa valores
        if (HasStateAuthority)
        {
            playerColor = new Color(Random.value, Random.value, Random.value);
            Health = 3;
            HitCount = 0;
        }

        // Mostrar UI solo para el jugador local
        if (Object.HasInputAuthority)
        {
            UIManager.Instance?.SetHealth(Health);
        }

        // Instanciar cámara únicamente para mi jugador
        if (Object.HasInputAuthority && _cinemachineFreeLookPrefab != null)
        {
            _camInstance = Instantiate(_cinemachineFreeLookPrefab);
            var vcam = _camInstance.GetComponent<CinemachineFreeLook>();

            if (vcam != null)
            {
                Transform target = transform.Find("CameraTarget");
                StartCoroutine(SetupCinemachineDelayed(vcam, target));
            }
        }
    }

    private System.Collections.IEnumerator SetupCinemachineDelayed(CinemachineFreeLook vcam, Transform target)
    {
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

        // Actualiza UI SOLO del jugador local
        if (Object.HasInputAuthority)
            UIManager.Instance?.SetHealth(Health);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_ApplyDamage(int amount, RpcInfo info = default)
    {
        if (!HasStateAuthority) return;

        Health -= amount;
        HitCount += amount;

        if (Health <= 0)
            Runner.Despawn(Object);

        if (HitCount >= 3)
            Runner.Shutdown();
    }

    public override void FixedUpdateNetwork()
    {
        if (!GetInput(out NetworkInputData data))
            return;

        Vector3 move = data.direction;

        if (move.sqrMagnitude > 1f)
            move.Normalize();

        if (move.sqrMagnitude > 0.001f)
            _forward = move;

        _cc.Move(5f * move * Runner.DeltaTime);

        if (move.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(move),
                Runner.DeltaTime * 10f
            );
        }

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

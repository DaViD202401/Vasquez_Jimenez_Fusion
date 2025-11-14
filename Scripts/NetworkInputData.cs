using Fusion;
using UnityEngine;

public struct NetworkInputData : INetworkInput
{
    public const byte MOUSEBUTTON0 = 1;
    public NetworkButtons buttons;

    // Dirección en WORLD SPACE (calculada en cliente a partir de la cámara)
    public Vector3 direction;
}

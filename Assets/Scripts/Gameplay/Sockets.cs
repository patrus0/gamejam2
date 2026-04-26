using UnityEngine;
using UnityEngine.Events;
using System;

public class Socket : MonoBehaviour
{
    [SerializeField] private string socketID;          // ID гнезда (например "A1")
    [SerializeField] private Transform plugPosition;   // куда именно ставить штырь (опционально)
    [SerializeField] private UnityEvent<Pin, bool> onPinAttempt; // событие при попытке вставки

    private Pin currentPin = null;
    public static event Action<Pin, Socket> PinPlugged;
    public string SocketID => socketID;
    public bool IsOccupied => currentPin != null;
    public Transform PlugPosition => plugPosition;

    public bool AttemptPlug(Pin pin)
    {
        if (IsOccupied) return false;
        pin.PlugInto(this);
        currentPin = pin;
        onPinAttempt?.Invoke(pin, true); // или false, если позже понадобится различать
        PinPlugged?.Invoke(pin, this);
        return true;
    }

    public void UnplugPin(Pin pin)
    {
        if (currentPin == pin) currentPin = null;
    }
}
using UnityEngine;
using UnityEngine.Events;

public class Socket : MonoBehaviour
{
    [SerializeField] private string socketID;          // ID гнезда (например "A1")
    [SerializeField] private Transform plugPosition;   // куда именно ставить штырь (опционально)
    [SerializeField] private UnityEvent<Pin, bool> onPinAttempt; // событие при попытке вставки

    private Pin currentPin = null;
    public string SocketID => socketID;
    public bool IsOccupied => currentPin != null;
    public Transform PlugPosition => plugPosition;

    public bool AttemptPlug(Pin pin)
    {
        if (IsOccupied) return false;
        bool isCorrect = (pin.PinID == socketID);
        onPinAttempt?.Invoke(pin, isCorrect);
        if (isCorrect)
        {
            pin.PlugInto(this);
            currentPin = pin;
            return true;
        }
        return false;
    }

    public void UnplugPin(Pin pin)
    {
        if (currentPin == pin) currentPin = null;
    }
}
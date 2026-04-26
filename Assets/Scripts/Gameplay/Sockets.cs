
using UnityEngine.Events;
using System;
using UnityEngine;

public class Socket : MonoBehaviour
{
    [SerializeField] private string socketID;
    private bool isOccupied = false;

    public string SocketID => socketID;
    public bool IsOccupied => isOccupied;

    public static event Action<Pin, Socket> PinPlugged; //Удалить, когда разберёмся с кодом Notebook

    public void SetOccupied(bool state)
    {
        isOccupied = state;
    }
}
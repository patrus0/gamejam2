using UnityEngine;
using System.Collections.Generic;
using System;

[System.Serializable]
public struct CustomerData
{
    public string fullName;     // ФИО
    public string targetSocket; // Какой сокет ему нужен
}

public struct CallData
{
    public string fullName;
    public string targetSocket;
    public string pinID;

    public CallData(string fullName, string targetSocket, string pinID)
    {
        this.fullName = fullName;
        this.targetSocket = targetSocket;
        this.pinID = pinID;
    }
}

public class CallManager : MonoBehaviour
{
    public static event Action<CallData> CallStarted;

    [Header("База данных")]
    [SerializeField] private List<CustomerData> customers;
    
    [Header("Настройки звонков")]
    [SerializeField] private float minInterval = 5f;
    [SerializeField] private float maxInterval = 15f;
    
    [Header("Текущий активный вызов (Для отладки)")]
    public string currentCustomerName;
    public string currentRequiredSocket;
    public string currentRequiredPin;

    private List<Lamp> allLamps = new List<Lamp>();
    private float nextCallTime;
    private string lastCustomerName = string.Empty;
    public IReadOnlyList<CustomerData> Customers => customers;

    private void Start()
    {
        // Находим все лампы в начале
        allLamps.AddRange(FindObjectsByType<Lamp>(FindObjectsSortMode.None));
        SetNextCallTime();
    }

    private void Update()
    {
        if (Time.time >= nextCallTime)
        {
            TryMakeCall();
            SetNextCallTime();
        }
    }

    void SetNextCallTime()
    {
        nextCallTime = Time.time + UnityEngine.Random.Range(minInterval, maxInterval);
    }

    void TryMakeCall()
    {
        // Ищем свободные лампы (состояние Off)
        List<Lamp> freeLamps = allLamps.FindAll(l => l.CurrentState == Lamp.State.Off);

        if (freeLamps.Count > 0 && customers.Count > 0)
        {
            // Выбираем случайную свободную лампу
            Lamp selectedLamp = freeLamps[UnityEngine.Random.Range(0, freeLamps.Count)];
            // Выбираем случайного клиента
            int customerIndex = GetNextCustomerIndex();
            CustomerData data = customers[customerIndex];
            lastCustomerName = data.fullName ?? string.Empty;

            // Обновляем публичные поля для вывода
            currentCustomerName = data.fullName;
            currentRequiredSocket = data.targetSocket;
            currentRequiredPin = selectedLamp.LinkedPinID;

            Debug.Log($"<color=yellow>[ЗВОНОК]</color> Клиент: {data.fullName} просит сокет {data.targetSocket} на линии {selectedLamp.LinkedPinID}");

            // Запускаем звонок
            selectedLamp.StartIncomingCall(data.targetSocket);
            CallStarted?.Invoke(new CallData(data.fullName, data.targetSocket, selectedLamp.LinkedPinID));
        }
    }

    private int GetNextCustomerIndex()
    {
        if (customers.Count <= 1)
            return 0;

        int guard = 0;
        int index = UnityEngine.Random.Range(0, customers.Count);

        while (guard < 20 && string.Equals(customers[index].fullName, lastCustomerName, StringComparison.Ordinal))
        {
            index = UnityEngine.Random.Range(0, customers.Count);
            guard++;
        }

        if (string.Equals(customers[index].fullName, lastCustomerName, StringComparison.Ordinal))
        {
            index = (index + 1) % customers.Count;
        }

        return index;
    }
}
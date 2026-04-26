using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public struct CustomerData
{
    public string fullName;     // ФИО
    public string targetSocket; // Какой сокет ему нужен
}

public class CallManager : MonoBehaviour
{
    [Header("База данных")]
    [SerializeField] private List<CustomerData> customers;
    
    [Header("Настройки звонков")]
    [SerializeField] private float minInterval = 5f;
    [SerializeField] private float maxInterval = 15f;
    
    [Header("Текущий активный вызов (Для отладки)")]
    public string currentCustomerName;
    public string currentRequiredSocket;

    private List<Lamp> allLamps = new List<Lamp>();
    private float nextCallTime;

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
        nextCallTime = Time.time + Random.Range(minInterval, maxInterval);
    }

    void TryMakeCall()
    {
        // Ищем свободные лампы (состояние Off)
        List<Lamp> freeLamps = allLamps.FindAll(l => l.CurrentState == Lamp.State.Off);

        if (freeLamps.Count > 0 && customers.Count > 0)
        {
            // Выбираем случайную свободную лампу
            Lamp selectedLamp = freeLamps[Random.Range(0, freeLamps.Count)];
            // Выбираем случайного клиента
            CustomerData data = customers[Random.Range(0, customers.Count)];

            // Обновляем публичные поля для вывода
            currentCustomerName = data.fullName;
            currentRequiredSocket = data.targetSocket;

            Debug.Log($"<color=yellow>[ЗВОНОК]</color> Клиент: {data.fullName} просит сокет {data.targetSocket} на линии {selectedLamp.LinkedPinID}");

            // Запускаем звонок
            selectedLamp.StartIncomingCall(data.targetSocket);
        }
    }
}
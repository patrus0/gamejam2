using System.Collections.Generic;
using UnityEngine;

public class CallNotificationConveyor : MonoBehaviour
{
    private struct CallEntry
    {
        public string name;
        public string socket;
        public string pin;
    }

    [Header("References")]
    [SerializeField] private CallManager callManager;
    [SerializeField] private InterceptedMessageUI[] panels;

    [Header("Behavior")]
    [SerializeField] private bool clearResolvedCalls = true;
    [SerializeField] private bool newestOnFirstPanel = true;

    private readonly List<CallEntry> activeEntries = new List<CallEntry>();

    private void Awake()
    {
        if (callManager == null)
            callManager = FindFirstObjectByType<CallManager>();
    }

    private void OnEnable()
    {
        CallManager.CallStarted += HandleCallStarted;
        Lamp.CallResolvedForPin += HandleCallResolved;
    }

    private void OnDisable()
    {
        CallManager.CallStarted -= HandleCallStarted;
        Lamp.CallResolvedForPin -= HandleCallResolved;
    }

    private void HandleCallStarted(CallData callData)
    {
        var newEntry = new CallEntry
        {
            name = callData.fullName,
            socket = callData.targetSocket,
            pin = callData.pinID
        };

        if (newestOnFirstPanel)
        {
            activeEntries.Insert(0, newEntry);
        }
        else
        {
            activeEntries.Add(newEntry);
        }

        TrimToPanelCount();
        RefreshPanels();
    }

    private void HandleCallResolved(string pinID, bool success)
    {
        if (!clearResolvedCalls || string.IsNullOrWhiteSpace(pinID))
            return;

        for (int i = 0; i < activeEntries.Count; i++)
        {
            if (string.Equals(activeEntries[i].pin, pinID, System.StringComparison.OrdinalIgnoreCase))
            {
                activeEntries.RemoveAt(i);
                RefreshPanels();
                return;
            }
        }
    }

    private void TrimToPanelCount()
    {
        int max = panels != null ? panels.Length : 0;
        if (max <= 0)
        {
            activeEntries.Clear();
            return;
        }

        if (activeEntries.Count <= max)
            return;

        activeEntries.RemoveRange(max, activeEntries.Count - max);
    }

    private void RefreshPanels()
    {
        if (panels == null || panels.Length == 0)
            return;

        for (int i = 0; i < panels.Length; i++)
        {
            InterceptedMessageUI panel = panels[i];
            if (panel == null)
                continue;

            if (i < activeEntries.Count)
            {
                CallEntry entry = activeEntries[i];
                panel.ShowCall(entry.name, entry.socket, entry.pin);
            }
            else
            {
                panel.HidePanel(true);
            }
        }
    }
}

using UnityEngine;

public class TestCaller : MonoBehaviour
{
    public Lamp someLamp;
    void Start() {
        
            // Заставляем лампу Pin_1 звонить и требовать сокет A1
            someLamp.StartIncomingCall("a6");
        
    }
}
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Events
{
    [Serializable]
    public class EventGameState : UnityEvent<PlayerMovement.GameState, PlayerMovement.GameState>
    {

    }
}

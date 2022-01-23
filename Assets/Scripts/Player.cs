using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class Player : NetworkBehaviour
{

    [SyncVar(hook = nameof(OnHolaCountChanged))]
    int holaCount = 0;

    void HandleMovement()
    {
        if (isLocalPlayer)
        {
            float moveHorizontal = Input.GetAxis("Horizontal");
            float moveVertical = Input.GetAxis("Vertical");
            Vector3 movement = new Vector3(moveHorizontal * 0.1f, moveVertical * 0.1f, 0);
            transform.position = transform.position + movement;
        }
    }

    private void Update()
    {
        HandleMovement();

        if (isLocalPlayer && Input.GetKeyDown(KeyCode.X))
        {
            Debug.Log("Sending Hola to the server");
            Hola();
        }

       
    }

    [Command]
    void Hola()
    {
        Debug.Log("Received Hola from client!");
        holaCount++;
        ReplyHola();
    }

    [TargetRpc]
    void ReplyHola()
    {
        Debug.Log("Received Hola from server");
    }

    [ClientRpc]
    void TooHigh()
    {
        Debug.Log("Too high!");
    }

    void OnHolaCountChanged(int oldCount, int newCount)
    {
        Debug.Log($"Hola count changed from {oldCount} to {newCount}");
    }
}

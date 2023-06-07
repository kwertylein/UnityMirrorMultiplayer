using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class GameManager : NetworkBehaviour
{
    [SyncVar]
    private int numberOfAlivePlayers;

    public static GameManager instance;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Debug.LogError("Error: more than one GameManager instance detected!");
            Destroy(this);
        }
    }

    public void PlayerDied()
    {
        numberOfAlivePlayers--;

        if (numberOfAlivePlayers == 1)
        {
            RpcGameEnded(FindObjectOfType<PlayerController>().netIdentity);
        }
    }

    // This method gets called when a new player joins
    public void PlayerJoined()
    {
        numberOfAlivePlayers++;
    }

    [ClientRpc]
    public void RpcGameEnded(NetworkIdentity winner)
    {
        // Display the end-screen with the winner
        Debug.Log(winner + " won with " + winner.GetComponent<PlayerController>().GetCoins() + " coins!");
    }
}
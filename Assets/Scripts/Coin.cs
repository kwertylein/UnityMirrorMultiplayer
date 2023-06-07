using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class Coin : NetworkBehaviour
{
    void OnTriggerEnter2D(Collider2D collider)
    {
        var player = collider.gameObject.GetComponent<PlayerController>();
        if (player != null)
        {
            player.CollectCoin();
            Destroy(gameObject);
        }
    }
}

using UnityEngine;
using Mirror;

public class Health : NetworkBehaviour
{
    public const int maxHealth = 100;

    [SyncVar(hook = nameof(OnHealthChanged))]
    public int currentHealth = maxHealth;

    public RectTransform healthBar;  // Reference to this player's health bar

    [ClientRpc]
    public void RpcTakeDamage(int damage)
    {
        if (!isServer)
            return;

        currentHealth -= damage;

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Debug.Log("Dead!");

            // Notify the player controller that this player died
            PlayerController playerController = GetComponent<PlayerController>();
            if (playerController != null)
            {
                playerController.Die();
            }
        }
    }

    void OnHealthChanged(int oldHealth, int newHealth)
    {
        // Update this player's health bar
        healthBar.sizeDelta = new Vector2(newHealth, healthBar.sizeDelta.y);
    }
}
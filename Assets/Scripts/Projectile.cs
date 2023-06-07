using Mirror;
using UnityEngine;

public class Projectile : NetworkBehaviour
{
    public float speed = 20f;
    public int damage = 40;
    public Rigidbody2D rb;
    public NetworkIdentity shooter;
    public SpriteRenderer spriteRenderer;
    public LayerMask wallLayer;
    private float border = 10f;

    [Server]
    private void Update()
    {
        if (transform.position.x < -border || transform.position.x > border)
        {
            NetworkServer.Destroy(gameObject);
        }
    }

    [Server]
    public void Launch(Vector2 direction, bool isFacingRight)
    {
        if (!isFacingRight)
        {
            Vector3 scale = transform.localScale;
            scale.y *= -1;
            transform.localScale = scale;
        }

        rb.velocity = direction * speed;
    }

    [ServerCallback]
    void OnTriggerEnter2D(Collider2D hitInfo)
    {
        Health health = hitInfo.GetComponent<Health>();

        if (health != null && hitInfo.GetComponent<NetworkIdentity>() != shooter)
        {
            health.RpcTakeDamage(damage);
            NetworkServer.Destroy(gameObject);
        }
        else if (((1 << hitInfo.gameObject.layer) & wallLayer) != 0)
        {
            NetworkServer.Destroy(gameObject);
        }
    }
}
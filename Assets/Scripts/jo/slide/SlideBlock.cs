using UnityEngine;

public class SlideBlock : MonoBehaviour
{
    public float slideDirection = 1f;
    public float slideForce = 10f;

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Player"))
        {
            Rigidbody2D rb = collision.collider.GetComponent<Rigidbody2D>();

            // x 방향으로 힘을 계속 가함
            rb.AddForce(new Vector2(slideDirection * slideForce, 0), ForceMode2D.Force);
        }
    }
}

using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    private Rigidbody2D rb;
    [HideInInspector] public float moveH, moveV;
    [SerializeField] private float moveSpeed;
    private float lastFacingX = 1f;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        moveH = Input.GetAxis("Horizontal") * moveSpeed;
        moveV = Input.GetAxis("Vertical") * moveSpeed;
        Flip();
    }

    private void FixedUpdate()
    {
        rb.velocity = new Vector2(moveH, moveV);
    }

    private void Flip()
    {
        Vector2 moveDir = new Vector2(moveH, moveV);
        if (moveDir.sqrMagnitude < 0.0001f)
        {
            return;
        }

        if (Mathf.Abs(moveDir.x) > 0.01f)
        {
            lastFacingX = moveDir.x;
        }

        transform.eulerAngles = lastFacingX >= 0f ? Vector3.zero : new Vector3(0f, 180f, 0f);
    }
}

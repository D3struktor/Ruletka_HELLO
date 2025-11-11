using Fusion;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class PlayerController : NetworkBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 7f;
    [Range(0f,1f)] public float airControl = 0.6f;

    [Header("Jump")]
    public float jumpForce = 12f;
    public float coyoteTime = 0.12f;
    public float jumpBufferTime = 0.12f;
    public float maxFallSpeed = -18f;

    [Header("Ground check")]
    public LayerMask groundMask;
    public float groundCheckInset = 0.02f;

    [Header("Attack")]
    public float attackRadius = 0.75f;
    public float knockback = 14f;
    public LayerMask playerMask;

    Rigidbody2D _rb;
    Collider2D  _col;

    bool  _isGrounded;
    float _timeSinceGrounded;
    float _timeSinceJumpPressed;

    public override void Spawned()
    {
        _rb  = GetComponent<Rigidbody2D>();
        _col = GetComponent<Collider2D>();


        _rb.interpolation = RigidbodyInterpolation2D.None;
        _rb.freezeRotation = true;
        _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;


        SetPhysicsSimulated(Object.HasStateAuthority);
    }

    void SetPhysicsSimulated(bool simulate)
    {

        _rb.simulated   = simulate;
        _rb.isKinematic = !simulate;
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        _rb.simulated = false;
    }

    public override void FixedUpdateNetwork()
    {

        if (!Object.HasStateAuthority)
            return;

        UpdateGrounded();

        _timeSinceGrounded    += Runner.DeltaTime;
        _timeSinceJumpPressed += Runner.DeltaTime;

        Vector2 move = Vector2.zero;
        bool jumpPressed  = false;
        bool jumpHeld     = false;
        bool attackPressed= false;

        if (GetInput(out PlayerInputData input))
        {
            move          = input.Move;
            jumpPressed   = input.JumpPressed;
            jumpHeld      = input.JumpHeld;
            attackPressed = input.AttackPressed;
        }

        if (jumpPressed) _timeSinceJumpPressed = 0f;


        float targetX = move.x * moveSpeed;
        float control = _isGrounded ? 1f : airControl;

        Vector2 v = _rb.linearVelocity;
        v.x = Mathf.Lerp(v.x, targetX, control);


        bool canJump = (_isGrounded || _timeSinceGrounded <= coyoteTime) &&
                       _timeSinceJumpPressed <= jumpBufferTime;

        if (canJump)
        {
            v.y = jumpForce;
            _timeSinceJumpPressed = jumpBufferTime + 1f;
            _timeSinceGrounded    = coyoteTime + 1f;
        }

        if (v.y < maxFallSpeed) v.y = maxFallSpeed;
        _rb.linearVelocity = v;


        if (Mathf.Abs(v.x) > 0.05f)
        {
            var s = transform.localScale;
            s.x = Mathf.Sign(v.x) * Mathf.Abs(s.x);
            transform.localScale = s;
        }


        if (attackPressed)
            DoAttack();
    }

    void UpdateGrounded()
    {
        Bounds b = _col.bounds;
        b.Expand(new Vector3(-groundCheckInset, -groundCheckInset, 0f));

        float extra = 0.05f;
        var hit = Physics2D.BoxCast(b.center, b.size, 0f, Vector2.down, extra, groundMask);

        bool was = _isGrounded;
        _isGrounded = hit.collider != null;

        if (_isGrounded) _timeSinceGrounded = 0f;
        else if (was && !_isGrounded) _timeSinceGrounded = 0f;
    }

    void DoAttack()
    {

        Vector2 center = (Vector2)transform.position + new Vector2(Mathf.Sign(transform.localScale.x) * 0.6f, 0.2f);

        Collider2D[] hits = new Collider2D[4];
        int count = Physics2D.OverlapCircleNonAlloc(center, attackRadius, hits, playerMask);

        for (int i = 0; i < count; i++)
        {
            var other = hits[i];
            if (!other || other.gameObject == gameObject) continue;

            var victim = other.GetComponent<PlayerController>();
            if (victim && victim != this)
            {
                Vector2 dir = ((Vector2)victim.transform.position - (Vector2)transform.position).normalized;
                victim.RPC_ApplyKnockback(dir * knockback);
            }
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.StateAuthority | RpcTargets.InputAuthority)]
    void RPC_ApplyKnockback(Vector2 impulse)
    {
        if (_rb != null)
        {
            var v = _rb.linearVelocity;
            v += impulse;
            _rb.linearVelocity = v;
        }
    }

    public void OnKilled()
    {
        if (!Object.HasStateAuthority) return;
        _rb.linearVelocity = Vector2.zero;

        var spawn = UnityEngine.Object.FindFirstObjectByType<SpawnManager2D>();
        transform.position = spawn ? spawn.GetSpawnPoint() : Vector3.zero;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.white;
        Vector2 center = (Vector2)transform.position + new Vector2(Mathf.Sign(transform.localScale.x) * 0.6f, 0.2f);
        Gizmos.DrawWireSphere(center, attackRadius);
    }
#endif
}

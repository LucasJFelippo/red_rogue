using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class PlayerMovement : MonoBehaviour
{
    [Header("Configurações de Movimento")]
    public float runSpeed = 7f;
    public float rotationSpeed = 15f;

    [Range(0f, 1f)] 
    public float attackMovementMultiplier = 0.4f;

    public LayerMask wallLayer;

    [Header("Configurações do Dash (Para Frente)")]
    public float dashDistance = 7f;
    public float dashDuration = 0.2f;

    [Header("Configurações da Esquiva (Para Trás)")]
    public float dodgeDistance = 4f;
    public float dodgeDuration = 0.25f;

    [Header("Cooldown")]
    public float actionCooldown = 0.5f;

    // --- Componentes e Variáveis Internas ---
    private Rigidbody rb;
    private Animator animator;
    private CapsuleCollider capsule;
    private PlayerInputActions controls;
    private PlayerAttack playerAttack;

    private Vector2 moveInput;
    public bool isDashing { get; private set; } = false;
    private float nextActionTime = 0f;
    private Coroutine dashCoroutine;

    // --- Variáveis para PowerUp ---
    private float defaultRunSpeed;
    private Coroutine speedBoostCoroutine;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        capsule = GetComponent<CapsuleCollider>();
        playerAttack = GetComponent<PlayerAttack>();

        defaultRunSpeed = runSpeed;

        controls = new PlayerInputActions();
        controls.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        controls.Player.Move.canceled += ctx => moveInput = Vector2.zero;

        controls.Player.Dash.performed += ctx => TryDash();
    }

    void OnEnable() => controls.Enable();
    void OnDisable() => controls.Disable();

    public void ApplySpeedBoost(float multiplier, float duration)
    {
        if (speedBoostCoroutine != null)
        {
            StopCoroutine(speedBoostCoroutine);
        }
        speedBoostCoroutine = StartCoroutine(SpeedBoostRoutine(multiplier, duration));
    }

    private IEnumerator SpeedBoostRoutine(float multiplier, float duration)
    {
        runSpeed = defaultRunSpeed * multiplier;

        yield return new WaitForSeconds(duration);

        runSpeed = defaultRunSpeed;
        speedBoostCoroutine = null;
    }

    void FixedUpdate()
    {
        if (isDashing) return;

        // --- MUDANÇA AQUI: Lógica de Lentidão ao Atacar ---
        
        float currentSpeedLimit = runSpeed; // Pega a velocidade (que pode estar buffada)

        if (playerAttack.isAttacking)
        {
            // Em vez de travar (return), reduzimos a velocidade
            currentSpeedLimit *= attackMovementMultiplier;
        }

        bool isMoving = moveInput.magnitude > 0.1f;
        
        // Se quiser que a animação de "Walk" toque devagar enquanto ataca, mantenha isMoving true.
        // Se quiser que ele deslize na pose de ataque, force false aqui ou no Animator.
        animator.SetBool("isMoving", isMoving);

        if (isMoving)
        {
            Vector3 camForward = Camera.main.transform.forward;
            Vector3 camRight = Camera.main.transform.right;
            camForward.y = 0; camRight.y = 0;
            camForward.Normalize(); camRight.Normalize();

            Vector3 moveDirection = (camForward * moveInput.y + camRight * moveInput.x).normalized;

            // Usa o limite calculado (Lento ou Normal)
            Vector3 targetVelocity = moveDirection * currentSpeedLimit;
            targetVelocity.y = rb.linearVelocity.y;
            rb.linearVelocity = targetVelocity;

            if (moveDirection != Vector3.zero)
            {
                // DICA: Se quiser travar a rotação enquanto ataca, coloque: if (!playerAttack.isAttacking)
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
            }
        }
        else
        {
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
        }
    }

    // --- FUNÇÃO Dash ---
    private void TryDash()
    {
        if (Time.time < nextActionTime) return;

        if (dashCoroutine != null)
        {
            StopCoroutine(dashCoroutine);
        }

        if (playerAttack.isAttacking)
        {
            playerAttack.CancelAttack();
        }

        nextActionTime = Time.time + actionCooldown;

        bool isMoving = moveInput.magnitude > 0.1f;

        // --- LÓGICA DE DASH/DODGE RESTAURADA ---
        if (isMoving)
        {
            // DASH

            Vector3 camForward = Camera.main.transform.forward;
            Vector3 camRight = Camera.main.transform.right;
            camForward.y = 0; camRight.y = 0;
            camForward.Normalize(); camRight.Normalize();
            Vector3 dashDirection = (camForward * moveInput.y + camRight * moveInput.x).normalized;

            dashCoroutine = StartCoroutine(DashCoroutine(dashDistance, dashDuration, dashDirection, "Dash"));
        }
        else
        {
            // DODGE
            Vector3 dashDirection = -transform.forward;

            dashCoroutine = StartCoroutine(DashCoroutine(dodgeDistance, dodgeDuration, dashDirection, "Dodge"));
        }
    }

    // Corrotina de Dash
    private IEnumerator DashCoroutine(float distance, float duration, Vector3 direction, string triggerName)
    {
        isDashing = true;
        animator.SetTrigger(triggerName);

        rb.isKinematic = true;

        float elapsedTime = 0f;
        float speed = distance / duration;

        while (elapsedTime < duration)
        {
            Vector3 movementStep = direction * speed * Time.fixedDeltaTime;
            Vector3 newPos = rb.position + movementStep;

            if (Physics.CapsuleCast(
                rb.position + capsule.center + Vector3.up * -capsule.height * 0.5f,
                rb.position + capsule.center + Vector3.up * capsule.height * 0.5f,
                capsule.radius,
                direction,
                out RaycastHit hit,
                movementStep.magnitude,
                wallLayer))
            {
                rb.MovePosition(hit.point - direction * capsule.radius);
                break;
            }

            rb.MovePosition(newPos);

            elapsedTime += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        rb.isKinematic = false;
        isDashing = false;
        dashCoroutine = null;
    }
}
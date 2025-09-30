using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class PlayerMovement : MonoBehaviour
{
    [Header("Configurações de Movimento")]
    public float runSpeed = 7f;
    public float rotationSpeed = 15f;

    [Header("Configurações do Dash (Para Frente)")]
    public float dashDistance = 7f;
    public float dashDuration = 0.2f;
    public float actionCooldown = 1.5f;

    [Header("Configurações da Esquiva (Para Trás)")]
    public float dodgeDistance = 4f;
    public float dodgeDuration = 0.25f;

    // --- Componentes e Variáveis Internas ---
    private Rigidbody rb;
    private Animator animator;
    private CapsuleCollider capsule;
    private PlayerInputActions controls;
    private PlayerAttack playerAttack; // Referência ao script de ataque

    private Vector2 moveInput;
    private bool actionInputPressed = false;
    private bool isBusy = false; // Trava para dash/esquiva
    private float nextActionTime = 0f;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        capsule = GetComponent<CapsuleCollider>();
        playerAttack = GetComponent<PlayerAttack>(); // Pega a referência do script de ataque

        controls = new PlayerInputActions();
        controls.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        controls.Player.Move.canceled += ctx => moveInput = Vector2.zero;
        controls.Player.Dash.performed += ctx => actionInputPressed = true;
    }

    void OnEnable() => controls.Enable();
    void OnDisable() => controls.Disable();

    void FixedUpdate()
    {
        // Se estiver ocupado com um dash, esquiva OU um ataque, não faz mais nada.
        if (isBusy || playerAttack.isAttacking) return;

        bool isMoving = moveInput.magnitude > 0.1f;

        // --- LÓGICA DE DASH E ESQUIVA CONTEXTUAL ---
        bool shiftPressed = Keyboard.current.leftShiftKey.isPressed;

        if ((actionInputPressed || (shiftPressed && isMoving)) && Time.time >= nextActionTime)
        {
            nextActionTime = Time.time + actionCooldown;

            if (isMoving)
            {
                StartCoroutine(ActionCoroutine(dashDistance, dashDuration, transform.forward, "Dash"));
            }
            else
            {
                StartCoroutine(ActionCoroutine(dodgeDistance, dodgeDuration, -transform.forward, "Dodge"));
            }
        }
        actionInputPressed = false;


        // --- LÓGICA DE MOVIMENTO E ROTAÇÃO ---
        if (isMoving)
        {
            Vector3 camForward = Camera.main.transform.forward;
            Vector3 camRight = Camera.main.transform.right;
            camForward.y = 0; camRight.y = 0;
            camForward.Normalize(); camRight.Normalize();

            Vector3 moveDirection = (camForward * moveInput.y + camRight * moveInput.x).normalized;

            rb.MovePosition(rb.position + moveDirection * runSpeed * Time.fixedDeltaTime);

            if (moveDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
            }
        }
        else
        {
            rb.linearVelocity = Vector3.zero;
        }

        // --- ATUALIZAÇÃO DO ANIMATOR ---
        animator.SetBool("isMoving", isMoving);
    }

    private IEnumerator ActionCoroutine(float distance, float duration, Vector3 direction, string triggerName)
    {
        isBusy = true;
        animator.SetTrigger(triggerName);

        Vector3 startPosition = rb.position;
        Vector3 actionDirection = direction.normalized;

        float actualDistance = distance;
        Vector3 p1 = transform.position + capsule.center + Vector3.up * -capsule.height * 0.5f;
        Vector3 p2 = p1 + Vector3.up * capsule.height;
        if (Physics.CapsuleCast(p1, p2, capsule.radius, actionDirection, out RaycastHit hit, distance))
        {
            actualDistance = hit.distance;
        }

        Vector3 endPosition = startPosition + actionDirection * actualDistance;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            rb.MovePosition(Vector3.Lerp(startPosition, endPosition, elapsedTime / duration));
            elapsedTime += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        rb.MovePosition(endPosition);
        isBusy = false;
    }
}
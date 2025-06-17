using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 720f; // Graus por segundo para rotação

    [Header("Jumping")]
    public float jumpForce = 7f;
    public Transform groundCheck; // Objeto filho para verificar se está no chão
    public float groundDistance = 0.2f;
    public LayerMask groundMask;  // Quais layers são consideradas chão

    private Animator animator;
    private Rigidbody rb;
    private Vector3 moveDirection;
    private bool isGrounded;

    void Start()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();

        // Congelar rotação no Rigidbody para que o script controle totalmente
        if (rb != null)
        {
            rb.freezeRotation = true;
        }
        else
        {
            Debug.LogError("Rigidbody não encontrado no personagem! O pulo não funcionará.");
        }

        if (groundCheck == null)
        {
            Debug.LogError("Referência 'groundCheck' não definida no Inspector!");
        }
    }

    void Update()
    {
        // --- Ground Check ---
        if (groundCheck != null)
        {
            isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
        }
        else
        {
            isGrounded = true; // Assume que está no chão se não houver groundCheck (não ideal)
        }

        // Atualiza o parâmetro IsGrounded no Animator
        animator.SetBool("IsGrounded", isGrounded);

        // --- Input de Movimento ---
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");

        moveDirection = new Vector3(horizontalInput, 0f, verticalInput).normalized;
        float inputMagnitude = moveDirection.magnitude; // Já é normalizado, então será 0 ou 1

        // Atualiza o parâmetro "Speed" no Animator
        animator.SetFloat("Speed", inputMagnitude);

        // --- Rotação ---
        if (moveDirection != Vector3.zero) // Se houver input de movimento
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        // --- Pulo ---
        if (Input.GetButtonDown("Jump") && isGrounded && rb != null)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            animator.SetTrigger("Jump"); // Ativa o trigger de pulo no Animator
        }
    }

    void FixedUpdate()
    {
        // --- Movimento Físico ---
        // É melhor aplicar forças no FixedUpdate
        if (rb != null && moveDirection.magnitude >= 0.1f)
        {
            // Mover o personagem usando Rigidbody para melhor interação com física
            // Calculamos a velocidade desejada e então aplicamos
            // Isso evita o problema de "empurrar" objetos com transform.Translate
            Vector3 targetVelocity = moveDirection * moveSpeed;
            // Mantemos a velocidade Y atual para não interferir com o pulo/gravidade
            targetVelocity.y = rb.linearVelocity.y;
            rb.linearVelocity = targetVelocity;
        }
        // Se não houver input e estiver no chão, pode ser bom zerar a velocidade horizontal
        // para evitar deslizar (dependendo das configurações de Physic Material)
        else if (rb != null && isGrounded)
        {
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
        }
    }

    // Para debug, desenha a esfera do groundCheck no editor
    void OnDrawGizmosSelected()
    {
        if (groundCheck == null)
            return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(groundCheck.position, groundDistance);
    }
}
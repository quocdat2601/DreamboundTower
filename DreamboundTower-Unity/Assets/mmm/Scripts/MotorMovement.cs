using UnityEngine;

public class MotorMovement : MonoBehaviour
{
    AudioManager audioManager;

    public Transform player;
    public float speed = 10f;
    public float chaseRange = 30f;
    public float overshootDistance = 10f;

    private bool chasing = false;
    private bool returning = false;
    private Vector3 direction;

    private float groundY;

    private void Awake()
    {
        // Find AudioManager with null check
        GameObject audioObject = GameObject.FindGameObjectWithTag("Audio");
        if (audioObject != null)
        {
            audioManager = audioObject.GetComponent<AudioManager>();
        }
        else
        {
            Debug.LogWarning("No GameObject with 'Audio' tag found. AudioManager will be null.");
        }
    }

    void Start()
    {
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player").transform;
        }

        groundY = transform.position.y;
    }

    void Update()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (!chasing && !returning && distanceToPlayer <= chaseRange)
        {
            chasing = true;
            direction = (transform.position.x > player.position.x) ? Vector3.left : Vector3.right;
        }

        if (chasing)
        {
            Vector3 target = player.position + direction * overshootDistance;
            MoveHorizontally(target);
            if (audioManager != null)
                audioManager.PlaySFX(audioManager.motor);

            if (Vector3.Distance(transform.position, target) < 0.1f)
            {
                chasing = false;
                returning = true;
                direction = -direction;
            }
        }

        if (returning)
        {
            Vector3 target = player.position + direction * overshootDistance;
            MoveHorizontally(target);

            if (Vector3.Distance(transform.position, target) < 0.1f)
            {
                returning = false;
            }
        }

        FlipSprite();
    }

    void MoveHorizontally(Vector3 targetPosition)
    {
        Vector3 newPos = transform.position;
        newPos.x = Mathf.MoveTowards(transform.position.x, targetPosition.x, speed * Time.deltaTime);
        newPos.y = groundY;
        transform.position = newPos;
    }

    void FlipSprite()
    {
        Vector3 scale = transform.localScale;
        scale.x = (direction.x > 0) ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
        transform.localScale = scale;
    }
}

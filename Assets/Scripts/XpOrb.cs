using UnityEngine;

public class XpOrb : MonoBehaviour
{
    private PlayerStats player;
    public int Amount;
    [SerializeField] private float magnetSpeed = 12f;
    private bool attracted;

    private void Awake()
    {
        player = FindAnyObjectByType<PlayerStats>();
    }

    void Update()
    {
        if (player == null) return;

        Vector3 offset = player.transform.position - transform.position;
        offset.y = 0; // Ignore vertical distance for attraction
        float distance = offset.sqrMagnitude;   
        if (distance < player.pickupRange * player.pickupRange)
        {
            attracted = true;
        }

        if (attracted)
        {
            float step = magnetSpeed * Time.deltaTime;
            transform.position = Vector3.MoveTowards(transform.position, player.transform.position, step);
        }

        if (distance < 0.5f * 0.5f)
        {
            player.AddXp(Amount);
            AudioManager.Instance.Play(Sfx.XpPickup);
            Destroy(gameObject);
        }
    }
}
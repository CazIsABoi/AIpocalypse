using TMPro;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class DamagePopup : MonoBehaviour
{
    [SerializeField] private float Lifetime = 1.5f;
    public Vector3 TargetPosition;
    public string PopupDisplayContent;
    [SerializeField] private TMP_Text displayText;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        displayText.text = PopupDisplayContent;

        Lifetime -= Time.deltaTime;
        if (Lifetime > 1.25f) transform.position = Vector3.MoveTowards(transform.position, TargetPosition, 16 * Time.deltaTime);

        if (Lifetime > 1.1f) transform.localScale = Vector3.MoveTowards(transform.localScale, new Vector3(0.04f, 0.04f, 1), 0.15f * Time.deltaTime);
        else if(Lifetime > 0.9f) transform.localScale = Vector3.MoveTowards(transform.localScale, new Vector3(0.02f, 0.02f, 1), 0.3f * Time.deltaTime);
        else if (Lifetime < 0.4f && Lifetime > 0) transform.localScale = Vector3.MoveTowards(transform.localScale, new Vector3(0.0f, 0.0f, 1), 0.15f * Time.deltaTime);
        else if(Lifetime <= 0) Destroy(gameObject);

        Vector3 direction = Camera.main.transform.position - transform.position;
        transform.rotation = Quaternion.LookRotation(direction) * Quaternion.Euler(0f, 180f, 0f);
    }
}

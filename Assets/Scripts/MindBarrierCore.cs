using UnityEngine;

public class MindBarrierCore : MonoBehaviour, IDamageable
{

    [SerializeField] private float maxHealth = 5000f;
    //[SerializeField] private float baseHealth = 5000f;
    [SerializeField] private GameObject DamagePopup;
    private float health;
    public bool destroyed;

    public bool BarrierActive = true;
    [SerializeField] private GameObject BarrierObj;

    [SerializeField] private GameObject BarrierParticle;
    public GameObject ArtifimindBody;

    [SerializeField] private GameObject CrystalVisibility;
    [SerializeField] private GameObject ExplodePrefab;

    [SerializeField] private float ShieldEnergyPulseInterval;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        health = maxHealth;
    }

    // Update is called once per frame
    void Update()
    {
        if (!BarrierActive)
        {
            BarrierObj.SetActive(false);
            if (ShieldEnergyPulseInterval > 0) ShieldEnergyPulseInterval -= Time.deltaTime;
            else
            {
                if (health > 40000) ShieldEnergyPulseInterval = 0.4f;
                else if (health > 30000) ShieldEnergyPulseInterval = 0.8f;
                else if (health > 20000) ShieldEnergyPulseInterval = 1.2f;
                else if (health > 10000) ShieldEnergyPulseInterval = 1.6f;
                else if (health > 0) ShieldEnergyPulseInterval = 2f;

                if (health > 0)
                {
                    GameObject bullet = Instantiate(BarrierParticle, transform.position + new Vector3(0, 1.5f, 0) + new Vector3(Random.Range(-0.7f, 0.7f), Random.Range(-0.2f, 0.2f), Random.Range(-0.7f, 0.7f)), Quaternion.identity);
                    bullet.GetComponent<BarrierParticle>().TargetPosition = ArtifimindBody.transform.position;
                }
            }
        }
    }

    public void TakeDamage(float amount)
    {
        if (BarrierActive) amount = 0;
        if (destroyed) return;
        health -= amount;
        if (health <= 0)
        {
            AudioManager.Instance?.Play(Sfx.DroneDeath);
            CrystalVisibility.SetActive(false);
            Instantiate(ExplodePrefab, CrystalVisibility.transform.position, Quaternion.identity);
            destroyed = true;
        }
        if (amount <= 0f) return;   // barrier still up: no "0" popup spam
        GameObject spawnedPopup = Instantiate(DamagePopup, transform.position, Quaternion.identity);
        spawnedPopup.GetComponent<DamagePopup>().TargetPosition = transform.position + new Vector3(0, 3.5f, 0);
        spawnedPopup.GetComponent<DamagePopup>().PopupDisplayContent = StatNames.Compact((int)amount);

        AudioManager.Instance?.Play(Sfx.CoreHit);
    }
}

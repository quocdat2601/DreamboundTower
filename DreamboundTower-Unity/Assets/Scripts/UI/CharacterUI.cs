using UnityEngine;
using UnityEngine.UI;

public class CharacterUI : MonoBehaviour
{
    public Image avatarImage;
    public Slider hpBar;

    public int maxHP = 100;
    private int currentHP;

    void Start()
    {
        currentHP = maxHP;
        UpdateHPUI();
    }

    public void TakeDamage(int damage)
    {
        currentHP = Mathf.Max(0, currentHP - damage);
        UpdateHPUI();
    }

    void UpdateHPUI()
    {
        if (hpBar != null)
            hpBar.value = (float)currentHP / maxHP;
    }

    public bool IsDead => currentHP <= 0;
}

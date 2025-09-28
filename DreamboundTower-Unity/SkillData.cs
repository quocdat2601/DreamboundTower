using UnityEngine;

public struct SkillData
{
    public string name;
    public string description;
    public int manaCost;
    public int cooldown;
    public Sprite icon;
    public bool isPassive;

    public SkillData(string name, string description, int manaCost, int cooldown, Sprite icon, bool isPassive)
    {
        this.name = name;
        this.description = description;
        this.manaCost = manaCost;
        this.cooldown = cooldown;
        this.icon = icon;
        this.isPassive = isPassive;
    }
}
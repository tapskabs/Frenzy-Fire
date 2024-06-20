using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Unit : MonoBehaviour
{
    public string unitName;
    public int unitLevel;

    public int damage;

    public int maxHP;
    public int currentHP;

    public bool TakeDamage(int dmg)
    {
        currentHP -= dmg;

        if (currentHP <= 0)
            return true;
        else
            return false;
    }

    public void Heal(int amount)
    {
        currentHP += amount;
        if (currentHP > maxHP)
            currentHP = maxHP;
    }

    public void LevelUp()
    {
        unitLevel++;
        maxHP += 10; // Increase max HP by 10 for each level
        damage += 5; // Increase damage by 5 for each level
        currentHP = maxHP; // Restore HP to max after leveling up
    }
}

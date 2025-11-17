using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Player Meta Stats")]
    public int playerLevel = 1;
    public int damage = 10;
    public int maxHP = 100;
    public int currentHP = 100;

    [Header("Run settings")]
    public int levelsPerVictory = 5;      // how many levels you gain per defeated enemy
    public int loseLevelsOnDeath = 15;    // how many levels you lose on death
    public int loseHPOnDeath = 15;        // how much maxHP you lose on death
    public int loseDamageOnDeath = 15;    // how much damage you lose on death

    [Header("Upgrade thresholds")]
    public int upgradeLevelInterval = 10; // show upgrade panel every 10 levels

    [HideInInspector]
    public int lastUpgradeLevelTriggered = 0; // helps ensure panel triggers once per threshold

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // Call whenever player defeats an enemy
    public void ApplyVictory(int enemyGivenLevels = -1)
    {
        // Default gives fixed levelsPerVictory unless enemyGivenLevels is provided
        int gain = (enemyGivenLevels > 0) ? enemyGivenLevels : levelsPerVictory;
        playerLevel += gain;
        // Stats scaling per request: damage +5 and maxHP +5 per enemy defeat (user wanted +5 each)
        damage += 5;
        maxHP += 5;
        // do not change currentHP here (player's health persists)
    }

    // Call on player death
    public void ApplyDeathPenalty()
    {
        playerLevel -= 15;
        if (playerLevel < 1) playerLevel = 1;

        maxHP -= 15;
        if (maxHP < 1) maxHP = 1;

        damage -= 15;
        if (damage < 1) damage = 1;

        currentHP = maxHP; // restore HP after penalty
    }


    // Called after any change where we might need to display the upgrade UI
    // Returns true if an upgrade panel should be shown (level crossed next multiple of upgradeLevelInterval)
    public bool ShouldTriggerUpgrade()
    {
        int nextThreshold = (playerLevel / upgradeLevelInterval) * upgradeLevelInterval;
        if (playerLevel >= upgradeLevelInterval && nextThreshold > lastUpgradeLevelTriggered)
        {
            lastUpgradeLevelTriggered = nextThreshold;
            return true;
        }
        return false;
    }

    // Apply an upgrade chosen by the player
    public void ApplyUpgrade_Health(int amount)
    {
        maxHP += amount;
        // do NOT heal currentHP unless you want to
        currentHP = Mathf.Min(currentHP, maxHP);
    }

    public void ApplyUpgrade_Damage(int amount)
    {
        damage += amount;
    }

}

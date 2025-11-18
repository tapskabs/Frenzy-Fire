using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public enum BattleState { START, PLAYERTURN, ENEMYTURN, WON, LOST }

public class BattleSystem : MonoBehaviour
{
    [Header("Prefabs / Transforms")]
    public GameObject playerPrefab;
    public GameObject[] enemyPrefabs; // last one = boss prefab
    public Transform playerBattleStation;
    public Transform enemyBattleStation;

    [Header("UI")]
    public Text dialogueText;
    public BattleHUD playerHUD;
    public BattleHUD enemyHUD;
    public UpgradePanel upgradePanel;

    private Unit playerUnit;
    private Unit enemyUnit;
    private GameObject currentEnemyGO;

    private BattleState state;

    // cooldown
    private int turnsSinceSpecial = 3;
    private const int requiredTurnsBetweenSpecial = 3;

    // ⭐ NEW — Track boss fight
    private bool isBossFight = false;


    void Start()
    {
        state = BattleState.START;
        StartCoroutine(SetupBattle());
    }

    IEnumerator SetupBattle()
    {
        // first-time player spawn
        if (playerUnit == null)
        {
            GameObject playerGO = Instantiate(playerPrefab, playerBattleStation);
            playerUnit = playerGO.GetComponent<Unit>();
        }

        // load player stats from GameManager
        var gm = GameManager.Instance;
        playerUnit.unitLevel = gm.playerLevel;
        playerUnit.damage = gm.damage;
        playerUnit.maxHP = gm.maxHP;
        playerUnit.currentHP = gm.currentHP;

        // enemy spawn
        SpawnSingleEnemy();

        // normal message OR boss incoming warning
        if (isBossFight)
        {
            dialogueText.text = " BOSS INCOMING! Prepare yourself!"; // ⭐ NEW
        }
        else
        {
            dialogueText.text = "A wild " + enemyUnit.unitName + " approaches...";
        }

        playerHUD.SetHUD(playerUnit);
        enemyHUD.SetHUD(enemyUnit);

        yield return new WaitForSeconds(1.5f);

        state = BattleState.PLAYERTURN;
        PlayerTurn();
    }

    void SpawnSingleEnemy()
    {
        if (currentEnemyGO != null) Destroy(currentEnemyGO);

        // ⭐ NEW — Boss only spawns when > level 50
        bool spawnBoss = GameManager.Instance.playerLevel >= 50;

        GameObject prefabToSpawn;

        if (spawnBoss)
        {
            prefabToSpawn = enemyPrefabs[enemyPrefabs.Length - 1];
            isBossFight = true; // ⭐ NEW
        }
        else
        {
            int maxIndex = Mathf.Max(0, enemyPrefabs.Length - 1);
            int idx = (maxIndex == 0) ? 0 : Random.Range(0, maxIndex);
            prefabToSpawn = enemyPrefabs[idx];
            isBossFight = false;
        }

        currentEnemyGO = Instantiate(prefabToSpawn, enemyBattleStation);
        enemyUnit = currentEnemyGO.GetComponent<Unit>();

        // scale enemy stats to player
        int scaleLevel = Mathf.Max(1, GameManager.Instance.playerLevel + Random.Range(-2, 3));
        enemyUnit.unitLevel = scaleLevel;
        enemyUnit.maxHP = Mathf.Max(1, enemyUnit.maxHP + scaleLevel * 4);
        enemyUnit.currentHP = enemyUnit.maxHP;
        enemyUnit.damage = Mathf.Max(1, enemyUnit.damage + scaleLevel * 2);

        enemyHUD.SetHUD(enemyUnit);
    }

    // ——————————————————————————————————————————
    // PLAYER ATTACK
    // ——————————————————————————————————————————
    IEnumerator PlayerAttack()
    {
        bool isDead = enemyUnit.TakeDamage(playerUnit.damage);
        enemyHUD.SetHP(enemyUnit.currentHP);

        dialogueText.text = "You hit for " + playerUnit.damage + " damage.";
        yield return new WaitForSeconds(1.2f);

        turnsSinceSpecial++;

        if (isDead)
        {
            state = BattleState.WON;
            OnVictory();
        }
        else
        {
            state = BattleState.ENEMYTURN;
            StartCoroutine(EnemyTurn());
        }
    }

    // ——————————————————————————————————————————
    // SPECIAL ATTACK
    // ——————————————————————————————————————————
    IEnumerator PlayerSpecial()
    {
        if (turnsSinceSpecial < requiredTurnsBetweenSpecial)
        {
            dialogueText.text = $"Special on cooldown. Wait {requiredTurnsBetweenSpecial - turnsSinceSpecial} turn(s).";
            yield break;
        }

        int specialDamage = playerUnit.damage * 2;
        bool isDead = enemyUnit.TakeDamage(specialDamage);

        enemyHUD.SetHP(enemyUnit.currentHP);
        dialogueText.text = "SPECIAL ATTACK! You dealt " + specialDamage + "!";

        turnsSinceSpecial = 0;

        yield return new WaitForSeconds(1.2f);

        if (isDead)
        {
            state = BattleState.WON;
            OnVictory();
        }
        else
        {
            state = BattleState.ENEMYTURN;
            StartCoroutine(EnemyTurn());
        }
    }

    // ——————————————————————————————————————————
    // PLAYER HEAL
    // ——————————————————————————————————————————
    IEnumerator PlayerHeal()
    {
        // Use upgraded heal value from GameManager
        int heal = GameManager.Instance.healAmount;
        playerUnit.Heal(heal);

        // Update HUD
        playerHUD.SetHP(playerUnit.currentHP);
        dialogueText.text = $"You healed {heal} HP!";

        // Make heal persistent
        GameManager.Instance.currentHP = playerUnit.currentHP;

        yield return new WaitForSeconds(1.2f);

        // Count this as a turn for special cooldown
        turnsSinceSpecial++;

        state = BattleState.ENEMYTURN;
        StartCoroutine(EnemyTurn());
    }



    // ——————————————————————————————————————————
    // ENEMY TURN
    // ——————————————————————————————————————————
    IEnumerator EnemyTurn()
    {
        bool willHeal = Random.Range(0, 2) == 0;

        if (willHeal)
        {
            enemyUnit.Heal(8);
            enemyHUD.SetHP(enemyUnit.currentHP);
            dialogueText.text = enemyUnit.unitName + " healed 8 HP!";
            yield return new WaitForSeconds(1.0f);

            turnsSinceSpecial++;
            state = BattleState.PLAYERTURN;
            PlayerTurn();
            yield break;
        }

        dialogueText.text = enemyUnit.unitName + " attacks!";
        yield return new WaitForSeconds(1.0f);

        bool isDead = playerUnit.TakeDamage(enemyUnit.damage);
        playerHUD.SetHP(playerUnit.currentHP);

        dialogueText.text = enemyUnit.unitName + " hit you for " + enemyUnit.damage + "!";
        yield return new WaitForSeconds(1.0f);

        turnsSinceSpecial++;

        if (isDead)
        {
            state = BattleState.LOST;
            OnPlayerDefeated();
        }
        else
        {
            state = BattleState.PLAYERTURN;
            PlayerTurn();
        }
    }

    // ——————————————————————————————————————————
    // PLAYER TURN MESSAGE
    // ——————————————————————————————————————————
    void PlayerTurn()
    {
        dialogueText.text = "Choose an action:";
        playerHUD.SetHUD(playerUnit);
        enemyHUD.SetHUD(enemyUnit);
    }

    // ——————————————————————————————————————————
    // VICTORY HANDLING
    // ——————————————————————————————————————————
    void OnVictory()
    {
        dialogueText.text = "Enemy defeated!";

        // apply victory rewards
        GameManager.Instance.ApplyVictory();

        // update player data
        playerUnit.unitLevel = GameManager.Instance.playerLevel;
        playerUnit.damage = GameManager.Instance.damage;
        playerUnit.maxHP = GameManager.Instance.maxHP;

        playerHUD.SetHUD(playerUnit);

        // ⭐ NEW — Boss defeated? End game immediately
        if (isBossFight)
        {
            dialogueText.text = " You defeated the BOSS! Victory!";
            StartCoroutine(LoadEndScreen());
            return;
        }

        // level-up upgrade panel
        if (GameManager.Instance.ShouldTriggerUpgrade())
        {
            upgradePanel?.Show();
        }

        Destroy(currentEnemyGO);
        StartCoroutine(SpawnNextAfterDelay(1.2f));
    }

    IEnumerator LoadEndScreen() // ⭐ NEW
    {
        yield return new WaitForSeconds(3f);
        SceneManager.LoadScene("EndScreen");
    }

    IEnumerator SpawnNextAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        state = BattleState.START;
        StartCoroutine(SetupBattle());
    }

    // ——————————————————————————————————————————
    // DEFEAT HANDLING
    // ——————————————————————————————————————————
    void OnPlayerDefeated()
    {
        dialogueText.text = "You were defeated...";

        GameManager.Instance.ApplyDeathPenalty();

        playerUnit.unitLevel = GameManager.Instance.playerLevel;
        playerUnit.damage = GameManager.Instance.damage;
        playerUnit.maxHP = GameManager.Instance.maxHP;
        playerUnit.currentHP = GameManager.Instance.currentHP;

        playerHUD.SetHUD(playerUnit);

        StartCoroutine(RespawnAfterDeath(1.5f));
    }

    IEnumerator RespawnAfterDeath(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (currentEnemyGO != null)
            Destroy(currentEnemyGO);

        state = BattleState.START;
        StartCoroutine(SetupBattle());
    }

    // ——————————————————————————————————————————
    // BUTTON HOOKS
    // ——————————————————————————————————————————
    public void OnAttackButton()
    {
        if (state != BattleState.PLAYERTURN) return;
        StartCoroutine(PlayerAttack());
    }

    public void OnHealButton()
    {
        if (state != BattleState.PLAYERTURN) return;
        StartCoroutine(PlayerHeal());
    }

    public void OnSpecialButton()
    {
        if (state != BattleState.PLAYERTURN) return;
        StartCoroutine(PlayerSpecial());
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement; 

public enum BattleState { START, PLAYERTURN, ENEMYTURN, WON, LOST }

public class BattleSystem : MonoBehaviour
{
    public GameObject playerPrefab;

    
    public GameObject[] enemyPrefabs;

    public Transform playerBattleStation;
    public Transform enemyBattleStation;

    Unit playerUnit;
    Unit enemyUnit;

    public Text dialogueText;

    public BattleHUD playerHUD;
    public BattleHUD enemyHUD;

    public BattleState state;

    private int currentEnemyIndex = 0;

    
    public string nextSceneName;

    
    private int specialCooldown = 0;
    private const int specialCooldownDuration = 3;
    private int currentTurn = 0;

    
    void Start()
    {
        state = BattleState.START;
        StartCoroutine(SetupBattle());
    }

    IEnumerator SetupBattle()
    {
        
        if (playerUnit == null)
        {
            GameObject playerGO = Instantiate(playerPrefab, playerBattleStation);
            playerUnit = playerGO.GetComponent<Unit>();
        }

        SpawnNextEnemy();

        dialogueText.text = "A wild " + enemyUnit.unitName + " approaches...";

        playerHUD.SetHUD(playerUnit);
        enemyHUD.SetHUD(enemyUnit);

        yield return new WaitForSeconds(2f);

        state = BattleState.PLAYERTURN;
        PlayerTurn();
    }

    void SpawnNextEnemy()
    {
        if (currentEnemyIndex >= enemyPrefabs.Length)
        {
            EndBattle();
            return;
        }

        
        if (enemyBattleStation.childCount > 0)
        {
            foreach (Transform child in enemyBattleStation)
            {
                Destroy(child.gameObject);
            }
        }

        
        GameObject enemyGO = Instantiate(enemyPrefabs[currentEnemyIndex], enemyBattleStation);
        enemyUnit = enemyGO.GetComponent<Unit>();

        enemyHUD.SetHUD(enemyUnit);
        currentEnemyIndex++;
    }

    IEnumerator PlayerAttack()
    {
        bool isDead = enemyUnit.TakeDamage(playerUnit.damage);

        enemyHUD.SetHP(enemyUnit.currentHP);
        dialogueText.text = "The attack is successful!";

        yield return new WaitForSeconds(2f);

        if (isDead)
        {
            state = BattleState.WON;
            EndBattle();
        }
        else
        {
            state = BattleState.ENEMYTURN;
            StartCoroutine(EnemyTurn());
        }
    }

    IEnumerator PlayerSpecialAbility()
    {
        bool isDead = enemyUnit.TakeDamage(playerUnit.damage * 2); 

        enemyHUD.SetHP(enemyUnit.currentHP);
        dialogueText.text = "Special ability attack is successful!";

        specialCooldown = specialCooldownDuration; 

        yield return new WaitForSeconds(2f);

        if (isDead)
        {
            state = BattleState.WON;
            EndBattle();
        }
        else
        {
            state = BattleState.ENEMYTURN;
            StartCoroutine(EnemyTurn());
        }
    }

    IEnumerator EnemyTurn()
    {
        
        bool willHeal = Random.Range(0, 2) == 0;

        if (willHeal)
        {
            enemyUnit.Heal(10);
            enemyHUD.SetHP(enemyUnit.currentHP);
            dialogueText.text = enemyUnit.unitName + " Is Healing";
        }
        else
        {
            dialogueText.text = enemyUnit.unitName + " attacks!";

            yield return new WaitForSeconds(1f);

            bool isDead = playerUnit.TakeDamage(enemyUnit.damage);

            playerHUD.SetHP(playerUnit.currentHP);

            if (isDead)
            {
                state = BattleState.LOST;
                EndBattle();
                yield break;
            }
        }

        yield return new WaitForSeconds(1f);

        state = BattleState.PLAYERTURN;
        PlayerTurn();
    }

    void EndBattle()
    {
        if (state == BattleState.WON)
        {
            dialogueText.text = "You won the battle!";
            if (currentEnemyIndex >= enemyPrefabs.Length)
            {
                dialogueText.text = "You defeated all enemies!";
                
                Invoke("LoadNextScene", 2f);
                return;
            }

            LevelUpPlayer(); 

            dialogueText.text += " You leveled up!";
            playerHUD.SetHUD(playerUnit); 
            SpawnNextEnemy();
            state = BattleState.START;
            StartCoroutine(SetupBattle());
        }
        else if (state == BattleState.LOST)
        {
            dialogueText.text = "You were defeated.";
            
            Invoke("LoadNextScene", 2f);
        }
    }

    void LevelUpPlayer()
    {
        playerUnit.unitLevel += 5; 
        playerUnit.damage += 5;    
        playerUnit.maxHP += 5;    
        playerHUD.SetHUD(playerUnit); 
    }

    void PlayerTurn()
    {
        dialogueText.text = "Choose an action:";
        currentTurn++;
        if (specialCooldown > 0)
        {
            specialCooldown--;
        }
    }

    IEnumerator PlayerHeal()
    {
        playerUnit.Heal(10);

        playerHUD.SetHP(playerUnit.currentHP);
        dialogueText.text = "You feel renewed strength!";

        yield return new WaitForSeconds(2f);

        state = BattleState.ENEMYTURN;
        StartCoroutine(EnemyTurn());
    }

    public void OnAttackButton()
    {
        if (state != BattleState.PLAYERTURN)
            return;

        StartCoroutine(PlayerAttack());
    }

    public void OnHealButton()
    {
        if (state != BattleState.PLAYERTURN)
            return;

        StartCoroutine(PlayerHeal());
    }

    public void OnSpecialAbilityButton()
    {
        if (state != BattleState.PLAYERTURN)
            return;

        if (specialCooldown > 0)
        {
            dialogueText.text = $"Special ability is on cooldown. {specialCooldown} turn(s) remaining.";
            return;
        }

        StartCoroutine(PlayerSpecialAbility());
    }

    void LoadNextScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }
}

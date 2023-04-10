using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;


public enum BattleState { START, PLAYERTURN, ENEMYTURN, WON, LOST }

public class BattleSystem : MonoBehaviour
{
    public string level;

	public GameObject playerPrefab;
	

	public Transform playerStart;
	

    //public GameObject Grid;
    public GameObject EnemyGrid;

	Unit playerUnit;
	//Unit enemyUnit;  //enemy loop here
	//Unit enemy2Unit;
    private List<EnemyData> enemyDataList;
    private List<Unit> enemyUnits;


    public TextMeshProUGUI dialogueText;

	public BattleHUD playerHUD;
	public bool hasMoved;
	//public BattleHUD enemyHUD; //enemy loop here
	//public BattleHUD enemy2HUD;

	public BattleState state;

	public GameObject cardPanel;
	public GameObject rewardScreen;
	public GameObject victoryScreen;
	public GameObject lostScreen;

	public GameObject baseCard;
	public Sprite struggleIcon;
    Animator playerAnimator;

	public static BattleSystem Instance;
	

	// Start is called before the first frame update
	void Start()
	{
		Instance = this;
		state = BattleState.START;
		level = MapManager.CurrentLevel;
        playerAnimator = playerPrefab.GetComponent<Animator>();
		//playerUnit.currentHP = MapManager.currentHP;
		StartCoroutine(SetupBattle());
	}

    //ienumerator nextenemyturn(string nextenemy) //enemy loop here
    //   {
    //	if (nextenemy == "enemy")
    //       {
    //		return enemyturn();
    //       }
    //	else if (nextenemy == "enemy2") {
    //		return enemy2turn();
    //       }
    //       else
    //       {
    //		debug.log("hello");  //this failsafe catch remove after
    //		return setupbattle();
    //       }
    //   }
    //implement last enumerator that just starts player turn()
    IEnumerator SetupBattle()
	{
		//GameObject playerGO = Instantiate(playerPrefab, playerStart);
		playerUnit = playerPrefab.GetComponent<Unit>();
		playerUnit.setMaxHP(MapManager.maxHP);
		playerUnit.currentHP = MapManager.currentHP;
		playerUnit.location = 4;
		hasMoved = false;
		enemyDataList = EnemyDataManager.getEnemyData(level);
        enemyUnits = new List<Unit>();

        //GameObject enemyGO = Instantiate(enemyPrefab, enemyStart);  //bilal enemy script
		//enemyUnit = enemyGO.GetComponent<Unit>();

		//GameObject enemy2GO = Instantiate(enemy2Prefab, enemy2Start);
		//enemy2Unit = enemy2GO.GetComponent<Unit>();

        for (int i = 0; i < enemyDataList.Count; i++)
        {
            Unit enemy = new Unit();
            enemy.currentHP = enemyDataList[i].HP;
            enemy.maxHP = enemyDataList[i].HP;
            enemy.isStunned = false;
            enemy.unitName = enemyDataList[i].Name;
			enemy.location = enemyDataList[i].location;
			enemy.damage = enemyDataList[i].Damage;
			enemy.attackPattern1 = new int[3] { enemyDataList[i].AttackPattern1, enemyDataList[i].AttackPattern2, enemyDataList[i].AttackPattern3 };
			enemyUnits.Add(enemy);
			GameObject enemyObj = GameObject.Find(enemy.unitName);

            EnemyGrid.transform.GetChild(i).gameObject.SetActive(true);
			EnemyGrid.transform.GetChild(i).gameObject.transform.position = enemyObj.transform.position;
			EnemyGrid.transform.GetChild(i).gameObject.transform.position = EnemyGrid.transform.GetChild(i).gameObject.transform.position + new Vector3(0, 0.7f, 0);
			EnemyGrid.transform.GetChild(i).gameObject.GetComponent<BattleHUD>().SetHUD(enemy);
        }

		dialogueText.text = "A wild " + enemyUnits[0].unitName + " approaches...";

		playerHUD.SetHUD(playerUnit);   

		yield return new WaitForSeconds(2f);

		state = BattleState.PLAYERTURN;
		PlayerTurn();
	}


	IEnumerator EnemyTurn(int enemyNum)    //enemy loop here + enemyturn2
	{
        Unit enemyUnit = enemyUnits[enemyNum];
        if (enemyUnit.isStunned == true)
        {
            dialogueText.text = enemyUnits[enemyNum].unitName + " is stunned!";
            yield return new WaitForSeconds(1f);
            enemyUnits[enemyNum].isStunned = false;
            state = BattleState.ENEMYTURN; //make this next turn function
            if (enemyNum + 1 < enemyUnits.Count)
                yield return StartCoroutine(EnemyTurn(enemyNum+1));
            //here would call next enemy turn
        }
        else
        {
			bool isPlayerDead = false;
			if (enemyUnit.currentHP > 0 & enemyUnits[enemyNum].attackPattern1.Contains(playerUnit.location))
            {
				dialogueText.text = enemyUnits[enemyNum].unitName + " attacks!";

				yield return new WaitForSeconds(1f);

                playerAnimator.SetBool("Hit", true);
                isPlayerDead = playerUnit.TakeDamage(enemyUnits[enemyNum].damage);

				playerHUD.SetHP(playerUnit.currentHP);

				yield return new WaitForSeconds(1f);
                playerAnimator.SetBool("Hit", false);
            }
			else if(enemyUnit.currentHP > 0) 
            {
				dialogueText.text = enemyUnits[enemyNum].unitName + " misses!";
				yield return new WaitForSeconds(1f);
			}
			

            if (isPlayerDead)
            {
                playerPrefab.transform.localScale = new Vector3(1.5f, 1.7f, 1.0f);
                playerAnimator.SetBool("Dead", true);
                yield return new WaitForSeconds(2f);
                state = BattleState.LOST;
                EndBattle();
            }
            else
            {
                state = BattleState.ENEMYTURN;
                if (enemyNum + 1 < enemyUnits.Count)
                    yield return StartCoroutine(EnemyTurn(enemyNum+1));
            }
        }
    }

	

	void EndBattle()
	{

        for (int i = 0; i < enemyUnits.Count; i++)
        {
            GameObject.Find(enemyUnits[i].unitName).gameObject.SetActive(false);
        }


		if (state == BattleState.WON)
		{
			MapManager.currentHP = playerUnit.currentHP;
			dialogueText.text = "You won the battle!";
			if (enemyUnits[0].unitName == "Big Bad")
            {
				victoryScreen.SetActive(true);
				MapManager.CurrentLevel = "Start";
				MapManager.CurrentArea = "Forest";
				MapManager.maxHP = 10;
				MapManager.currentHP = 10;
				PlayerDeck.Instance.destroyDeck();
				dialogueText.text = "You beat the demo!";
				StartCoroutine(LoadToStartScreen());
			}
			rewardScreen.SetActive(true);
			int curLev = PlayerPrefs.GetInt("areaLevel");
			curLev += 1;
			PlayerPrefs.SetInt("areaLevel", curLev);
		}
		else if (state == BattleState.LOST)
		{
			lostScreen.SetActive(true);
			dialogueText.text = "You were defeated.";
			MapManager.CurrentLevel = "Start";
			MapManager.CurrentArea = "Forest";
			MapManager.maxHP = 10;
			MapManager.currentHP = 10;
			PlayerDeck.Instance.destroyDeck();
			StartCoroutine(LoadToStartScreen());


		}
	}

	void PlayerTurn()
	{
		hasMoved = false;
		if (cardPanel.transform.childCount < 1)
		{

			GameObject g;
			g = Instantiate(baseCard, cardPanel.transform);
			Debug.Log(g.transform.GetChild(0).gameObject.name);
			g.transform.GetChild(0).gameObject.GetComponent<TextMeshProUGUI>().text = "Struggle";
			g.transform.GetChild(1).gameObject.GetComponent<Image>().sprite = struggleIcon;
			g.transform.GetChild(2).GetComponent<TextMeshProUGUI>().text = "1";
			int K = 9;
			for (int j = 0; j < K; j++)
			{
				int pattern_square = j; //gives u square
				g.transform.GetChild(pattern_square + 3).gameObject.GetComponent<Image>().color = Color.red;
			}
			int[] strugglepattern = new int[9] { 0, 1, 2, 3, 4, 5, 6, 7, 8 };
			g.GetComponent<Button>().onClick.AddListener(delegate {OnStruggleButton(); });
			//g.GetComponent<Button>().AddEventListener(1,strugglepattern, 1, this.gameObject, g);
			Debug.Log("No more spells left!");
		}
		Debug.Log("Attack Pattern is...");
		Debug.Log(enemyUnits[0].attackPattern1[1]);
		for (int i = 0; i < enemyDataList.Count; i++)
        {
			if (enemyUnits[i].currentHP > 0)
			Grid.Instance.highlightSingleEnemyAttackPositions(enemyUnits[i].attackPattern1);
		}
		dialogueText.text = "Choose an action:";
		
	}

	IEnumerator PlayerAttack(int[] attackRange,int attackDamage)
	{
		state = BattleState.ENEMYTURN;
		Grid.Instance.defaultGridColor();
        //check if enemy is in pattern and can just make another button that hard codes attack one  //enemy loop here
        //bool isDead = enemyUnit.TakeDamage(playerUnit.damage);
        //bool isDead2 = enemy2Unit.TakeDamage(playerUnit.damage);
        //List<bool> deadUnits = new List<bool>();
        bool[] foundDead = new bool[enemyUnits.Count];
		Array.Fill(foundDead, false);

        playerPrefab.transform.localScale = new Vector3(1.5f, 2.25f, 1f);
        playerAnimator.SetBool("Attacking_2", true);

        for (int i = 0; i < enemyUnits.Count; i++)
        {
			if (enemyUnits[i].TakeDamage(0))
            {
				foundDead[i] = true;
            }
			if (IsEnemyInRange(attackRange,enemyUnits[i]))
				if (enemyUnits[i].TakeDamage(attackDamage))
					foundDead[i] = true;
        }


        for (int i = 0; i < enemyDataList.Count; i++)
        {
            EnemyGrid.transform.GetChild(i).gameObject.GetComponent<BattleHUD>().SetHP(enemyUnits[i].currentHP);  //enemy loop here
            //enemy2HUD.SetHP(enemy2Unit.currentHP);
        }

        dialogueText.text = "The attack is successful!";

		yield return new WaitForSeconds(2f);

        playerAnimator.SetBool("Attacking_2", false);
        playerPrefab.transform.localScale = new Vector3(1f, 1.5f, 1f);

        bool notallDead = Array.Exists(foundDead, element => element == false);
		//Debug.Log(notallDead);
        if (!(notallDead))
		{

			state = BattleState.WON;
			EndBattle();
		}
		else
		{
			state = BattleState.ENEMYTURN;
			yield return StartCoroutine(EnemyTurn(0));
            state = BattleState.PLAYERTURN;
            PlayerTurn();
        }
	}

	
	IEnumerator Struggle()
	{
		state = BattleState.ENEMYTURN;
		//check if enemy is in pattern and can just make another button that hard codes attack one  //enemy loop here
		//bool isDead = enemyUnit.TakeDamage(playerUnit.damage);
		//bool isDead2 = enemy2Unit.TakeDamage(playerUnit.damage);
		//List<bool> deadUnits = new List<bool>();
		bool foundDead = false;
        playerPrefab.transform.localScale = new Vector3(1.5f, 2.25f, 1f);
        playerAnimator.SetBool("Attacking_2", true);

		for (int i = 0; i < enemyUnits.Count; i++)
		{
				if (enemyUnits[i].TakeDamage(1))
					foundDead = true;
		}


		for (int i = 0; i < enemyDataList.Count; i++)
		{
			EnemyGrid.transform.GetChild(i).gameObject.GetComponent<BattleHUD>().SetHP(enemyUnits[i].currentHP);  //enemy loop here
																												  //enemy2HUD.SetHP(enemy2Unit.currentHP);
		}


        
        dialogueText.text = "The attack is successful!";

		yield return new WaitForSeconds(2f);
        playerAnimator.SetBool("Attacking_2", false);
        playerPrefab.transform.localScale = new Vector3(1f, 1.5f, 1f);


        if (foundDead)
		{
			state = BattleState.ENEMYTURN;
			yield return StartCoroutine(EnemyTurn(0));
			state = BattleState.PLAYERTURN;
			PlayerTurn();
			//state = BattleState.WON;
			//EndBattle();
		}
		else
		{
			state = BattleState.ENEMYTURN;
			yield return StartCoroutine(EnemyTurn(0));
			state = BattleState.PLAYERTURN;
			PlayerTurn();
		}
	}

	IEnumerator PlayerHorizontalAttack()
	{
		state = BattleState.ENEMYTURN;
		//check if enemy is in pattern and can just make another button that hard codes attack one  //enemy loop here
		//bool isDead = enemyUnit.TakeDamage(playerUnit.damage);
		//bool isDead2 = enemy2Unit.TakeDamage(playerUnit.damage);
		//List<bool> deadUnits = new List<bool>();
		bool foundDead = false;

		for (int i = 0; i < enemyUnits.Count; i++)
		{
			if (enemyUnits[i].location == 3 || enemyUnits[i].location == 4 || enemyUnits[i].location == 5)
				if (enemyUnits[i].TakeDamage(3))
					foundDead = true;
		}


		for (int i = 0; i < enemyDataList.Count; i++)
		{
			EnemyGrid.transform.GetChild(i).gameObject.GetComponent<BattleHUD>().SetHP(enemyUnits[i].currentHP);  //enemy loop here
																												  //enemy2HUD.SetHP(enemy2Unit.currentHP);
		}

		dialogueText.text = "The attack is successful!";

		yield return new WaitForSeconds(2f);


		if (foundDead)
		{
			state = BattleState.WON;
			EndBattle();
		}
		else
		{
			state = BattleState.ENEMYTURN;
			yield return StartCoroutine(EnemyTurn(0));
			state = BattleState.PLAYERTURN;
			PlayerTurn();
		}
	}

	IEnumerator PlayerHeal()
	{
		playerUnit.Heal(5);

		playerHUD.SetHP(playerUnit.currentHP);
		dialogueText.text = "You feel renewed strength!";

		yield return new WaitForSeconds(2f);

		state = BattleState.ENEMYTURN;
        yield return StartCoroutine(EnemyTurn(0));
        state = BattleState.PLAYERTURN;
        PlayerTurn();//nextEnemyTurn("enemy"));
    }

	IEnumerator PlayerStun()
	{
		//check if enemy is in squares, also //stuns only one enemy
        int stunnedEnemyNum=0;
        for (int i = 0; i < enemyUnits.Count; i++)
        {
            if (!enemyUnits[i].isStunned)
            {
                stunnedEnemyNum = i;
                enemyUnits[i].isStunned = true;
                break;
            }
        }
		//enemyUnit.isStunned = true;

		dialogueText.text = "You stunned " + enemyUnits[stunnedEnemyNum].unitName;

		yield return new WaitForSeconds(2f);
		

		state = BattleState.ENEMYTURN;
        yield return StartCoroutine(EnemyTurn(stunnedEnemyNum));
        state = BattleState.PLAYERTURN;
        PlayerTurn();// nextEnemyTurn("enemy"));
    }

	IEnumerator LoadToStartScreen()
    {
		dialogueText.text = "You were defeated.";
		yield return new WaitForSeconds(2);
		SceneManager.LoadScene("UserInterfaceScene");
	}

	public void OnAttackButton(int[] attackRange,int damage)
	{
		if (state != BattleState.PLAYERTURN)
			return;

		StartCoroutine(PlayerAttack(attackRange,damage));
	}

	public void OnMoveButton(int position)
	{	
		playerUnit.location = position;
		hasMoved = true;
	}


	public void OnStruggleButton()
	{
		if (state != BattleState.PLAYERTURN)
			return;

		StartCoroutine(Struggle());
	}

	public void OnHorizontalAttackButton()
	{
		if (state != BattleState.PLAYERTURN)
			return;

		StartCoroutine(PlayerHorizontalAttack());
	}

	public void OnHealButton()
	{
		if (state != BattleState.PLAYERTURN)
			return;

		StartCoroutine(PlayerHeal());
	}

	public void OnStunButton()
	{
		if (state != BattleState.PLAYERTURN)
			return;

		StartCoroutine(PlayerStun());
	}

	private bool IsEnemyInRange(int[] attackRange,Unit enemy)
    {
		foreach (int location in attackRange)
		{
			if (enemy.location == location)
            {
				return true;
            }
		
				
           
		}
		return false;
    }
}
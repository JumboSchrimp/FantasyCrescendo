﻿using UnityEngine;
using Crescendo.API;
using System.Collections.Generic;
using UnityEngine.UI;

public class StockMatch : Match {

    //TODO: cleanup

    [SerializeField]
    private int stockCount = 5;

    //TODO: Remove UI Code from this class


    [SerializeField]
    private CharacterData[] characters;

    public StockCriteria GetPlayerData(int playerNumber) {
        return criteria[playerNumber];
    }

    public class StockCriteria {

        private int _lives;
        private Character target;

        public bool Alive {
            get { return _lives > 0; }
        }

        public int Lives {
            get { return _lives; }
        }

        public StockCriteria(Character character, int count) {
            _lives = count;
            target = character;
            target.OnBlastZoneExit += OnDeath;
        }

        void OnDeath() {
            _lives--;
            if (!Alive) {
                target.gameObject.SetActive(false);
                target.GetComponent<CharacterRespawn>().enabled = false;
            }
        }

    }

    private List<StockCriteria> criteria;

    //TODO: Remove this hack
    protected override void Awake() {
        base.Awake();
        criteria = new List<StockCriteria>();
        SetCharacters(characters);
        StartMatch();
    }

    void Update() {
        int winner = -1;
        int aliveCount = 0;
        for (int i = 0; i < criteria.Count; i++) {
            if (criteria[i].Alive) {
                if (aliveCount == 0)
                    winner = i;
                else
                    winner = -1;
                aliveCount++;
            }
        }

        if (aliveCount == 0) {
            Debug.Log("Sudden Death");
        } else if(winner > 0) {
           DeclareWinner(winner);
        }
    }

    protected override void OnStartMatch()
    {
        criteria.Clear();
    }

	protected override void OnSpawn (Character character) {
		GameObject characterObject = character.gameObject;
		if(criteria.Count == 0) {
			characterObject.AddComponent<TestInput>();
        }
        criteria.Add(new StockCriteria(character, stockCount));
        characterObject.AddComponent<CharacterDeath>();
        characterObject.AddComponent<CharacterRespawn>();
	}

}

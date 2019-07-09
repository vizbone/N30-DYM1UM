﻿using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class UIManager : MonoBehaviour
{
	public GameObject pauseObj;
	public GameObject gameOverObj;

    public TextMeshProUGUI healthText;

	PlayerShooterController player;

	void Start () 
	{
		player = PlayerShooterController.inst;
		pauseObj.SetActive (false);
		gameOverObj.SetActive (false);
	}
	
	void Update () 
	{
		if (player != null)
		{
			if (Input.GetKeyDown (key: KeyCode.Escape)) 
				PausePlay (!pauseObj.activeSelf);
		}
		else
		{
			PausePlay (false);
			Cursor.visible = true;
			Cursor.lockState= CursorLockMode.None;
			if (!gameOverObj.activeSelf)
				gameOverObj.SetActive (true);
		}


		healthText.text = player.currentHealth + "/" + player.maxHealth;
	}

	public void PausePlay (bool pause) 
	{
		pauseObj.SetActive (pause);
		player.paused = pause;
		Cursor.visible = pause;
		Cursor.lockState = pause ?  CursorLockMode.None : CursorLockMode.Locked;
		Time.timeScale = pause ? 0 : 1;
	}

	public void MainMenu () 
	{
		SceneManager.LoadScene ("Main Menu");
	}
}
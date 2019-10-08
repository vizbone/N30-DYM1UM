﻿using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class UIManager : MonoBehaviour
{
	[Header ("General Properties")]
	public static UIManager inst;
	[SerializeField] PlayerController player;
	[SerializeField] Animator guiAnim; //Stand in for now... Will think about how to better integrate Animations
	[SerializeField] Vector2 baseResolution, currentResolution;

	[Header ("Menus")]
	[SerializeField] RectTransform pauseScreen;
	[SerializeField] RectTransform optionsScreen;
	[SerializeField] RectTransform gameOverScreen;

	[Header("UI Templates")]
	public GameObject playerUI;
	public GameObject cctvUI;

	[Header("Crosshair")]
	[SerializeField] bool isFocusing; //Check if a Hackable or Interactable Object has been focused on
	[SerializeField] Image[] rings; //For Rotation of Rings. 0 is Inner, 1 is Middle, 2 is Outer
	[SerializeField] float[] ringRotSpeeds; //For Rotation of Rings
	[SerializeField] float crosshairLerpTime;
	[SerializeField] bool crosshairIsLerping; //Check if the Focus Animation is Ongoing

	[Header("Objective Marker")]
	[SerializeField] Image marker;
	[SerializeField] Transform objective;
	[SerializeField] float offset; //Offset for Min Max XY
	[SerializeField] Vector2 minXY, maxXY;

	[Header("Stealth Gauge")]
	public RectTransform mainPointer; //Pointer to Instantiate
	public List<RectTransform> detectedPointers; //To Point to where Player is detected from. Only problem that has not been fixed is instantiating when not enough pointers... (Can be Coded in Optimisation)
	[SerializeField] Image stealthGauge;
	[SerializeField] Image detectedWarning;
	[SerializeField] float warningTime;

	[Header("Instructions and Error Msgs")]
	[SerializeField] Sprite[] controlsSprites; //Mouse Click is 0, E is 1
	[SerializeField] TextMeshProUGUI controlsInfo;
	[SerializeField] Image controlsIcon;
	[SerializeField] RectTransform controlsBackDrop;
	[SerializeField] TextMeshProUGUI errorMsg;
	[SerializeField] float instructionsLerpTime;

	[Header("Game States")]
	//May want to use Enum for Game States
	public bool isGameOver;
	public bool isPaused;

	public Action action;

	private void Awake()
	{
		inst = this;
		baseResolution = new Vector2 (1920, 1080);
		currentResolution = new Vector2(Screen.currentResolution.width, Screen.currentResolution.height);

		for (int i = 0; i < 10; i++)
		{
			if (i == 0) detectedPointers.Add(mainPointer);
			else detectedPointers.Add(Instantiate(mainPointer, mainPointer.transform.parent));

			detectedPointers[i].gameObject.SetActive(false);
		}

		//Set Min Max XY for Waypoint Pos
		minXY = new Vector2(marker.GetPixelAdjustedRect().width / 2 + offset, marker.GetPixelAdjustedRect().height / 2 + offset);
		maxXY = new Vector2(Screen.width - minXY.x, Screen.height - minXY.y);
	}

	private void Start()
	{
		player = PlayerController.inst;

		//Set Rings to Correct Local Scale First Before Game Start
		rings[0].rectTransform.localScale = new Vector3(1.25f, 1.25f, 1.25f);
		rings[2].rectTransform.localScale = new Vector3(0.75f, 0.75f, 0.75f);
		rings[0].gameObject.SetActive(false);
		rings[2].gameObject.SetActive(false);
		action += RotateRings;

		action += FlashDetectedWarning;
		detectedWarning.color = new Color(detectedWarning.color.r, detectedWarning.color.g, detectedWarning.color.b, 0);

		Color startColor = Color.clear;
		controlsBackDrop.anchoredPosition = new Vector2(125, 0);
		controlsInfo.color = startColor;
		controlsIcon.color = startColor;
		errorMsg.color = startColor;

		action += LerpInstructions; //Instructions will only Lerp based on Boolean
	}

	void Update () 
	{
		PointToObjective();
		stealthGauge.fillAmount = (player.stealthGauge / player.stealthThreshold);

		if (Input.GetKeyDown(KeyCode.Escape) && !isGameOver) PausePlay();

		if (action != null) action();
	}

	public void GameOver()
	{
		isGameOver = true;
		Time.timeScale = 0;
		Cursor.lockState = CursorLockMode.None;
		Cursor.visible = true;

		//Closes all other Screens
		pauseScreen.gameObject.SetActive(false);
		optionsScreen.gameObject.SetActive(false);

		gameOverScreen.gameObject.SetActive(true);
	}

	public void Focus(bool playerIsFocusing)
	{
		if (playerIsFocusing && !isFocusing) isFocusing = true;
		else if (!playerIsFocusing && isFocusing) isFocusing = false;
		else return;

		if (crosshairIsLerping) return;

		rings[0].gameObject.SetActive(true);
		rings[2].gameObject.SetActive(true);
		crosshairIsLerping = true;
		action += LerpFocusFeedback;
	}

	public void DisplayInstructionsAndErrors(bool isHackableObj, string errorTxt = "")
	{
		if (isHackableObj)
		{
			controlsIcon.sprite = controlsSprites[0];
			controlsInfo.text = "Hack";
		}
		else
		{
			controlsIcon.sprite = controlsSprites[1];
			controlsInfo.text = "Interact";
		}

		errorMsg.text = errorTxt;
	}

	public void LocateHackable(IHackable hackable, RectTransform pointer)
	{
		//Rotation is Correct
		Vector3 toPosition = hackable.transform.position;
		Vector3 fromPosition = player.transform.position;

		Vector3 horDir = (new Vector3(toPosition.x, 0, toPosition.z) - new Vector3(fromPosition.x, 0, fromPosition.z)).normalized;
		Vector3 forward = player.GetPlayerCamera().transform.forward;

		float horAngle = Vector3.SignedAngle(new Vector3(forward.x, 0, forward.z).normalized, horDir, Vector3.up); //Not Sure why this Works
		pointer.eulerAngles = new Vector3(0, 0, -horAngle); //Set Rotation of Player Pointer to Point at Player
	}

	#region Objective Marker Functions
	void PointToObjective()
	{
		if (!objective) return;

		Vector3 objPos = objective.position;
		Vector2 objScreenPos = player.CurrentViewingCamera.WorldToScreenPoint(objPos);

		//Check if Objective is in front or behind of Player (any body that the Player is in)
		Vector3 dirToObj = (objPos - player.CurrentViewingCamera.transform.position).normalized;
		//If Objective is behind of where Player (any body that the Player is in) is at
		if (Vector3.Dot(player.CurrentViewingCamera.transform.forward, dirToObj) < 0)
		{
			//If Object is on the Right side of the Player, Clamp it to the LEFT (Since Player is facing behind) and vice versa
			if (objScreenPos.x > Screen.width / 2) objScreenPos.x = minXY.x;
			else objScreenPos.x = maxXY.x;
		}

		//Clamp to prevent Marker from going Offscreen
		objScreenPos.x = Mathf.Clamp(objScreenPos.x, minXY.x, maxXY.x);
		objScreenPos.y = Mathf.Clamp(objScreenPos.y, minXY.y, maxXY.y);

		marker.transform.position = objScreenPos;
	}

	void ShowMarker()
	{
		marker.gameObject.SetActive(true);
	}

	void HideMarker()
	{
		marker.gameObject.SetActive(false);
	}

	void ClearObjective()
	{
		objective = null;
	}

	// I put this to public - Nigel
	public void SetNewObjective(Transform newObjective)
	{
		objective = newObjective;
		ShowMarker();
	}
	#endregion

	#region GUI Animations
	void RotateRings()
	{
		rings[1].rectTransform.Rotate(0, 0, ringRotSpeeds[1] * Time.deltaTime);

		if (!rings[0].gameObject.activeInHierarchy) return;

		rings[0].rectTransform.Rotate(0, 0, ringRotSpeeds[0] * Time.deltaTime);
		rings[2].rectTransform.Rotate(0, 0, ringRotSpeeds[2] * Time.deltaTime);
	}

	void LerpFocusFeedback()
	{
		crosshairLerpTime = isFocusing ? Mathf.Min(crosshairLerpTime + Time.deltaTime * 5, 1) : Mathf.Max(crosshairLerpTime - Time.deltaTime * 5, 0);
		rings[0].rectTransform.localScale = Vector3.Lerp(new Vector3(1.25f, 1.25f, 1.25f), Vector3.one, crosshairLerpTime);
		rings[2].rectTransform.localScale = Vector3.Lerp(new Vector3(0.75f, 0.75f, 0.75f), Vector3.one, crosshairLerpTime);

		if (crosshairLerpTime >= 1 && isFocusing)
		{
			crosshairLerpTime = 1;
			rings[0].rectTransform.localScale = Vector3.one;
			rings[2].rectTransform.localScale = Vector3.one;

			crosshairIsLerping = false;
			action -= LerpFocusFeedback;
		}
		else if (crosshairLerpTime <= 0 && !isFocusing)
		{
			crosshairLerpTime = 0;
			rings[0].rectTransform.localScale = new Vector3(1.25f, 1.25f, 1.25f);
			rings[2].rectTransform.localScale = new Vector3(0.75f, 0.75f, 0.75f);

			rings[0].gameObject.SetActive(false);
			rings[2].gameObject.SetActive(false);

			crosshairIsLerping = false;
			action -= LerpFocusFeedback;
		}
	}

	void FlashDetectedWarning()
	{
		if (warningTime == 0 && !player.isDetected) return;

		float alpha = 0;
		Color newColor = detectedWarning.color;

		if (player.isDetected)
		{
			warningTime += Time.deltaTime * 3;
			alpha = Mathf.PingPong(warningTime, 1);
		}
		else warningTime = 0;

		newColor.a = alpha;
		detectedWarning.color = newColor;
	}

	void LerpInstructions()
	{
		//Do not Lerp at all if it has reached desired Position/Color
		if ((instructionsLerpTime >= 1 && player.isFocusing) || (instructionsLerpTime <= 0 && !player.isFocusing)) return;

		instructionsLerpTime = player.isFocusing ? Mathf.Min(instructionsLerpTime + Time.deltaTime * 5, 1) : Mathf.Max(instructionsLerpTime - Time.deltaTime * 5, 0);
		controlsBackDrop.anchoredPosition = Vector2.Lerp(new Vector2(125, 0), Vector2.zero, instructionsLerpTime);

		Color infoColor = Color.Lerp(Color.clear, Color.white, instructionsLerpTime);
		controlsInfo.color = infoColor;
		controlsIcon.color = infoColor;

		errorMsg.color = Color.Lerp(Color.clear, Color.red, instructionsLerpTime);

		if (instructionsLerpTime >= 1 && player.isFocusing)
		{
			infoColor = Color.white;
			controlsBackDrop.anchoredPosition = Vector2.zero;
			controlsInfo.color = infoColor;
			controlsIcon.color = infoColor;
			errorMsg.color = Color.red;
		}
		else if (instructionsLerpTime <= 0 && !player.isFocusing)
		{
			infoColor = Color.clear;
			controlsBackDrop.anchoredPosition = new Vector2(125, 0);
			controlsInfo.color = infoColor;
			controlsIcon.color = infoColor;
			errorMsg.color = infoColor;
		}
	}
	#endregion 

	#region Animation Events
	public void ShowStaticScreen()
	{
		guiAnim.SetTrigger("Static");
	}

	public void StaticScreenAnimEvent()
	{
		player.ForcedUnhackAnimEvent();
	}
	#endregion

	#region Button Functions
	public void PausePlay () 
	{
		isPaused = !isPaused;
		optionsScreen.gameObject.SetActive(false); //If Players Press the Esc Key when in the Options Menu. Unless you want to disable use of Shortcut Keys when in Pause
		pauseScreen.gameObject.SetActive(isPaused);
		Cursor.visible = isPaused;
		Cursor.lockState = isPaused ?  CursorLockMode.None : CursorLockMode.Locked;
		Time.timeScale = isPaused ? 0 : 1;
	}

	public void Options()
	{
		optionsScreen.gameObject.SetActive(!optionsScreen.gameObject.activeSelf);
	}

	public void MainMenu () 
	{
		Time.timeScale = 1;
		SceneManager.LoadScene ("Main Menu", LoadSceneMode.Single);
	}

	public void Retry()
	{
		//Need a Proper Respawn
		Time.timeScale = 1;
		SceneManager.LoadScene(SceneManager.GetActiveScene().name, LoadSceneMode.Single);
	}
	#endregion
}
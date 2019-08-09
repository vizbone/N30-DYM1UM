﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shield : MonoBehaviour
{
	[Header ("Shield Properties")]
	public ColorIdentifier color;
	public bool isDisabled;
	[SerializeField] Renderer renderer; //If the Shield has a Renderer
	//[SerializeField] Collider collider; //If the Shield has a Collider

	//If Shield is to have a Collider, need to make it a Different Layer

	void Start ()
	{
		renderer = GetComponent<Renderer> ();
	}

	public void EnableDisableShield(bool enable)
	{
		isDisabled = !enable;

		if (isDisabled) OnShieldDisable();
		else OnShieldEnable();
	}

	void OnShieldEnable()
	{
		//For Any Lerp Functions
		renderer.enabled = true;
	}

	void OnShieldDisable()
	{
		renderer.enabled = false;
	}
}
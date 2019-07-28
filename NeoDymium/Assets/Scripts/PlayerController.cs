﻿using UnityEngine;
using System;

public class PlayerController : MonoBehaviour
{
	//May want to use Character Controller instead of Rigidbody
	public static PlayerController inst;

	[Header("Player Renderer")]
	public Renderer playerRenderer;

	[Header("Player Movement")]
	[SerializeField] CharacterController controller;
	[SerializeField] Camera playerCam;
	[SerializeField] float horLookSpeed = 1, vertLookSpeed = 1;
	[SerializeField] float yaw, pitch; //Determines Camera and Player Rotation
	[SerializeField] Vector3 velocity; //Player Velocity
	public float crouchSpeed = 5, walkSpeed = 10, runSpeed = 20; //Different Move Speed for Different Movement Action
	public float groundOffset = 0.02f;

	[Header ("For Crouching")]
	public bool isCrouching = false;
	[SerializeField] Transform standCamPos, crouchCamPos;
	[SerializeField] float playerStandHeight, playerCrouchHeight;
	float crouchStandLerpTime;

	[Header("For Hacking")]
	public bool isHacking = false;
	public IHackable hackedObj;
	public LayerMask hackableLayer;
	[SerializeField] Camera currentViewingCamera;

	[Header("Movement on Slope")]
	public bool onSlope;
	[SerializeField] float slopeForce; //For now manually inputting a value to clamp the Player down. Look for Terry to come up with a fix
	public LayerMask slopeLayer;
	public float distFromGround
	{
		get { return (controller.height / 2) + groundOffset; } //playerCollider.bounds.extents.y + 0.2f; This is via collider}
	}  //Stores the Collider.Bounds.Extents.Y. (Extents is always half of the collider size). With Controller, it is CharacterController.Height/2

	[Header("Advanced Camera Movement")]
	public bool headBob = true;
	public float headBobLerpTime;
	public float maxHeadBobOffset;
	public float bobSpeed;
	public Vector3 currentHeadBobOffset;
	public Transform headRefPoint;

	[Header("For Animations")]
	[SerializeField] Animator anim;

	[Header ("Others")]
	public bool paused;
	public Action action;

	void Awake () 
	{
		inst = this;
		playerRenderer = GetComponentInChildren<Renderer>();
	}

    void Start()
    {
		//Lock Cursor in Middle of Screen
		Cursor.lockState = CursorLockMode.Locked;
		Cursor.visible = true;

		//Getting Components
		playerCam = GetComponentInChildren<Camera>();
		controller = GetComponent<CharacterController>();
		currentViewingCamera = playerCam;
		anim = GetComponentInChildren<Animator>();
		//Set Ground Check. May need to change the y
		//distFromGround = (controller.height/2) + groundOffset; //playerCollider.bounds.extents.y + 0.2f; This is via collider

		headRefPoint = standCamPos;
		action += LerpHeadBob;
	}

	void Update()
    {
		if (!paused)
		{
			if (!isHacking)
			{
				SlopeCheck();
				ToggleCrouch();
				PlayerRotation();
				PlayerMovement();
			}

			//if (Input.GetKeyDown(KeyCode.P)) ResetHeadBob();
			if (Input.GetMouseButtonDown(0)) Hack();
			if (Input.GetMouseButtonDown(1)) Unhack();

			if (action != null) action();
		}
	}

	void PlayerRotation()
	{
		//Camera and Player Rotation
		yaw += horLookSpeed * Input.GetAxis("Mouse X");
		pitch -= vertLookSpeed * Input.GetAxis("Mouse Y"); //-Since 0-1 = 359 and 359 is rotation upwards;
		pitch = Mathf.Clamp(pitch, -90, 90); //Setting Angle Limits

		transform.eulerAngles = new Vector3(0, yaw, 0);
		playerCam.transform.localEulerAngles = new Vector3(pitch, 0, 0);
	}

	void PlayerMovement()
	{
		//if (controller.isGrounded)
		//Default to Walk Speed if Aiming. If not aiming, check if Player is holding shift.
		float movementSpeed = isCrouching ? crouchSpeed : Input.GetKey(KeyCode.LeftShift) ? runSpeed : walkSpeed;

		if (movementSpeed < walkSpeed) SetBobSpeedAndOffset(0.5f, 0.015f);
		else if (movementSpeed > walkSpeed) SetBobSpeedAndOffset(5f, 0.05f);
		else SetBobSpeedAndOffset(3f, 0.03f);

		Vector3 xMovement = Input.GetAxisRaw("Horizontal") * transform.right;
		Vector3 zMovement = Input.GetAxisRaw("Vertical") * transform.forward;
		Vector3 horVelocity = (xMovement + zMovement).normalized * movementSpeed;

		if (horVelocity.sqrMagnitude == 0) SetBobSpeedAndOffset(0.5f, 0.015f); //Set Bobbing for Idle
		anim.SetFloat("Speed", horVelocity.sqrMagnitude);

		velocity = new Vector3(horVelocity.x, velocity.y, horVelocity.z);

		//Applying Gravity before moving
		velocity.y = onSlope ? -slopeForce : -9.81f * Time.deltaTime;

		controller.Move(velocity * Time.deltaTime);
	}

	void ToggleCrouch()
	{
		if (Input.GetKeyDown(KeyCode.LeftControl))
		{
			isCrouching = !isCrouching;
			anim.SetBool("Crouch", isCrouching);
			action += LerpCrouchStand;
			if (!isHacking) headRefPoint = isCrouching ? crouchCamPos : standCamPos;
			headBob = false;
		}
	}

	void LerpCrouchStand()
	{
		crouchStandLerpTime = isCrouching ? Mathf.Min(crouchStandLerpTime + Time.deltaTime * 5, 1) : Mathf.Max(crouchStandLerpTime - Time.deltaTime * 5, 0);
		playerCam.transform.position = Vector3.Lerp(standCamPos.position, crouchCamPos.position, crouchStandLerpTime);
		controller.height = Mathf.Lerp(playerStandHeight, playerCrouchHeight, crouchStandLerpTime);
		controller.center = Vector3.Lerp(new Vector3(0, playerStandHeight / 2 + groundOffset, 0), new Vector3(0, playerCrouchHeight / 2 + groundOffset, 0), crouchStandLerpTime);

		if (isCrouching && crouchStandLerpTime >= 1)
		{
			playerCam.transform.position = crouchCamPos.position;

			ResetHeadBobTime(); 
			headBob = (bool)headRefPoint; //Check if Player should resume to Bob head

			controller.height = playerCrouchHeight;
			controller.center = new Vector3(0, playerCrouchHeight / 2 + groundOffset, 0);
			//distFromGround = (controller.height / 2) + groundOffset; //Using Property so do not have to set everytime
			action -= LerpCrouchStand;
		}
		else if (!isCrouching && crouchStandLerpTime <= 0)
		{
			playerCam.transform.position = standCamPos.position;

			ResetHeadBobTime(); 
			headBob = (bool)headRefPoint; //Check if Player should resume to Bob head

			controller.height = playerStandHeight;
			controller.center = new Vector3(0, playerStandHeight / 2 + groundOffset, 0);
			//distFromGround = (controller.height / 2) + groundOffset; //Using Property so do not have to set everytime
			action -= LerpCrouchStand;
		}
	}

	void LerpHeadBob()
	{
		if (!headBob) return;

		headBobLerpTime += Time.deltaTime;
		currentHeadBobOffset = new Vector3(0, MathFunctions.SmoothPingPong(headBobLerpTime, maxHeadBobOffset, bobSpeed), 0);
		currentViewingCamera.transform.position = headRefPoint.position + currentHeadBobOffset;
	}

	void SlopeCheck()
	{
		RaycastHit hit;
		if (Physics.Raycast(transform.position, -Vector3.up, out hit, distFromGround, slopeLayer)) onSlope = true;
	}

	//Do not want Controller.Move to be manipulated directly by other Scripts
	public void StopPlayerMovementImmediately()
	{
		velocity = new Vector3 (0, -9.81f * Time.deltaTime, 0);
	}

	void Hack()
	{
		RaycastHit hit;
		if (Physics.Raycast(currentViewingCamera.transform.position, currentViewingCamera.transform.forward, out hit, Mathf.Infinity))
		{
			if (hackableLayer == (hackableLayer | 1 << hit.transform.gameObject.layer)) //The | is needed if the Layermask Stores multiple layers
			{
				IHackable hackable = hit.transform.GetComponent<IHackable>();

				if (!hackable) return;
				else
				{
					if (hackedObj) hackedObj.OnUnhack();
					isHacking = true;
					hackedObj = hackable;
					hackedObj.OnHack();
				}
			}
		}
	}
	void Unhack()
	{
		if (hackedObj == null) return;
		hackedObj.OnUnhack();
		hackedObj = null;
		isHacking = false;
	}

	public void ChangeViewCamera(Camera camera, Transform headRefPoint = null)
	{
		currentViewingCamera.depth = 0;
		ResetHeadBob(headRefPoint);
		currentViewingCamera = camera;
		currentViewingCamera.depth = 2;
	}

	//Specific For Head Bobbing
	void ResetHeadBob(Transform newHeadRefPoint = null)
	{
		ResetHeadBobTime();
		if (headRefPoint) currentViewingCamera.transform.position = headRefPoint.position; //Reseting Camera Position

		//Overrides into new Head Reference Point
		headRefPoint = newHeadRefPoint;
		headBob = (bool)headRefPoint;
		print("Called and Head Bob is " + headBob + ". Time stamp is " + Time.time);
	}

	void ResetHeadBobTime()
	{
		headBobLerpTime = 0;
		currentHeadBobOffset = Vector3.zero;
	}

	void SetBobSpeedAndOffset(float speed = 1, float maxOffset = 0.15f)
	{
		bobSpeed = speed;
		maxHeadBobOffset = maxOffset;
	}

	//For Getting Private Components
	public Camera GetPlayerCamera()
	{
		return playerCam;
	}

	public Transform GetHeadRefTransform()
	{
		return isCrouching ? crouchCamPos : standCamPos;
	}
}
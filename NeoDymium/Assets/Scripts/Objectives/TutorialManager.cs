﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialManager : MonoBehaviour
{
    public int currentTutoriaSectionNumber;
    UIManager uIManager;

    void Awake ()
    {
        uIManager = FindObjectOfType<UIManager>();
    }

    void Start ()
    {
        currentTutoriaSectionNumber = 0;
    }

    void Update ()
    {
        CurrentDisplayingHint();
    }

    void CurrentDisplayingHint ()
    {
        switch (currentTutoriaSectionNumber)
        {
            case 7:
                uIManager.currentHint.gameObject.SetActive (true);
                uIManager.currentHint.text = "This Control Kiosk seems like it could do some good distraction ( ͡° ͜ʖ ͡°)";
                break;
            case 6:
                uIManager.currentHint.gameObject.SetActive (true);
                uIManager.currentHint.text = "Urgh, that AI is guarding the Door," + "<br>" + "Maybe something here could serve as a distraction";
                break;
            case 5:
                uIManager.currentHint.gameObject.SetActive (true);
                uIManager.currentHint.text = "Urgh, its those Higher Security Level AI Robots" + "<br>" + "I will just have to sneak my way past them for now";
                break;
            case 4:
                uIManager.currentHint.gameObject.SetActive (true);
                uIManager.currentHint.text = "To Hack," + "<br>" + "Simply look at any AI Robot to gain control of them ( ͡ʘ ͜ʖ ͡ʘ)";
                break;
            case 3:
                uIManager.currentHint.gameObject.SetActive (true);
                uIManager.currentHint.text = "Seems like the Blue Locked Coloured Door can only be opened by the same coloured AI Robot" + "<br>" + "I would need to find a Blue AI to unlock it";
                break;
            case 2:
                uIManager.currentHint.gameObject.SetActive (true);
                uIManager.currentHint.text = "That's the Security Room, I need to get past it without being spotted (▀̿Ĺ̯▀̿ ̿)";
                break;
            case 1:
                uIManager.currentHint.gameObject.SetActive (true);
                uIManager.currentHint.text = "CTRL to Crouch" + "<br>" + "Bend Over My Slave ( ͡o ͜ʖ ͡o)";
                break;
            default:
                if (uIManager.currentHint.gameObject.activeInHierarchy) uIManager.currentHint.gameObject.SetActive (false);
                break;
        }
    }
}

﻿using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class HostAndClient : MonoBehaviour
{
    public void ShowIPScreen()
    {
        SceneManager.LoadScene("CharacterSelect");
    }

    public void EnterHostIPScreen()
    {
        SceneManager.LoadScene("IP Input");
    }

    public void GoBack()
    {
        SceneManager.LoadScene("SelectMode");
    }
}
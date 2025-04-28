using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Firebase;
using Firebase.Auth;
using UnityEngine.UI;
using System;
using System.Runtime.CompilerServices;

public class BackendLogin : MonoBehaviour
{
    FirebaseAuth auth;
    public GameObject loginPanel;

    private BackendManager _BM;

    private void Awake()
    {
        _BM = GetComponent<BackendManager>();
        loginPanel.SetActive(true);
    }

    private void Start()
    {
        auth = FirebaseAuth.DefaultInstance;
    }

    public void OnLoginClick()
    {
        ResponseToLogin();
    }

    private void ResponseToLogin()
    {
        auth.SignInAnonymouslyAsync().ContinueWith(task =>
        {
            if (task.IsCompleted && !task.IsCanceled && !task.IsFaulted)
            {
                ResetData();
            }
            else
            {
                Debug.LogError($"Anonymous sign-in failed: {task.Exception}");
            }
        });
    }

    void ResetData()
    {
        _BM.DeleteStorage();
        _BM.DeleteData();

        _BM.LoadPastDate(0);
    }
}

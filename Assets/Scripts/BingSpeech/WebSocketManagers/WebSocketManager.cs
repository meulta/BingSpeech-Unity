
#if UNITY_EDITOR

using System.Security.Cryptography.X509Certificates;
using WebSocketSharp;
using System.Net.Security;
using System;
using UnityEngine;
using System.Collections.Generic;
using System.Net;

public class WebSocketManager : IWebSocketManager
{
    public WebSocket socket;

    public event MessageEventHandler OnMessage;
    public event OpenEventHandler OnOpen;

    public WebSocketManager()
    {
        ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(CheckValidCertificateCallback);
    }

    public void CloseSocket()
    {
        socket.Close();
    }

    public void OpenWebsocket(string connectionId, List<KeyValuePair<string, string>> websocketheaders)
    {
        socket = new WebSocket(connectionId, websocketheaders);

        socket.OnMessage += (sender, e) =>
        {
            if (OnMessage != null)
            {
                OnMessage.Invoke(e.Data);
            }
        };

        socket.OnOpen += (sender, e) =>
        {
            if (OnOpen != null)
            {
                OnOpen.Invoke();
            }
        };

        socket.OnError += (sender, e) =>
        {
            Debug.LogError(e.Exception);
        };

        socket.Connect();
    }

    public void SendBinary(byte[] data)
    {
        socket.Send(data);
    }

    public void SendString(string data)
    {
        socket.Send(data);
    }

    public bool CheckValidCertificateCallback(System.Object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
    {
        bool valid = true;

        // If there are errors in the certificate chain, look at each error to determine the cause.
        if (sslPolicyErrors != SslPolicyErrors.None)
        {
            for (int i = 0; i < chain.ChainStatus.Length; i++)
            {
                if (chain.ChainStatus[i].Status != X509ChainStatusFlags.RevocationStatusUnknown)
                {
                    chain.ChainPolicy.RevocationFlag = X509RevocationFlag.EntireChain;
                    chain.ChainPolicy.RevocationMode = X509RevocationMode.Online;
                    chain.ChainPolicy.UrlRetrievalTimeout = new TimeSpan(0, 1, 0);
                    chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllFlags;
                    bool chainIsValid = chain.Build((X509Certificate2)certificate);
                    if (!chainIsValid)
                    {
                        valid = false;
                    }
                }
            }
        }
        return valid;
    }
}

#endif
#if ENABLE_WINMD_SUPPORT

using System.Collections.Generic;
using Windows.Networking.Sockets;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Web;
using Windows.Foundation;
using Windows.Storage.Streams;
using Windows.Security.Cryptography.Certificates;
using UnityEngine;
using System;

public class WSAWebSocketManager : IWebSocketManager
{
    private MessageWebSocket socket;
    private DataWriter messageWriter;

    public event OpenEventHandler OnOpen;
    public event MessageEventHandler OnMessage;

    public void CloseSocket()
    {
        throw new System.NotImplementedException();
    }

    public async void OpenWebsocket(string url, List<KeyValuePair<string, string>> websocketheaders)
    {
        socket = new MessageWebSocket();

        socket.Closed += (sender, args) =>
        {
            Debug.Log("Stopped reason: " + args.Reason);
        };

        socket.MessageReceived += OnWebSocketMessage;

        socket.Control.MessageType = SocketMessageType.Utf8;
        socket.ServerCustomValidationRequested += OnServerCustomValidationRequested;
        socket.Control.IgnorableServerCertificateErrors.Add(ChainValidationResult.Untrusted);
        socket.Control.IgnorableServerCertificateErrors.Add(ChainValidationResult.InvalidName);

        foreach (var header in websocketheaders)
        {
            socket.SetRequestHeader(header.Key, header.Value);
        }

        await socket.ConnectAsync(new Uri(url));
        messageWriter = new DataWriter(socket.OutputStream);

        if (OnOpen != null)
        {
            OnOpen.Invoke();
        }
    }

    public async void SendBinary(byte[] data)
    {
        try
        {
            socket.Control.MessageType = SocketMessageType.Binary;
            messageWriter.WriteBytes(data);
            await messageWriter.StoreAsync();
        }
        catch (Exception ex)
        {
            WebErrorStatus status = WebSocketError.GetStatus(ex.GetBaseException().HResult);

            switch (status)
            {
                case WebErrorStatus.OperationCanceled:
                    Debug.LogError("Background write canceled.");
                    break;

                default:
                    Debug.LogError("Error: " + status);
                    Debug.LogError(ex.Message);
                    break;
            }
        }
    }

    public async void SendString(string data)
    {
        try
        {
            socket.Control.MessageType = SocketMessageType.Utf8;

            messageWriter.WriteString(data);
            await messageWriter.StoreAsync();
        }
        catch (Exception ex)
        {
            WebErrorStatus status = WebSocketError.GetStatus(ex.GetBaseException().HResult);

            switch (status)
            {
                case WebErrorStatus.OperationCanceled:
                    Debug.LogError("Background write canceled.");
                    break;

                default:
                    Debug.LogError("Error: " + status);
                    Debug.LogError(ex.Message);
                    break;
            }
        }
    }

    private void OnWebSocketMessage(MessageWebSocket sender, MessageWebSocketMessageReceivedEventArgs args)
    {
        try
        {
            using (DataReader reader = args.GetDataReader())
            {
                reader.UnicodeEncoding = Windows.Storage.Streams.UnicodeEncoding.Utf8;

                string message = reader.ReadString(reader.UnconsumedBufferLength);
                Debug.Log("Received: " + message);

                if (OnMessage != null)
                {
                    OnMessage.Invoke(message);
                }
            }
        }
        catch (Exception ex)
        {
            WebErrorStatus status = WebSocketError.GetStatus(ex.GetBaseException().HResult);
            Debug.Log("Received failed with status: " + status.ToString());
            Debug.Log(ex.Message);
        }
    }

    private void OnServerCustomValidationRequested(MessageWebSocket sender, WebSocketServerCustomValidationRequestedEventArgs args)
    {

        bool isValid;
        using (Deferral deferral = args.GetDeferral())
        {

            // Get the server certificate and certificate chain from the args parameter.
            isValid = true;

            if (!isValid)
            {
                args.Reject();
            }
        }
    }
}

#endif

using System.Collections.Generic;

public interface IWebSocketManager {

    void OpenWebsocket(string url, List<KeyValuePair<string, string>> websocketheaders);

    event MessageEventHandler OnMessage;
    event OpenEventHandler OnOpen;

    void CloseSocket();

    void SendString(string data);

    void SendBinary(byte[] data);
}

public delegate void MessageEventHandler(string text);
public delegate void OpenEventHandler();

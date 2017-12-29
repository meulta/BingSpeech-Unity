using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using UnityEngine;
using Newtonsoft.Json;

public class BingSpeechWebSocketClient : MonoBehaviour
{
    private string bingTokenUrl = "https://api.cognitive.microsoft.com/sts/v1.0/issueToken";
    private string bingAPIUrl = "wss://speech.platform.bing.com/speech/recognition/dictation/cognitiveservices/v1?language=en-US&format=simple";
    private string requestid;
    private string bingSpeechToken;
    private string micId;

    private IWebSocketManager socketManager;

    private AudioClip recording;
    private string audioFile;

    //Editor options
    public int MaxLengthRecording = 10;
    public string bingSpeechApiKey = "d20cb96b1f534f6b96189e83eb388752";

    //Events
    public delegate void MessageReceived(MessageBase message);
    public event MessageReceived OnMessageReceived;

    //Public but hidden from editor
    [HideInInspector]
    public string AnalyzedText { get; set; }

    [HideInInspector]
    public bool Listening { get; set; }

    [HideInInspector]
    public object JsonSpeechConfigPaylod { get; set; }

    public void Start()
    {
        audioFile = Path.Combine(Application.persistentDataPath, "Recording.wav");

#if UNITY_EDITOR
        socketManager = new WebSocketManager();
#elif ENABLE_WINMD_SUPPORT
        socketManager = new WSAWebSocketManager();
#endif
    }

    public void StartListening()
    {
        if (!Listening)
        {
            AnalyzedText = string.Empty;
            recording = Microphone.Start(micId, true, MaxLengthRecording, 22050);
            while (!(Microphone.GetPosition(null) > 0)) { } // Wait until the recording has started
            Listening = true;
        }
    }

    public void Analyze()
    {
        if (Listening)
        {
            Microphone.End(micId);
            SavWav.Save("Recording.wav", recording);
            Listening = false;
            StartCoroutine(CallBingSpeechApi());
        }
    }

    IEnumerator CallBingSpeechApi()
    {
        yield return GetToken();
        OpenWebSocket();
    }

    IEnumerator GetToken()
    {
        var bingHeaders = new Dictionary<string, string>() {
            { "Ocp-Apim-Subscription-Key", bingSpeechApiKey }
        };

        WWW tokenreq = new WWW(bingTokenUrl, new byte[1], bingHeaders);
        yield return tokenreq;
        bingSpeechToken = tokenreq.text;
    }

    private void HandlingNewMessage(string data)
    {
        var socketMessage = new BingSocketTextMessage(data);
        var message = socketMessage.AsMessage();

        //Broadcasting message to registered
        if (OnMessageReceived != null)
        {
            OnMessageReceived.Invoke(message);
        }

        Debug.Log(data);

        //Handling event
        if (message is SpeechFragmentMessage)
        {
            AnalyzedText += ((SpeechFragmentMessage)message).Text + " ";
        }
        else if (message is SpeechPhraseMessage && ((SpeechPhraseMessage)message).DisplayText != null)
        {
            AnalyzedText = ((SpeechPhraseMessage)message).DisplayText;
        }
        else if (message is TurnStartMessage)
        {
            SendAllAudio();
        }
        else if (message is TurnEndMessage)
        {
            socketManager.CloseSocket();
        }
    }

    public void OpenWebSocket()
    {
        var connectionId = Guid.NewGuid().ToString("N");
        var websocketheaders = new List<KeyValuePair<string, string>>() {
            new KeyValuePair<string, string>( "Authorization", "Bearer " + bingSpeechToken),
            new KeyValuePair<string, string>( "X-ConnectionId", connectionId ),
        };

        socketManager.OnMessage += (message) =>
        {
            HandlingNewMessage(message);
        };

        socketManager.OnOpen += () =>
        {
            SendSpeechConfig();
            SendFirstAudioPart();
        };

        socketManager.OpenWebsocket(bingAPIUrl, websocketheaders);
    }

    public void SendSpeechConfig()
    {
        if (JsonSpeechConfigPaylod == null)
        {
            JsonSpeechConfigPaylod = new
            {
                context = new
                {
                    system = new
                    {
                        version = "1.0.00000"
                    },
                    os = new
                    {
                        platform = "Unity",
                        name = "Unity",
                        version = ""
                    },
                    device = new
                    {
                        manufacturer = "SpeechSample",
                        model = "UnitySpeechSample",
                        version = "1.0.00000"
                    }
                }
            };
        }

        //Create a request id that is unique for this 
        requestid = Guid.NewGuid().ToString("N");

        //Send the first message after connecting to the websocket with required headers
        string payload =
         "Path: speech.config" + Environment.NewLine +
         "x-timestamp: " + DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffK") + Environment.NewLine +
         "content-type: application/json; charset=utf-8" + Environment.NewLine + Environment.NewLine +
         JsonConvert.SerializeObject(JsonSpeechConfigPaylod);

        socketManager.SendString(payload);
    }

    public void SendFirstAudioPart()
    {
        var headerBytes = CreateAudioHeaders();
        var headerHead = CreateAudioHeaderHead(headerBytes);

        using (RiffChunker riff = new RiffChunker(audioFile))
        {
            var riffHeaderBytes = riff.RiffHeader.Bytes;
            var arr = headerHead.Concat(headerBytes).Concat(riffHeaderBytes).ToArray();

            socketManager.SendBinary(arr);
        }
    }

    public void SendAllAudio()
    {
        using (RiffChunker riff = new RiffChunker(audioFile))
        {
            var currentChunk = riff.Next();
            while (currentChunk != null)
            {
                var cursor = 0;
                while (cursor < currentChunk.SubChunkDataBytes.Length)
                {
                    var headerBytes = CreateAudioHeaders();
                    var headerHead = CreateAudioHeaderHead(headerBytes);

                    var length = Math.Min(4096 * 2 - headerBytes.Length - 8, currentChunk.AllBytes.Length - cursor); //8bytes for the chunk header

                    var chunkHeader = Encoding.ASCII.GetBytes("data").Concat(BitConverter.GetBytes((UInt32)length)).ToArray();

                    byte[] dataArray = new byte[length];
                    Array.Copy(currentChunk.AllBytes, cursor, dataArray, 0, length);

                    cursor += length;

                    var arr = headerHead.Concat(headerBytes).Concat(chunkHeader).Concat(dataArray).ToArray();
                    var arrSeg = new ArraySegment<byte>(arr, 0, arr.Length);

                    socketManager.SendBinary(arr);
                }

                //Move to the next RIFF chunk if there is one. 
                currentChunk = riff.Next();
            }
            //Send Audio End
            {
                var headerBytes = CreateAudioHeaders();
                var headerHead = CreateAudioHeaderHead(headerBytes);
                var arr = headerHead.Concat(headerBytes).ToArray();

                socketManager.SendBinary(arr);
            }
        }
    }

    private byte[] CreateAudioHeaders()
    {
        var outputBuilder = new StringBuilder();
        outputBuilder.Append("path:audio\r\n");
        outputBuilder.Append("x-requestid:" + requestid + "\r\n");
        outputBuilder.Append("x-timestamp:" + DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffK") + "\r\n");
        outputBuilder.Append("content-type:audio/x-wav\r\n");

        return Encoding.ASCII.GetBytes(outputBuilder.ToString());
    }

    private byte[] CreateAudioHeaderHead(byte[] headerBytes)
    {
        var headerbuffer = new ArraySegment<byte>(headerBytes, 0, headerBytes.Length);
        var str = "0x" + (headerBytes.Length).ToString("X");
        var headerHeadBytes = BitConverter.GetBytes((UInt16)headerBytes.Length);
        var isBigEndian = !BitConverter.IsLittleEndian;
        return !isBigEndian ? new byte[] { headerHeadBytes[1], headerHeadBytes[0] } : new byte[] { headerHeadBytes[0], headerHeadBytes[1] };
    }
}
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml.Linq;
using UnityEngine;
using UnityEngine.UI;
using WebSocket4Net;

public class BingSpeechWebClient : MonoBehaviour
{
    private string bingTokenUrl = "https://api.cognitive.microsoft.com/sts/v1.0/issueToken";
    private string bingAPIUrl = "wss://speech.platform.bing.com/speech/recognition/dictation/cognitiveservices/v1?language=en-US&format=simple";
    private string bingSpeechApiKey = "d20cb96b1f534f6b96189e83eb388752";
    private string requestid;
    private string bingSpeechToken;
    private string _micID = null;
    private AudioClip _rec = null;
    private string audioFile;
    public string analyzedText;
    public bool listening = false;

    public void Start()
    {
        audioFile = Path.Combine(Application.persistentDataPath, "Recording.wav");
        ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(CheckValidCertificateCallback);
    }

    public void StartListening()
    {
        if (!listening)
        {
            _rec = Microphone.Start(_micID, true, 20, 22050);
            while (!(Microphone.GetPosition(null) > 0)) { } // Wait until the recording has started
            listening = true;
        }
    }

    public void Analyze()
    {
        if (listening)
        {
            Microphone.End(_micID);
            SavWav.Save("Recording.wav", _rec);
            listening = false;
            StartCoroutine(CallBingSpeechApi(bingSpeechApiKey));
        }
    }

    IEnumerator CallBingSpeechApi(string APIKey)
    {
        var bingHeaders = new Dictionary<string, string>() {
            { "Ocp-Apim-Subscription-Key", APIKey } //Bing Speech API
        };

        //Token
        WWW www = new WWW(bingTokenUrl, new byte[1], bingHeaders);
        yield return www;
        bingSpeechToken = www.text;

        //Connect
        var connectionId = Guid.NewGuid().ToString("N");
        var websocketheaders = new List<KeyValuePair<string, string>>() {
            new KeyValuePair<string, string>( "Authorization", "Bearer " + bingSpeechToken),
            new KeyValuePair<string, string>( "X-ConnectionId", connectionId ),
        };

        var ws = new WebSocket(bingAPIUrl, "", null, websocketheaders);

        ws.MessageReceived += (sender, e) =>
        {
            var socketMessage = new BingSocketTextMessage(e.Message);

            var message = socketMessage.AsMessage();
            Debug.Log(e.Message);
            if (message is SpeechFragmentMessage)
            {
                analyzedText += ((SpeechFragmentMessage)message).Text + " ";
            }

            if (message is SpeechPhraseMessage && ((SpeechPhraseMessage)message).DisplayText != null)
            {
                analyzedText = ((SpeechPhraseMessage)message).DisplayText;
            }

            if (message is TurnStartMessage)
            {
                using (RiffChunker riff = new RiffChunker(audioFile))
                {
                    var currentChunk = riff.Next();
                    while (currentChunk != null)
                    {
                        var cursor = 0;
                        while (cursor < currentChunk.SubChunkDataBytes.Length)
                        {
                            /* #region Prepare header */
                            var outputBuilder = new StringBuilder();
                            outputBuilder.Append("path:audio" + Environment.NewLine);
                            outputBuilder.Append($"x-requestid:{requestid}" + Environment.NewLine);
                            outputBuilder.Append($"x-timestamp:{DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffK")}" + Environment.NewLine);
                            outputBuilder.Append($"content-type:audio/x-wav" + Environment.NewLine);

                            var headerBytes = Encoding.ASCII.GetBytes(outputBuilder.ToString());
                            var headerbuffer = new ArraySegment<byte>(headerBytes, 0, headerBytes.Length);
                            var str = "0x" + (headerBytes.Length).ToString("X");
                            var headerHeadBytes = BitConverter.GetBytes((UInt16)headerBytes.Length);
                            var isBigEndian = !BitConverter.IsLittleEndian;
                            var headerHead = !isBigEndian ? new byte[] { headerHeadBytes[1], headerHeadBytes[0] } : new byte[] { headerHeadBytes[0], headerHeadBytes[1] };
                            /* #endregion*/

                            var length = Math.Min(4096 * 2 - headerBytes.Length - 8, currentChunk.AllBytes.Length - cursor); //8bytes for the chunk header

                            var chunkHeader = Encoding.ASCII.GetBytes("data").Concat(BitConverter.GetBytes((UInt32)length)).ToArray();

                            byte[] dataArray = new byte[length];
                            Array.Copy(currentChunk.AllBytes, cursor, dataArray, 0, length);

                            cursor += length;

                            var arr = headerHead.Concat(headerBytes).Concat(chunkHeader).Concat(dataArray).ToArray();
                            var arrSeg = new ArraySegment<byte>(arr, 0, arr.Length);

                            ws.Send(new List<ArraySegment<byte>>() { arrSeg });
                        }

                        //Move to the next RIFF chunk if there is one. 
                        currentChunk = riff.Next();
                    }
                    /* #region Send Audio End  */
                    {
                        /* #region Prepare header */
                        var outputBuilder = new StringBuilder();
                        outputBuilder.Append("path:audio" + Environment.NewLine);
                        outputBuilder.Append($"x-requestid:{requestid}" + Environment.NewLine);
                        outputBuilder.Append($"x-timestamp:{DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffK")}" + Environment.NewLine);
                        outputBuilder.Append($"content-type:audio/x-wav" + Environment.NewLine);

                        var headerBytes = Encoding.ASCII.GetBytes(outputBuilder.ToString());
                        var headerbuffer = new ArraySegment<byte>(headerBytes, 0, headerBytes.Length);
                        var str = "0x" + (headerBytes.Length).ToString("X");
                        var headerHeadBytes = BitConverter.GetBytes((UInt16)headerBytes.Length);
                        var isBigEndian = !BitConverter.IsLittleEndian;
                        var headerHead = !isBigEndian ? new byte[] { headerHeadBytes[1], headerHeadBytes[0] } : new byte[] { headerHeadBytes[0], headerHeadBytes[1] };
                        /* #endregion*/

                        var arr = headerHead.Concat(headerBytes).ToArray();
                        var arrSeg = new ArraySegment<byte>(arr, 0, arr.Length);

                        ws.Send(new List<ArraySegment<byte>>() { arrSeg });
                    }
                    /* #endregion*/
                    Console.WriteLine($"Finished sending data");
                }
            }

        };

        ws.Opened += (sender, e) =>
        {
            var json = new
            {
                context = new
                {
                    system = new
                    {
                        version = "1.0.00000"
                    },
                    os = new
                    {
                        platform = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/59.0.3071.115 Safari/537.36",
                        name = "Browser",
                        version = ""
                    },
                    device = new
                    {
                        manufacturer = "SpeechSample",
                        model = "SpeechSample",
                        version = "1.0.00000"
                    }
                }
            };

            requestid = Guid.NewGuid().ToString("N");
            string payload =
             "Path: speech.config" + Environment.NewLine +
             "x-requestid: " + requestid + Environment.NewLine +
             "x-timestamp: " + DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffK") + Environment.NewLine +
             "content-type: application/json; charset=utf-8" + Environment.NewLine + Environment.NewLine +
             JsonConvert.SerializeObject(json, Formatting.None);

            ws.Send(payload);

            var outputBuilder = new StringBuilder();
            outputBuilder.Append("path:audio\r\n");
            outputBuilder.Append($"x-requestid:{requestid}\r\n");
            outputBuilder.Append($"x-timestamp:{DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffK")}\r\n");
            outputBuilder.Append($"content-type:audio/x-wav\r\n");

            var headerBytes = Encoding.ASCII.GetBytes(outputBuilder.ToString());
            var headerbuffer = new ArraySegment<byte>(headerBytes, 0, headerBytes.Length);
            var str = "0x" + (headerBytes.Length).ToString("X");
            var headerHeadBytes = BitConverter.GetBytes((UInt16)headerBytes.Length);
            var isBigEndian = !BitConverter.IsLittleEndian;
            var headerHead = !isBigEndian ? new byte[] { headerHeadBytes[1], headerHeadBytes[0] } : new byte[] { headerHeadBytes[0], headerHeadBytes[1] };
            /* #endregion*/

            using (RiffChunker riff = new RiffChunker(audioFile))
            {
                var riffHeaderBytes = riff.RiffHeader.Bytes;
                var arr = headerHead.Concat(headerBytes).Concat(riffHeaderBytes).ToArray();
                var arrSeg = new ArraySegment<byte>(arr, 0, arr.Length);
                ws.Send(new List<ArraySegment<byte>>() { arrSeg });
            }
        };

        ws.Error += (sender, e) =>
        {
            Debug.LogError(e.Exception);
        };

        ws.Open();
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
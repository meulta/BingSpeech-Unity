using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SpeechTest : MonoBehaviour {

    public Text debugDisplay;
    private BingSpeechWebSocketClient bingSpeechWebClient;
    private bool speak;

    // Use this for initialization
    void Start () {
        bingSpeechWebClient = GetComponent<BingSpeechWebSocketClient>();
        bingSpeechWebClient.OnMessageReceived += (message) => {
            if(message is TurnEndMessage)
            {
                //Here is how you get messages.
            }
        };
    }
	
	// Update is called once per frame
	void Update () {
        if (bingSpeechWebClient != null)
        {
            debugDisplay.text = bingSpeechWebClient.Listening ? "Listening... " :  bingSpeechWebClient.AnalyzedText;
        }
    }

    public void StartListening()
    {
        if (!speak)
        {
            bingSpeechWebClient.StartListening();
            speak = true;
        }
    }

    public void StopAndAnalyze()
    {
        if (speak)
        {
            bingSpeechWebClient.Analyze();
            speak = false;
        }
    }
}

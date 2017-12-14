using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SpeechTest : MonoBehaviour {

    public Text debugDisplay;
    private BingSpeechWebClient bingSpeechWebClient;
    private bool speak;

    // Use this for initialization
    void Start () {
        bingSpeechWebClient = GetComponent<BingSpeechWebClient>();
    }
	
	// Update is called once per frame
	void Update () {
        if (bingSpeechWebClient != null)
        {
            debugDisplay.text = bingSpeechWebClient.listening ? "Listening... " :  bingSpeechWebClient.analyzedText;
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

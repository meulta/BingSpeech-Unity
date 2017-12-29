using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;

internal class BingSocketTextMessage
{
    /* #region Public Properties */
    public string Body { get; private set; }
    public List<KeyValuePair<string, string>> Headers { get; private set; }
    public string Path
    {
        get
        {
            return this.GetHeader("path");
        }
    }
    public string RequestId
    {
        get
        {
            return this.GetHeader("x-requestid");
        }
    }
    /* #endregion Public Properties */
    /* #region Public Constructors */
    public BingSocketTextMessage(string message)
    {
        var headers = new List<KeyValuePair<string, string>>();
        StringReader str = new StringReader(message);
        bool wasPreviousLineEmpty = true;
        string line = null;
        do
        {
            line = str.ReadLine();
            if (line == string.Empty)
            {
                if (wasPreviousLineEmpty) break;
            }
            else
            {
                var colonIndex = line.IndexOf(':');
                if (colonIndex > -1)
                {
                    headers.Add(new KeyValuePair<string, string>(line.Substring(0, colonIndex), line.Substring(colonIndex + 1)));
                }
            }

        } while (line != null);
        this.Headers = headers;
        this.Body = str.ReadToEnd();
    }
    /* #endregion Public Constructors */
    /* #region Public Methods */
    public MessageBase AsMessage()
    {
        Type type = null;
        switch (this.Path.ToLower())
        {
            case "turn.start":
                type = typeof(TurnStartMessage);
                break;
            case "turn.end":
                type = typeof(TurnEndMessage);
                break;
            case "speech.enddetected":
                type = typeof(SpeechEndDetectedMessage);
                break;
            case "speech.phrase":
                type = typeof(SpeechPhraseMessage);
                break;
            case "speech.hypothesis":
                type = typeof(SpeechHypothesisMessage);
                break;
            case "speech.startdetected":
                type = typeof(SpeechStartDetectedMessage);
                break;
            case "speech.fragment":
                type = typeof(SpeechFragmentMessage);
                break;
            default:
                throw new NotSupportedException(this.Path);
        }
        var retval = JsonConvert.DeserializeObject(this.Body, type);
        return (MessageBase)retval;
    }
    public string GetHeader(string key)
    {
        foreach (var p in this.Headers)
        {
            if (string.Equals(p.Key, key, StringComparison.OrdinalIgnoreCase))
            {
                return p.Value;
            }
        }
        return null;
    }
    /* #endregion Public Methods */
}
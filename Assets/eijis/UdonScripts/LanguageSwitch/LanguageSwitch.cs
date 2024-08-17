//#define TKCH_DEBUG

using System;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

public class LanguageSwitch : UdonSharpBehaviour
{
    public ScrollRect infoView;
    public GameObject[] targets;

    private GameObject[] enTargets;
    private GameObject[] jaTargets;

    private int displayLine;
    private float tickStepV = 2.0f;
    private float tickStepH = 0.2f;
    private float hStep = 0;
    private float vStep = (1f / 29);
    
    [NonSerialized, UdonSynced, FieldChangeCallback(nameof(LineCount))]
    private int lineCount = 0;
    [NonSerialized, UdonSynced, FieldChangeCallback(nameof(HScrollCount))]
    private int hScrollCount = 0;

    public int LineCount
    {
        get => lineCount;
        set
        {
            lineCount = value;
        }
    }
    
    public int HScrollCount
    {
        get => hScrollCount;
        set
        {
            hScrollCount = value;
        }
    }
    
    public override void OnPlayerJoined(VRCPlayerApi player)
    {
        if (ReferenceEquals(null, player))
        {
            return;
        }
        
        if (player.isLocal)
        {
            return;
        }
        
        if (ReferenceEquals(null, Networking.LocalPlayer))
        {
            return;
        }

        if (Networking.LocalPlayer.isMaster)
        {
            RequestSerialization();
        }
    }
    
    public void Start()
    {
        enTargets = new GameObject[targets.Length];
        jaTargets = new GameObject[targets.Length];

        int i = 0;
        foreach (var target in targets)
        {
            var trEn = target.transform.Find("TextEn");
            if (ReferenceEquals(null, trEn))
            {
                trEn = target.transform.Find("Text");
            }
            
            var trJa = target.transform.Find("TextJa");
            if (ReferenceEquals(null, trJa))
            {
                trJa = target.transform.Find("Text");
            }

            if (ReferenceEquals(null, trEn) || ReferenceEquals(null, trJa))
            {
                enTargets[i] = null;
                jaTargets[i] = null;
            }
            else
            {
                enTargets[i] = trEn.gameObject;
                jaTargets[i] = trJa.gameObject;
            }

            i++;
        }

        lineCount = 0;
        infoView.normalizedPosition = new Vector2(0, 1.0f);
        SendCustomEventDelayedSeconds(nameof(ScrollTickV),tickStepV);
        SendCustomEventDelayedSeconds(nameof(ScrollTickH),tickStepH);
    }

    /*
    public void OnEnable()
    {
        infoView.normalizedPosition = new Vector2(100f, 100f);
    }

    private void Update()
    {
        infoView.normalizedPosition = Vector2.Lerp(infoView.normalizedPosition, );
    }
    */

    public void ScrollTickH()
    {
        var currentPos = infoView.normalizedPosition;
        //float newX = (hStep == 0 ? 0 : currentPos.x + hStep);
        float newX = (hStep == 0 ? 0 : hScrollCount * hStep);
        if (1.0f < newX)
        {
            newX = 1.0f;
        }
        else
        {
            hScrollCount++;
        }
        infoView.normalizedPosition = new Vector2(newX, currentPos.y);
        SendCustomEventDelayedSeconds(nameof(ScrollTickH),tickStepH);
    }

    public void ScrollTickV()
    {
        var currentPos = infoView.normalizedPosition;

#if TKCH_DEBUG
        //Debug.Log($"LanguageSwitch::ScrollTick() currentPos[x,y] => [{currentPos.x}, {currentPos.y}]");
        Debug.Log($"LanguageSwitch::ScrollTickV() currentPos.y => {currentPos.y}");
#endif

        //float newY = currentPos.y + vStep;
        float newY = 1f - ((lineCount + 1) * vStep);
        if (29 < lineCount)
        {
            newY = 1.0f;
            lineCount = 0;
        }
        else
        {
            lineCount++;
        }

        if (0 < lineCount % 2)
        {
            hStep = 0.02f;
            tickStepV = 10.0f;
        }
        else
        {
            hScrollCount = 0;
            hStep = 0;
            tickStepV = 1.0f;
        }

        infoView.normalizedPosition = new Vector2(currentPos.x, newY);
#if TKCH_DEBUG
        //Debug.Log($"LanguageSwitch::ScrollTick() newPos[x,y] => [{infoView.normalizedPosition.x}, {infoView.normalizedPosition.y}]");
        Debug.Log($"LanguageSwitch::ScrollTickV() newPos.y => {infoView.normalizedPosition.y}");
#endif
        SendCustomEventDelayedSeconds(nameof(ScrollTickV),tickStepV);
    }
    
    public void ToEnglish()
    {
#if TKCH_DEBUG
        Debug.Log("LanguageSwitch::ToEnglish()");
#endif
        foreach (var target in jaTargets)
        {
            if (ReferenceEquals(null, target))
            {
                continue;
            }
            target.SetActive(false);
        }        
        
        foreach (var target in enTargets)
        {
            if (ReferenceEquals(null, target))
            {
                continue;
            }
            target.SetActive(true);
        }        
    }

    public void ToJapanese()
    {
#if TKCH_DEBUG
        Debug.Log("LanguageSwitch::ToJapanese()");
#endif
        foreach (var target in enTargets)
        {
            if (ReferenceEquals(null, target))
            {
                continue;
            }
            target.SetActive(false);
        }

        foreach (var target in jaTargets)
        {
            if (ReferenceEquals(null, target))
            {
                continue;
            }
            target.SetActive(true);
        }
    }

}

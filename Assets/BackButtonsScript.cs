using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KModkit;
using System.Text.RegularExpressions;
using System;

public class BackButtonsScript : MonoBehaviour {

    public KMAudio audio;
    public KMBombInfo bomb;

    public KMSelectable[] buttons;
    public TextMesh display;
    private string[] allButtonTexts = new string[] { "RETURN", "BACK UP", "<--", "<-", "<---", "GO BACK", "FALLBACK", "UNDO", "BACKPEDAL", "BACKTRACK", "LOOK BACK", "TURN BACK", "REGRESS", "RELAPSE", "REVERT", "RETRACT", "RECEDE", "RENEGE", "REWIND" };
    private float[] textXScales = new float[] { 0.0008f, 0.0008f, 0.0008f, 0.0008f, 0.0008f, 0.0008f, 0.0007f, 0.0008f, 0.00061f, 0.00059f, 0.00061f, 0.0006f, 0.00075f, 0.00075f, 0.0008f, 0.00075f, 0.0008f, 0.0008f, 0.0008f };
    private string[] buttonTexts = new string[13];
    private List<int> correctPresses = new List<int>();
    private int curIndex;
    private bool activated;

    static int moduleIdCounter = 1;
    int moduleId;
    private bool moduleSolved;

    void Awake()
    {
        moduleId = moduleIdCounter++;
        moduleSolved = false;
        foreach (KMSelectable obj in buttons)
        {
            KMSelectable pressed = obj;
            pressed.OnInteract += delegate () { PressButton(pressed); return false; };
        }
        GetComponent<KMBombModule>().OnActivate += OnActivate;
    }

    void Start () {
        display.text = "";
        RandomizeButtons();
        DeterminePresses();
    }

    void OnActivate()
    {
        display.text = "0";
        activated = true;
    }

    void PressButton(KMSelectable pressed)
    {
        if (moduleSolved != true && activated == true)
        {
            pressed.AddInteractionPunch(0.75f);
            audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, pressed.transform);
            if (Array.IndexOf(buttons, pressed) == correctPresses[curIndex])
            {
                curIndex++;
                display.text = curIndex.ToString();
                if (curIndex == correctPresses.Count)
                {
                    moduleSolved = true;
                    GetComponent<KMBombModule>().HandlePass();
                    Debug.LogFormat("[Back Buttons #{0}] All correct buttons have been pressed, module disarmed", moduleId);
                }
            }
            else
            {
                GetComponent<KMBombModule>().HandleStrike();
                List<int> pressedSoFar = new List<int>();
                for (int i = 0; i < curIndex; i++)
                    pressedSoFar.Add(correctPresses[i] + 1);
                Debug.LogFormat("[Back Buttons #{0}] Pressed button {1} but expected button {2}! Correct presses so far: {3}", moduleId, Array.IndexOf(buttons, pressed) + 1, correctPresses[curIndex] + 1, pressedSoFar.Count == 0 ? "None" : pressedSoFar.Join(" "));
            }
        }
    }

    private void RandomizeButtons()
    {
        for (int i = 0; i < 13; i++)
        {
            int rand = UnityEngine.Random.Range(0, allButtonTexts.Length);
            buttonTexts[i] = allButtonTexts[rand];
            buttons[i].GetComponentInChildren<TextMesh>().gameObject.transform.localScale = new Vector3(textXScales[rand], 0.0008f, 0.0008f);
            buttons[i].GetComponentInChildren<TextMesh>().text = allButtonTexts[rand];
        }
        Debug.LogFormat("[Back Buttons #{0}] The texts on the buttons in reading order are: {1}", moduleId, buttonTexts.Join(", "));
    }

    private void DeterminePresses()
    {
        char[] rules = new char[] { 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L' };
        List<int> trueBtns = new List<int>();
        List<int> falseBtns = new List<int>();
        bool useTrues = false;
        for (int i = 0; i < 13; i++)
        {
            if (buttonTexts[i].Equals("RETURN") || buttonTexts[i].Equals("BACK UP") || buttonTexts[i].Equals("<--"))
            {
                if (useTrues)
                {
                    for (int j = 0; j < trueBtns.Count; j++)
                        correctPresses.Add(trueBtns[j] - 1);
                    Debug.LogFormat("[Back Buttons #{0}] Button with the text \"{1}\" encountered | Press all buttons that have had true rules up to this point: {2}", moduleId, buttonTexts[i], trueBtns.Count == 0 ? "None" : trueBtns.Join(" "));
                    useTrues = false;
                }
                else
                {
                    for (int j = 0; j < falseBtns.Count; j++)
                        correctPresses.Add(falseBtns[j] - 1);
                    Debug.LogFormat("[Back Buttons #{0}] Button with the text \"{1}\" encountered | Press all buttons that have not had true rules up to this point: {2}", moduleId, buttonTexts[i], falseBtns.Count == 0 ? "None" : falseBtns.Join(" "));
                    useTrues = true;
                }
            }
            bool check = RuleCheck(i);
            if (check)
            {
                correctPresses.Add(i);
                trueBtns.Add(i + 1);
            }
            else
                falseBtns.Add(i + 1);
            if (i != 12)
                Debug.LogFormat("[Back Buttons #{0}] Rule {1}: {2}{3}", moduleId, rules[i], check, check ? " | Press " + (i + 1) : "");
            else
                Debug.LogFormat("[Back Buttons #{0}] The last button must always be pressed", moduleId);
        }
    }

    private bool RuleCheck(int rule)
    {
        switch (rule)
        {
            case 0:
                if (bomb.IsPortPresent(Port.DVI) || bomb.IsPortPresent(Port.Serial))
                    return true;
                break;
            case 1:
                if (buttonTexts[1] == "<-" || buttonTexts[1] == "<--" || buttonTexts[1] == "<---")
                    return true;
                break;
            case 2:
                if (bomb.GetSerialNumber().Contains("B") || bomb.GetSerialNumber().Contains("7"))
                    return true;
                break;
            case 3:
                if (bomb.GetModuleIDs().Contains("BrokenButtonsModule") || bomb.GetModuleIDs().Contains("numberedButtonsModule"))
                    return true;
                break;
            case 4:
                if (buttonTexts[5].Contains("K") || buttonTexts[5].Contains("N"))
                    return true;
                break;
            case 5:
                if (correctPresses.Count > 1 && correctPresses.Count < 4)
                    return true;
                break;
            case 6:
                if (bomb.IsIndicatorPresent("BOB") || bomb.IsIndicatorPresent("CLR"))
                    return true;
                break;
            case 7:
                foreach (string mod in bomb.GetModuleNames())
                {
                    if (mod.ToLower().Replace(" ", "").Contains("back"))
                        return true;
                }
                break;
            case 8:
                if (IsPrime(correctPresses.Count))
                    return true;
                break;
            case 9:
                if (buttonTexts[8].StartsWith("RE"))
                    return true;
                break;
            case 10:
                if (bomb.GetBatteryCount() < 2)
                    return true;
                break;
            case 11:
                int ct = 0;
                foreach (string text in buttonTexts)
                {
                    if (buttonTexts[11] == text)
                        ct++;
                }
                if (ct == 1)
                    return true;
                break;
            default:
                return true;
        }
        return false;
    }

    private bool IsPrime(int number)
    {
        if (number <= 1) return false;
        if (number == 2) return true;
        if (number % 2 == 0) return false;

        var boundary = (int)Math.Floor(Math.Sqrt(number));

        for (int i = 3; i <= boundary; i += 2)
            if (number % i == 0)
                return false;

        return true;
    }

    //twitch plays
    #pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} press <pos1> (pos2)... [Presses the button in the specified position (optionally include multiple positions)] | Valid positions are 1-13 in reading order";
    #pragma warning restore 414
    IEnumerator ProcessTwitchCommand(string command)
    {
        string[] parameters = command.Split(' ');
        if (Regex.IsMatch(parameters[0], @"^\s*press\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return null;
            if (parameters.Length == 1)
            {
                yield return "sendtochaterror Please specify the position(s) of the button(s) you wish to press!";
            }
            else
            {
                for (int i = 1; i < parameters.Length; i++)
                {
                    int temp = 0;
                    if (!int.TryParse(parameters[i], out temp))
                    {
                        yield return "sendtochaterror!f The specified position '" + parameters[i] + "' is invalid!";
                        yield break;
                    }
                    if (temp < 1 || temp > 13)
                    {
                        yield return "sendtochaterror!f The specified position '" + parameters[i] + "' is out of range 1-13!";
                        yield break;
                    }
                }
                for (int i = 1; i < parameters.Length; i++)
                {
                    buttons[int.Parse(parameters[i]) - 1].OnInteract();
                    yield return new WaitForSeconds(0.1f);
                }
            }
            yield break;
        }
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        while (!activated) yield return true;
        int start = curIndex;
        for (int i = start; i < correctPresses.Count; i++)
        {
            buttons[correctPresses[i]].OnInteract();
            yield return new WaitForSeconds(0.1f);
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RuleValidator : MonoBehaviour
{
    public CustomParser[] Rules;
    public Text Input;
    public GameObject ImgYes;
    public GameObject ImgNo;
    public Text ResultDisplay;

    public void Start()
    {
        ImgYes.SetActive(false);
        ImgNo.SetActive(false);
        ResultDisplay.gameObject.SetActive(false);
    }

    public void ProcessRules()
    {
        // Sort rules according to priority
        List<CustomParser> rules = new List<CustomParser>();
        for (int i = 0; i < Rules.Length; i++)
        {
            rules.Add(Rules[i]);
        }

        rules.Sort();

        for (int i = 0; i < rules.Count; i++)
        {
            if (rules[i].Validate(Input.text))
            {
                Valid(rules[i].Number);
                return;
            }
        }

        Invalid();
    }

    private void Valid(int number)
    {
        ImgYes.SetActive(true);
        ImgNo.SetActive(false);

        ResultDisplay.gameObject.SetActive(true);
        ResultDisplay.text = "Sentence is VALID according to rule " + number;

        StartCoroutine(ClearDisplay());
    }

    private void Invalid()
    {
        ImgYes.SetActive(false);
        ImgNo.SetActive(true);

        ResultDisplay.gameObject.SetActive(true);
        ResultDisplay.text = "Sentence is NOT VALID according to any of the algorithms";

        StartCoroutine(ClearDisplay());
    }

    IEnumerator ClearDisplay()
    {
        yield return new WaitForSeconds(4);


        ImgYes.SetActive(false);
        ImgNo.SetActive(false);
        ResultDisplay.gameObject.SetActive(false);
    }
}

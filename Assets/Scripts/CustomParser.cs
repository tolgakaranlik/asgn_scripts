using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName ="Rule", menuName = "Rule")]
public class CustomParser : ScriptableObject, IEquatable<CustomParser>, IComparable<CustomParser>
{
    public int Priority = 0;
    public int Number = 0;
    public string Rule = "";

    class RuleValue
    {
        public enum RuleOperator { And, Or };
        public RuleOperator Operator = RuleOperator.And;
        public enum ValueTypes { Word, StackEntry };
        public ValueTypes ValueType = ValueTypes.Word;
        public string ValueWord = "";
        public bool Inverted = false;
        public int ValueStackEntry = -1;
    }

    class RuleLogic
    {
        public List<RuleValue> Values = new List<RuleValue>();
    }

    List<RuleLogic> Stack = new List<RuleLogic>();

    public bool Validate(string sentence)
    {
        // prepare a list of words
        List<string> sentenceAsList = new List<string>();
        string[] words = sentence.Trim().Split(' ');
        for (int i = 0; i < words.Length; i++)
        {
            sentenceAsList.Add(words[i].ToUpper());
        }

        // parse rule first
        Stack.Clear();
        if(!ParseRule(Rule))
        {
            return false;
        }

        // test if sentence is correct
        return ProcessStackEntry(Stack.Count - 1, sentenceAsList);

    }

    private bool ProcessStackEntry(int i, List<string> sentence)
    {
        for (int j = 0; j < Stack[i].Values.Count; j++)
        {
            // treat all ORed values at once
            if (Stack[i].Values[j].Operator == RuleValue.RuleOperator.Or)
            {
                bool endOfArray = false;
                for (int k = j; k < Stack[i].Values.Count; k++)
                {
                    if(Stack[i].Values[k].Operator != RuleValue.RuleOperator.Or || k == Stack[i].Values.Count - 1)
                    {
                        bool found = false;
                        endOfArray = k == Stack[i].Values.Count - 1 && Stack[i].Values[k].Operator == RuleValue.RuleOperator.Or;
                        int m = endOfArray ? k + 1 : k;

                        // iterate from i to k to see if any of them will return true
                        for (int t = j; t < m; t++)
                        {
                            if (ProcessStackEntryValue(i, t, sentence))
                            {
                                found = true;
                                if (endOfArray)
                                {
                                    j = m;
                                } else
                                {
                                    j = m - 1;
                                }

                                break;
                            }
                        }

                        if (!found)
                        {
                            return false;
                        }

                        break;
                    }
                }

                continue;
            }

            // back to regular operation
            if(!ProcessStackEntryValue(i, j, sentence))
            {
                return false;
            }
        }

        return true;
    }

    private bool ProcessStackEntryValue(int i, int j, List<string> sentence)
    {
        if (Stack[i].Values[j].ValueType == RuleValue.ValueTypes.Word && !Stack[i].Values[j].Inverted && !sentence.Contains(Stack[i].Values[j].ValueWord))
        {
            Debug.Log("Sentence is not okay for rule " + Number + ": word " + Stack[i].Values[j].ValueWord + " not present");

            return false;
        }
        else if (Stack[i].Values[j].ValueType == RuleValue.ValueTypes.Word && Stack[i].Values[j].Inverted && sentence.Contains(Stack[i].Values[j].ValueWord))
        {
            Debug.Log("Sentence is not okay for rule " + Number + ": word " + Stack[i].Values[j].ValueWord + " present");

            return false;
        }
        else if (Stack[i].Values[j].ValueType == RuleValue.ValueTypes.StackEntry)
        {
            bool result = ProcessStackEntry(Stack[i].Values[j].ValueStackEntry, sentence);
            return Stack[i].Values[j].Inverted ? !result : result;
        }

        return true;
    }

    bool ParseRule(string rule)
    {
        int parseResult;

        // parse inside parenthesis first
        if (rule.Contains(")"))
        {
            int indexOfLeft = rule.IndexOf("(");
            if(indexOfLeft == -1)
            {
                return false;
            }

            int indexOfRight = rule.IndexOf(")");
            if(indexOfRight <= indexOfLeft)
            {
                return false;
            }

            string subPhrase = rule.Substring(indexOfLeft + 1, indexOfRight - indexOfLeft - 1);
            parseResult = ParseRulePart(subPhrase);
            if (parseResult == -1)
            {
                return false;
            }

            rule = rule.Substring(0, indexOfLeft) + "#" + parseResult + rule.Substring(indexOfRight + 1);
            return ParseRule(rule.Trim());
        }

        // parse the actual/final string
        parseResult = ParseRulePart(rule);
        if (parseResult != -1)
        {
            return true;
        }

        return false;
    }

    private int ParseRulePart(string rule)
    {
        RuleLogic result = new RuleLogic();
        RuleValue value;
        string[] items = rule.Split(' ');
        int stackIndex;
        bool inverted;
        RuleValue.RuleOperator nextOperator = RuleValue.RuleOperator.And;

        for (int i = 0; i < items.Length; i++)
        {
            // process ! sign first
            inverted = items[i].StartsWith("!");
            if(inverted)
            {
                items[i] = items[i].Substring(1);
            }
            
            // process previous stack entries
            if (items[i].StartsWith("#"))
            {
                value = new RuleValue();
                value.ValueType = RuleValue.ValueTypes.Word;
                if (int.TryParse(items[i].Substring(1), out stackIndex))
                {
                    value.Operator = nextOperator;
                    value.ValueType = RuleValue.ValueTypes.StackEntry;
                    value.ValueStackEntry = stackIndex;
                    value.Inverted = inverted;
                } else
                {
                    Debug.LogError("Invalid stack entry: " + items[i].Substring(1));
                }

                result.Values.Add(value);
            }
            // process | sign
            else if (items[i].StartsWith("|"))
            {
                nextOperator = RuleValue.RuleOperator.Or;

                if(result.Values.Count > 0)
                {
                    result.Values[result.Values.Count - 1].Operator = RuleValue.RuleOperator.Or;
                }
            }
            // process & sign
            else if (items[i].StartsWith("&"))
            {
                if (!items[i].Contains("]"))
                {
                    value = new RuleValue();
                    value.ValueType = RuleValue.ValueTypes.Word;
                    value.ValueWord = items[i].Substring(1).ToUpper();
                    value.Inverted = inverted;
                    value.Operator = nextOperator;

                    result.Values.Add(value);
                    nextOperator = RuleValue.RuleOperator.And;
                }
                else
                {
                    // process [/]
                    string baseWord = items[i].Substring(1).ToUpper();
                    baseWord = baseWord.Substring(0, baseWord.IndexOf("["));
                    nextOperator = inverted ? RuleValue.RuleOperator.And : RuleValue.RuleOperator.Or;

                    string alternateWord = items[i].Substring(1).ToUpper();
                    alternateWord = alternateWord.Substring(alternateWord.IndexOf("[") + 1);
                    alternateWord = alternateWord.Substring(0, alternateWord.Length - 1);

                    value = new RuleValue();
                    value.ValueType = RuleValue.ValueTypes.Word;
                    value.ValueWord = baseWord.ToUpper();
                    value.Inverted = inverted;
                    value.Operator = nextOperator;

                    result.Values.Add(value);

                    string[] alternateWords = alternateWord.Split('/');
                    for (int a = 0; a < alternateWords.Length; a++)
                    {
                        value = new RuleValue();
                        value.ValueType = RuleValue.ValueTypes.Word;
                        value.ValueWord = baseWord + alternateWords[a].ToUpper();
                        value.Inverted = inverted;
                        value.Operator = nextOperator;

                        result.Values.Add(value);
                    }

                    nextOperator = RuleValue.RuleOperator.And;
                }
            }
            else
            {
                Debug.LogError("Unexpected item in the sentence: " + items[i]);
                return -1;
            }
        }

        Stack.Add(result);
        return Stack.Count - 1;
    }

    public int CompareTo(CustomParser other)
    {
        return other.Priority - Priority;
    }

    public bool Equals(CustomParser other)
    {
        return other.Priority == Priority;
    }
}

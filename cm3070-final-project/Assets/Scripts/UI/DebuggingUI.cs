using System.Collections.Generic;
using ModularVehicleSimulator.Debugging;
using TMPro;
using UnityEngine;

namespace ModularVehicleSimulator.UI
{
    public class DebuggingUI : MonoBehaviour
    {
        [SerializeField] private DebuggingManager debuggingManager;
        [SerializeField] private TextMeshProUGUI debugTextPrefab;
        private Dictionary<string, TextMeshProUGUI> debugMeshProUGUIs = new Dictionary<string, TextMeshProUGUI>();

        private void OnEnable()
        {
            InvokeRepeating("UpdateDebugValues", 0f, 0.5f);
        }

        private void OnDisable()
        {
            CancelInvoke("UpdateDebugValues");
        }

        private void UpdateDebugValues()
        {
            foreach (DebuggingTool tool in debuggingManager.Tools)
            {
                foreach (KeyValuePair<string, string> pair in tool.GetDebugValues())
                {
                    TextMeshProUGUI text;
                    if (!debugMeshProUGUIs.ContainsKey(pair.Key))
                    {
                        text = Instantiate(debugTextPrefab, transform);
                        debugMeshProUGUIs.Add(pair.Key, text);
                    }
                    else
                    {
                        text = debugMeshProUGUIs[pair.Key];
                    }
                    text.color = tool.DebugColor;
                    text.text = $"{pair.Key}: {pair.Value}";
                }
            }
        }

    }    
}

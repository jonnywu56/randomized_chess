using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Handles mod UI shown in modMenu
public class ModUI : MonoBehaviour
{
    // Prefab for mod entries
    [SerializeField] private GameObject modTemplate;

    // Tracks currently listed mods
    private List<GameObject> modsList = new List<GameObject>();
    // Used to find mod names and descriptions
    private ModsDict modsDict = new ModsDict();

    // Adds mod to display, adds corresponding GameObject to modsList
    public void AddMods(int dictNum){
        // Adds all mods of dictNum to modsList and displays them
        for(int modNum=0;modNum<modsDict.DictLength(dictNum);modNum++){
            GameObject newMove = Instantiate(modTemplate,Vector3.zero,Quaternion.identity,this.transform);
            newMove.name = "Mod "+dictNum.ToString()+"."+modNum.ToString();
            (string modName, string modDesc) mod = modsDict.Print(dictNum,modNum);
            newMove.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = mod.modName;
            newMove.transform.GetChild(1).GetComponent<TextMeshProUGUI>().text = mod.modDesc;
            modsList.Add(newMove);
            this.GetComponent<RectTransform>().sizeDelta = new Vector2(600,30*(modsList.Count)); 
        }

        // Scroll to top of move list
        GameObject.Find("/Canvas/ModsMenu/Scroll").GetComponent<ScrollRect>().verticalNormalizedPosition = 1f;
    }

    // Removes all mods from modsList, destroys all corresponding GameObjects
    public void DestroyMods(){
        while (modsList.Count>0){
            GameObject mod = modsList[modsList.Count-1];
            modsList.RemoveAt(modsList.Count-1);
            Destroy(mod);
        }
        this.GetComponent<RectTransform>().sizeDelta = new Vector2(0,0);
    }
    
}

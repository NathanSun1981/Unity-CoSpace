using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using System.IO;
using UnityEngine.UI;
using System;

public class GetCases : MonoBehaviour
{
    // Start is called before the first frame update
    //public Text text;
    private int fileIndex;
    void Start()
    {
        StartCoroutine(LoadingCases());

    }

    // Update is called once per frame
    void Update()
    {

        //text.text = string.Format("Loading case {0}", fileIndex);
    }

    public IEnumerator LoadingCases()
    {
        string fullPath = "/Cache/case-resources/";

        string ToSavePath = "SKPModules/";
        string ToSaveImages = "CaseImages/";
        if (Directory.Exists(fullPath))
        {
            System.IO.DirectoryInfo direction = new DirectoryInfo(fullPath);
            FileInfo[] files = direction.GetFiles("*", SearchOption.AllDirectories);
            int j = 1;

            for (int i = 0; i < files.Length; i++)
            {

                if (files[i].Name.EndsWith(".meta"))
                {
                    continue;
                }
                else if (files[i].Name.Equals("static-iso-model.png") || files[i].Name.Equals("static-iso-model.jpg"))
                {

                    if (Directory.Exists(ToSaveImages) && !File.Exists(ToSaveImages + "/" + files[i].Directory.Name + files[i].Extension))
                    {
                        File.Copy(files[i].FullName, ToSaveImages + "/" + files[i].Directory.Name + files[i].Extension);
                    }

                    fileIndex = j;

                    
                    byte[] byteArray = File.ReadAllBytes(files[i].FullName);
                    Texture2D texture = new Texture2D(2, 2);
                    texture.LoadImage(byteArray);

                    GameObject cases = new GameObject("Button", typeof(Button));
                    cases.transform.parent = this.gameObject.transform;
                    cases.name = files[i].Directory.Name;
                    cases.transform.localPosition = new Vector3(0, 0, 0);
                    cases.transform.localScale = new Vector3(1f, 1f, 1f);
                    cases.transform.localRotation = Quaternion.identity;
                    yield return null;

                    
                    Sprite sp = Sprite.Create(texture, new Rect(0.0f, 0.0f, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                    cases.AddComponent<Image>().sprite = sp;                   
                    cases.AddComponent<CaseResponse>();
                    GameObject text = new GameObject("Text", typeof(Text));
                    text.transform.SetParent(cases.transform);
                    //text.transform.parent = cases.transform;
                    text.transform.rotation = cases.transform.rotation;
                    text.transform.localScale = cases.transform.localScale;
                    text.transform.localPosition = Vector3.zero;
                    text.GetComponent<RectTransform>().sizeDelta = cases.transform.parent.GetComponent<GridLayoutGroup>().cellSize;

                    var style = text.GetComponent<Text>();
                    style.text = cases.name;
                    style.fontSize = 14;
                    style.font = Resources.GetBuiltinResource(typeof(Font), "Arial.ttf") as Font;
                    style.alignment = TextAnchor.LowerCenter;
                    style.color = Color.black;
                    

                    j++;

                    yield return null;
                }
                else if (Directory.Exists(ToSavePath) && files[i].Name.Equals("hd-dae.skp") && !File.Exists(ToSavePath + files[i].Directory.Name + ".skp"))
                {
                    File.Copy(files[i].FullName, ToSavePath + files[i].Directory.Name + ".skp");
                    yield return null;                    
                }

            }
        }
        else
        {

            Sprite[] sps = Resources.LoadAll<Sprite>("Images");
            foreach (var sp in sps)
            {
                GameObject cases = (GameObject)Instantiate(Resources.Load("CaseMenu"));
                cases.transform.SetParent(this.gameObject.transform);
                cases.transform.localPosition = new Vector3(0, 0, 0);
                cases.transform.localScale = new Vector3(1f, 1f, 1f);
                cases.transform.localRotation = Quaternion.identity;
                cases.name = sp.name;
                yield return null;

                //Sprite sp = Sprite.Create(texture, new Rect(0.0f, 0.0f, texture.width, texture.height), new Vector2(0.5f, 0.5f));

                cases.GetComponent<Image>().sprite = sp;
                cases.transform.Find("Text").GetComponent<Text>().text = sp.name;

            }



        }

        yield return null;

    }


}

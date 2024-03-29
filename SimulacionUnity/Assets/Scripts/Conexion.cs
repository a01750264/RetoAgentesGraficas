// TC2008B Modelaci�n de Sistemas Multiagentes con gr�ficas computacionales
// C# client to interact with Python server via GET
// Sergio Ruiz-Loza, Ph.D. March 2021

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;


public class Conexion : MonoBehaviour
{
    List<List<Vector3>> positions;

    public GameObject[] spheres;
    public float timeToUpdate = 5.0f;
    private float timer;
    public float dt;
    public int semaforo1;
    public int semaforo2;
    public List<int> fronts;
    public GameObject semaforoVerde1;
    public GameObject semaforoVerde2;
    public GameObject semaforoAmarillo1;
    public GameObject semaforoAmarillo2;
    public GameObject semaforoRojo1;
    public GameObject semaforoRojo2;


    // IEnumerator - yield return
    IEnumerator SendData(string data)
    {
        WWWForm form = new WWWForm();
        form.AddField("bundle", "the data");
        string url = "http://reto-equipo6-2.us-south.cf.appdomain.cloud/multiagentes";
        //string url = "http://localhost:8585/multiagentes";
        using (UnityWebRequest www = UnityWebRequest.Post(url, form))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(data);
            www.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
            //www.SetRequestHeader("Content-Type", "text/html");
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();          // Talk to Python
            //if (www.isNetworkError || www.isHttpError)
            if (www.result == UnityWebRequest.Result.Success)
            {
                //Debug.Log(www.downloadHandler.text);    // Answer from Python
                //Debug.Log("Form upload complete!");
                //Data tPos = JsonUtility.FromJson<Data>(www.downloadHandler.text.Replace('\'', '\"'));
                //Debug.Log(tPos);

                List<Vector3> newPositions = new List<Vector3>();
                string txt = www.downloadHandler.text.Replace('\'', '\"');
                print(txt);
                //txt = txt.TrimStart('"', '{', 'd', 'a', 't', 'a', ':', '[');
                txt = txt.TrimStart('"', '[', '{');
                txt = "{\"" + txt;
                txt = txt.TrimEnd(']', '}');
                txt = txt + '}';
                string[] strs = txt.Split(new string[] { "}, {" }, StringSplitOptions.None);
                Debug.Log("strs.Length:" + strs.Length);
                for (int i = 0; i < strs.Length; i++)
                {
                    strs[i] = strs[i].Trim();
                    if (i == 0) strs[i] = strs[i] + '}';
                    else if (i == strs.Length - 1) strs[i] = '{' + strs[i];
                    else strs[i] = '{' + strs[i] + '}';
                    Debug.Log(strs[i]);
                    Vector3 test = JsonUtility.FromJson<Vector3>(strs[i]);
                    print(test);
                    newPositions.Add(test);
                }

                List<Vector3> poss = new List<Vector3>();
                for (int s = 0; s < spheres.Length; s++)
                {
                    //spheres[s].transform.localPosition = newPositions[s];
                    poss.Add(newPositions[s]);
                }
                positions.Add(poss);
            }
            else
            {
                Debug.Log(www.error);
            }
        }

    }

    IEnumerator Semaforos(string data)
    {
        WWWForm form = new WWWForm();
        form.AddField("bundle", "the data");
        string urlSemaforos = "http://reto-equipo6-2.us-south.cf.appdomain.cloud/semaforos";
        //string urlSemaforos = "http://localhost:8585/semaforos";
        using (UnityWebRequest www = UnityWebRequest.Post(urlSemaforos, form))
        {
            //www.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
            //www.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
            //www.SetRequestHeader("Content-Type", "text/html");
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();          // Talk to Python
            //if (www.isNetworkError || www.isHttpError)
            if (www.result == UnityWebRequest.Result.Success)
            {
                string txt = www.downloadHandler.text.Replace('\'', '\"');
                txt = txt.TrimStart('"', '[', '{');
                txt = txt.TrimEnd(']', '}');
                semaforo1 = (int)Char.GetNumericValue(txt[0]);
                semaforo2 = (int)Char.GetNumericValue(txt[3]);
            }
            else
            {
                Debug.Log(www.error);
            }
        }

    }

    // Start is called before the first frame update
    void Start()
    {
        positions = new List<List<Vector3>>();
        Debug.Log(spheres.Length);
#if UNITY_EDITOR
        //string call = "WAAAAASSSSSAAAAAAAAAAP?";
        Vector3 fakePos = new Vector3(3.44f, 0, -15.707f);
        string json = EditorJsonUtility.ToJson(fakePos);
        //StartCoroutine(SendData(call));
        StartCoroutine(SendData(json));
        StartCoroutine(Semaforos(json));
        timer = timeToUpdate;
#endif
    }

    // Update is called once per frame
    void Update()
    {
        /*
         *    5 -------- 100
         *    timer ----  ?
         */
        timer -= Time.deltaTime;
        dt = 1.0f - (timer / timeToUpdate);

        if (timer < 0)
        {
#if UNITY_EDITOR
            timer = timeToUpdate; // reset the timer
            Vector3 fakePos = new Vector3(3.44f, 0, -15.707f);
            string json = EditorJsonUtility.ToJson(fakePos);
            StartCoroutine(SendData(json));
            StartCoroutine(Semaforos(json));
#endif
        }


        if (positions.Count > 1)
        {
            for (int s = 0; s < spheres.Length; s++)
            {
                // Get the last position for s
                List<Vector3> last = positions[positions.Count - 1];
                // Get the previous to last position for s
                List<Vector3> prevLast = positions[positions.Count - 2];
                // Interpolate using dt
                Vector3 interpolated = Vector3.Lerp(prevLast[s], last[s], dt);
                spheres[s].transform.localPosition = interpolated;

                if (last[s] != prevLast[s])
                {
                    Vector3 dir = last[s] - prevLast[s];
                    spheres[s].transform.rotation = Quaternion.LookRotation(dir);
                }
            }
        }


        // SEMAFOROS
        if (semaforo1 == 1)
        {
            semaforoAmarillo1.GetComponent<Renderer>().enabled = false;
            semaforoRojo1.GetComponent<Renderer>().enabled = false;
            semaforoVerde1.GetComponent<Renderer>().enabled = true;
        }
        else if (semaforo1 == 2)
        {
            semaforoAmarillo1.GetComponent<Renderer>().enabled = true;
            semaforoRojo1.GetComponent<Renderer>().enabled = false;
            semaforoVerde1.GetComponent<Renderer>().enabled = false;
        }
        else if (semaforo1 == 3)
        {
            semaforoAmarillo1.GetComponent<Renderer>().enabled = false;
            semaforoRojo1.GetComponent<Renderer>().enabled = true;
            semaforoVerde1.GetComponent<Renderer>().enabled = false;
        }

        if (semaforo2 == 1)
        {
            semaforoAmarillo2.GetComponent<Renderer>().enabled = false;
            semaforoRojo2.GetComponent<Renderer>().enabled = false;
            semaforoVerde2.GetComponent<Renderer>().enabled = true;
        }
        else if (semaforo2 == 2)
        {
            semaforoAmarillo2.GetComponent<Renderer>().enabled = true;
            semaforoRojo2.GetComponent<Renderer>().enabled = false;
            semaforoVerde2.GetComponent<Renderer>().enabled = false;
        }
        else if (semaforo2 == 3)
        {
            semaforoAmarillo2.GetComponent<Renderer>().enabled = false;
            semaforoRojo2.GetComponent<Renderer>().enabled = true;
            semaforoVerde2.GetComponent<Renderer>().enabled = false;
        }
    }
}


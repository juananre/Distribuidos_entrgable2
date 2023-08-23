using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using System.Linq;

public class TEST : MonoBehaviour
{
    [SerializeField] string ApiUrl = "https://sid-restapi.onrender.com/api/";
    [SerializeField] TMP_InputField UsernameInputFild;
    [SerializeField] TMP_InputField PasswordInputFild;

    [SerializeField] private string Token;
    [SerializeField] private string Username;
    [SerializeField] GameObject panel;

    [Header("Score list references")]
    [SerializeField] TextMeshProUGUI[] scoreUsernames;
    [SerializeField] TextMeshProUGUI[] scoreValues;

    [Header("Update score reference")]
    [SerializeField] TMP_InputField scoreInputField;

    void Start()
    {
        // Obtener el token de PlayerPrefs utilizando la clave correcta
        Token = PlayerPrefs.GetString("token");
        if (string.IsNullOrEmpty(Token))
        {
            Debug.Log("No hay token guardado");
        }
        else
        {
            // No sobrescrir UsernameInputFild y PasswordInputFild aquí
            // UsernameInputFild = GameObject.Find("UsernameInputFild").GetComponent<TMP_InputField>();
            // PasswordInputFild = GameObject.Find("PasworrnputFild").GetComponent<TMP_InputField>();

            Username = PlayerPrefs.GetString("username");
            StartCoroutine(GetPerfil(Username));
        }
    }
    public void Regist()
    {
        AuthData auntData = new AuthData();
        auntData.username = UsernameInputFild.text;
        auntData.password = PasswordInputFild.text;
        string json=JsonUtility.ToJson(auntData);  
        
        StartCoroutine(sendRegister(json));
    }
    public void login()
    {
        AuthData auntData = new AuthData();
        auntData.username = UsernameInputFild.text;
        auntData.password = PasswordInputFild.text;
        string json = JsonUtility.ToJson(auntData);

        StartCoroutine(sendlogin(json));
    }
    public void LoginOut()
    {
        // Borrar el token y el nombre de usuario almacenados en PlayerPrefs
        PlayerPrefs.DeleteKey("token");
        PlayerPrefs.DeleteKey("username");

        // Aquí puedes realizar otras acciones, como cambiar de escena o desactivar objetos, según tus necesidades
        panel.SetActive(false);

    }
    public void UpdateUserScore()
    {
        UserJson user = new UserJson();
        user.username = Username;

        if (int.TryParse(scoreInputField.text, out _))
        {
            user.data.score = int.Parse(scoreInputField.text);
        }

        string postData = JsonUtility.ToJson(user);
        Debug.Log(postData);
        StartCoroutine(UpdateScore(postData));
    }

    IEnumerator sendRegister(string json)
    {
        UnityWebRequest request = UnityWebRequest.Put($"{ApiUrl}usuarios", json);
        request.SetRequestHeader("Content-Type","application/json");
        request.method = "POST";
        yield return request.SendWebRequest();

        if (request.isNetworkError)
        {
            Debug.Log("cagaste" + request.error);
        }
        else
        {
            Debug.Log(request.downloadHandler.text);

            if (request != null && request.responseCode == 200)
            {
                AuthData data = JsonUtility.FromJson<AuthData>(request.downloadHandler.text);
                Debug.Log("se registro el usuario con id" +data.usuario.username + data.usuario._id);
                panel.SetActive(true);

            }
            else
            {
                Debug.Log(request.error);
            }

        }
    }
    IEnumerator sendlogin(string json)
    {
        UnityWebRequest request = UnityWebRequest.Put(ApiUrl + "auth/login", json);
        request.SetRequestHeader("Content-Type", "application/json");
        request.method = "POST";
        yield return request.SendWebRequest();

        if (request.isNetworkError)
        {
            Debug.Log("cagaste" + request.error);
        }
        else
        {
            Debug.Log(request.downloadHandler.text);

            if (request != null && request.responseCode == 200)
            {
                AuthData data = JsonUtility.FromJson<AuthData>(request.downloadHandler.text);
                Debug.Log("se inicion el usuario" + data.usuario.username);
                PlayerPrefs.SetString("token", data.token);
                PlayerPrefs.SetString("username",data.usuario.username);
                Debug.Log("token" + data.token);
                panel.SetActive(true);

                StartCoroutine(RetrieveAndSetScores());
            }
            else
            {
                Debug.Log(request.error);
            }

        }
    }
    IEnumerator GetPerfil(string username)
    {
        UnityWebRequest request = UnityWebRequest.Get(ApiUrl + "usuarios/" + username);
        request.SetRequestHeader("x-token", Token);
        yield return request.SendWebRequest();
        if (request.isNetworkError)
        {
            Debug.Log("cagaste" + request.error);
        }
        else
        {
            Debug.Log(request.downloadHandler.text);

            if (request != null && request.responseCode == 200)
            {
                AuthData data = JsonUtility.FromJson<AuthData>(request.downloadHandler.text);
                Debug.Log("su puntaje es: " + data.usuario.data.score);

                panel.SetActive(true);
                StartCoroutine(RetrieveAndSetScores());
            }
            else
            {
                Debug.Log(request.error);
            }
        }
    }
    IEnumerator RetrieveAndSetScores()
    {
        UnityWebRequest request = UnityWebRequest.Get($"{ApiUrl}usuarios");
        request.SetRequestHeader("x-token", Token);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.ConnectionError)
        {
            Debug.Log("NETWORK ERROR :" + request.error);
        }
        else
        {
            Debug.Log(request.downloadHandler.text);

            if (request.responseCode == 200)
            {
                Userlist jsonList = JsonUtility.FromJson<Userlist>(request.downloadHandler.text);
                Debug.Log(jsonList.usuarios.Count);

                foreach (User a in jsonList.usuarios)
                {
                    Debug.Log(a.username);
                }

                List<User> lista = jsonList.usuarios;
                List<User> listaOrdenada = lista.OrderByDescending(u => u.data.score).ToList<User>();
        

                int len = scoreUsernames.Length;
                for (int i = 0; i < len; i++)
                {
                    scoreUsernames[i].text = listaOrdenada[i].username;
                    scoreValues[i].text = listaOrdenada[i].data.score.ToString();
                }
            }
            else
            {
                string mensaje = "Status :" + request.responseCode;
                mensaje += "\ncontent-type:" + request.GetResponseHeader("content-type");
                mensaje += "\nError :" + request.error;
                Debug.Log(mensaje);
            }
        }
    }
    IEnumerator UpdateScore(string postData)
    {
        UnityWebRequest www = UnityWebRequest.Put($"{ApiUrl}usuarios", postData);

        www.method = "PATCH";
        www.SetRequestHeader("x-token", Token);
        www.SetRequestHeader("Content-Type", "application/json");

        yield return www.SendWebRequest();
        if (www.result == UnityWebRequest.Result.ConnectionError)
        {
            //index.SetActive(false);
            //login.SetActive(true);
            Debug.Log("NETWORK ERROR :" + www.error);
        }
        else
        {
            Debug.Log(www.downloadHandler.text);

            if (www.responseCode == 200)
            {

                AuthJson jsonData = JsonUtility.FromJson<AuthJson>(www.downloadHandler.text);
                StartCoroutine(RetrieveAndSetScores());
                Debug.Log(jsonData.usuario.username + " se actualizo " + jsonData.usuario.data.score);
            }
            else
            {
                string mensaje = "Status :" + www.responseCode;
                mensaje += "\ncontent-type:" + www.GetResponseHeader("content-type");
                mensaje += "\nError :" + www.error;
                Debug.Log(mensaje);
            }

        }
    }

}
[System.Serializable]
public class AuthData
{
    public string username;
    public string password;
    public User usuario;
    public string token;

}
[System.Serializable]
public class User
{
    public string _id;
    public string username;
    public string password;
    public bool estado;
    public UserData data;
}
[System.Serializable]
public class UserData
{
    public int score;
}
[System.Serializable]
public class Userlist
{
    public List<User> usuarios;
}
[System.Serializable]
public class UserJson
{
    public string _id;
    public string username;
    public string password;

    public UserData data;

    public UserJson()
    {
        data = new UserData();
    }
    public UserJson(string username, string password)
    {
        this.username = username;
        this.password = password;
        data = new UserData();
    }

}
[System.Serializable]
public class AuthJson
{
    public UserJson usuario;
    public UserData data;
    public string token;
}
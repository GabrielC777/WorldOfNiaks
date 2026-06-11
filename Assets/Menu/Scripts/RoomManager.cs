using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.SceneManagement;
using System.IO;

public class RoomManager : MonoBehaviourPunCallbacks {

  public static RoomManager Instance;

  private void Awake() {

        Debug.Log("[DEBUG TOTAL]</color> RoomManager ha despertado en la escena: " + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        if (Instance) {
          Destroy(gameObject);
          return;
        }
        DontDestroyOnLoad(gameObject);
        Instance = this;
  }

  public override void OnEnable() {
    base.OnEnable();
    SceneManager.sceneLoaded += OnSceneLoaded;
  }

  public override void OnDisable() {
    base.OnDisable();
  }

    void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
    {
        Debug.Log("<color=yellow>[DEBUG ROOM MANAGER]</color> Escena cargada: " + scene.name + " (Index: " + scene.buildIndex + ")");

        if (scene.buildIndex == 1) 
        {
            Debug.Log("<color=yellow>[DEBUG ROOM MANAGER]</color> Instanciando PlayerManager en la carpeta Resources/PhotonPrefabs...");
            PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", "PlayerManager"), Vector3.zero, Quaternion.identity);
        }
    }
}

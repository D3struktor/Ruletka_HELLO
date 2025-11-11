using System;                              
using System.Collections.Generic;          
using Fusion;
using Fusion.Sockets;
using UnityEngine;

public class RunnerHandler : MonoBehaviour, INetworkRunnerCallbacks
{
    [Header("Session")]
    [SerializeField] private string sessionName = "chaos-duel";
    [SerializeField] private int gameSceneBuildIndex = 1;

    [Header("Spawn")]
    [SerializeField] private NetworkPrefabRef playerPrefab;

    private NetworkRunner Runner;

    public async void StartHost()
    {
        Runner = gameObject.AddComponent<NetworkRunner>();
        Runner.ProvideInput = true;

        var sceneMgr = gameObject.AddComponent<NetworkSceneManagerDefault>();
        await Runner.StartGame(new StartGameArgs {
            GameMode     = GameMode.Host,
            SessionName  = sessionName,
            Scene        = SceneRef.FromIndex(gameSceneBuildIndex),
            SceneManager = sceneMgr
        });
    }

    public async void StartClient()
    {
        Runner = gameObject.AddComponent<NetworkRunner>();
        Runner.ProvideInput = true;

        var sceneMgr = gameObject.AddComponent<NetworkSceneManagerDefault>();
        await Runner.StartGame(new StartGameArgs {
            GameMode     = GameMode.Client,
            SessionName  = sessionName,
            SceneManager = sceneMgr
        });
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        var data = new PlayerInputData();
        data.Move = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized;

        bool jumpDown  = Input.GetKeyDown(KeyCode.Space);
        bool jumpHeld  = Input.GetKey(KeyCode.Space);
        bool attack    = Input.GetMouseButtonDown(0);

        data.JumpPressed = jumpDown;
        data.JumpHeld    = jumpHeld;
        data.AttackPressed = attack;

        input.Set(data);
    }



    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (!runner.IsServer) return;
        Vector3 pos = FindSpawnPoint();
        runner.Spawn(playerPrefab, pos, Quaternion.identity, player);
    }

    Vector3 FindSpawnPoint()
    {
        var sm = UnityEngine.Object.FindAnyObjectByType<SpawnManager2D>();
        return sm ? sm.GetSpawnPoint() : Vector3.zero;
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken token) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
}

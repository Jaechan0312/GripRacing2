using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GripReceiver : MonoBehaviour
{
    public static GripReceiver Instance { get; private set; }

    [Header("실시간 수신 데이터")]
    public float RawGripValue = 0f;
    public float ConvertedGripKg = 0f;

    [Header("역대 최고 기록 저장소")]
    public float maxGripRecordKg = 0f;

    [Header("상태 변수")]
    [HideInInspector] public bool isNewDataArrived = false;
    public bool isPaused = false;
    public bool isMeasuring = false;
    public bool isBleConnected = false;

    [Header("UDP Network Settings")]
    public int port = 5005;

    private Thread receiveThread;
    private UdpClient client;
    private string latestRawData = "0";
    private readonly object lockObject = new object();
    private bool isAppRunning = true;

    private readonly Queue<string> commandQueue = new Queue<string>();

    private const string GAME_SCENE_NAME = "SampleScene";
    private const string GRAPH_SCENE_NAME = "GraphScene";
    private const string HOME_SCENE_NAME = "MenuScene";

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            AutoStartBatchFile();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        isAppRunning = true;
        receiveThread = new Thread(new ThreadStart(ReceiveData));
        receiveThread.IsBackground = true;
        receiveThread.Start();
    }

    private void AutoStartBatchFile()
    {
        try
        {
            string desktopFolderPath = @"C:\Users\LG\OneDrive - 중앙대학교\바탕 화면";
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = "cmd.exe";
            startInfo.Arguments = $"/c cd /d \"{desktopFolderPath}\" && python ble_receiver.py";
            startInfo.WorkingDirectory = desktopFolderPath;

            // 💡 [화면 가림 방지 최적화] 창을 완전히 숨기면 실행 여부를 알 수 없어 답답하므로,
            // 검은 창이 대빵만하게 뜨지 않고 윈도우 작업 표시줄에 '최소화' 상태로 조용히 켜지도록 변경합니다.
            startInfo.CreateNoWindow = false;
            startInfo.WindowStyle = ProcessWindowStyle.Minimized; // 화면을 가리지 않고 아래로 최소화됨
            startInfo.UseShellExecute = true;

            Process.Start(startInfo);
            UnityEngine.Debug.Log("🚀 [GripReceiver] 화면 가림 방지(최소화 모드)로 파이썬 중계 프로그램을 실행했습니다!");
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError($"❌ [GripReceiver] 배치 파일 자동 실행 실패: {ex.Message}");
        }
    }

    void Update()
    {
        if (!isBleConnected && Input.anyKeyDown)
        {
            if (!Input.GetMouseButtonDown(0) && !Input.GetMouseButtonDown(1) && !Input.GetMouseButtonDown(2))
            {
                UnityEngine.Debug.Log("🔄 [GripReceiver] 사용자의 키보드 입력 감지! 파이썬을 최소화 모드로 재실행합니다.");
                AutoStartBatchFile();
            }
        }

        string cmd = null;
        lock (lockObject)
        {
            if (commandQueue.Count > 0)
            {
                cmd = commandQueue.Dequeue();
            }
        }

        if (!string.IsNullOrEmpty(cmd))
        {
            if (cmd == "BLE_CONNECTED")
            {
                isBleConnected = true;
                isMeasuring = true;
                UnityEngine.Debug.Log("🔵 [GripReceiver] 메인 루프에서 블루투스 연결 성공 감지 완료!");

                MenuStatusUI menuUI = FindAnyObjectByType<MenuStatusUI>();
                if (menuUI != null) menuUI.ShowConnectedText(true);
                return;
            }

            if (cmd == "BLE_DISCONNECTED")
            {
                isBleConnected = false;
                UnityEngine.Debug.LogWarning("⚠️ [GripReceiver] 블루투스 연결이 끊어졌습니다.");

                MenuStatusUI menuUI = FindAnyObjectByType<MenuStatusUI>();
                if (menuUI != null) menuUI.ShowConnectedText(false);
                return;
            }

            if (cmd == "BLE_FAIL")
            {
                isBleConnected = false;
                UnityEngine.Debug.LogError("🚨 [GripReceiver] 블루투스 송신이 잡히지 않아 게임을 자동으로 종료합니다!");
                ExitGameProcess();
                return;
            }

            string currentScene = SceneManager.GetActiveScene().name;

            if (cmd == "START_7")
            {
                string target = (currentScene == HOME_SCENE_NAME) ? GRAPH_SCENE_NAME : HOME_SCENE_NAME;
                isPaused = false;
                isMeasuring = (currentScene == HOME_SCENE_NAME);
                LoadSceneSafe(target);
            }
            else if (cmd == "START_4")
            {
                string target = (currentScene == HOME_SCENE_NAME) ? GAME_SCENE_NAME : HOME_SCENE_NAME;
                isPaused = false;
                isMeasuring = (currentScene == HOME_SCENE_NAME);
                LoadSceneSafe(target);
            }
            else if (cmd == "STOP")
            {
                isPaused = false;
                RawGripValue = 0f;
                ConvertedGripKg = 0f;
                isNewDataArrived = true;
                isMeasuring = false;

                if (currentScene != HOME_SCENE_NAME) LoadSceneSafe(HOME_SCENE_NAME);
            }
            else if (cmd == "PAUSE")
            {
                if (currentScene == GAME_SCENE_NAME) isPaused = true;
            }
            else if (cmd == "RESUME")
            {
                if (currentScene == GAME_SCENE_NAME) isPaused = false;
            }
        }

        if (!isPaused && isMeasuring)
        {
            string currentRaw;
            lock (lockObject)
            {
                currentRaw = latestRawData;
            }

            if (float.TryParse(currentRaw, out float parsedValue))
            {
                float maxResolution = 4095f;
                float newRaw = Mathf.Clamp(parsedValue, 0f, maxResolution);

                RawGripValue = newRaw;
                ConvertedGripKg = (RawGripValue / maxResolution) * 60f;
                isNewDataArrived = true;
            }
        }
    }

    private void ExitGameProcess()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void ReceiveData()
    {
        try
        {
            client = new UdpClient(port);
            client.Client.ReceiveTimeout = 1000;

            while (isAppRunning)
            {
                try
                {
                    IPEndPoint anyIP = new IPEndPoint(IPAddress.Any, 0);
                    byte[] dataByte = client.Receive(ref anyIP);
                    string msg = Encoding.UTF8.GetString(dataByte).Trim();

                    string upperMsg = msg.ToUpper().Replace(" ", "");

                    if (upperMsg.Contains("BLE_CONNECTED"))
                    {
                        lock (lockObject) { commandQueue.Enqueue("BLE_CONNECTED"); }
                    }
                    else if (upperMsg.Contains("BLE_DISCONNECTED"))
                    {
                        lock (lockObject) { commandQueue.Enqueue("BLE_DISCONNECTED"); }
                    }
                    else if (upperMsg.Contains("BLE_FAIL"))
                    {
                        lock (lockObject) { commandQueue.Enqueue("BLE_FAIL"); }
                    }
                    else if (upperMsg.Contains("START_7"))
                    {
                        lock (lockObject) { commandQueue.Enqueue("START_7"); latestRawData = "0"; }
                    }
                    else if (upperMsg.Contains("START_4"))
                    {
                        // 💡 오타 수정 완료: 마감 괄호 ')'가 빠져있던 치명적인 컴파일 에러를 바로잡았습니다.
                        lock (lockObject) { commandQueue.Enqueue("START_4"); latestRawData = "0"; }
                    }
                    else if (upperMsg.Contains("STOP"))
                    {
                        lock (lockObject) { commandQueue.Enqueue("STOP"); latestRawData = "0"; }
                    }
                    else if (upperMsg.Contains("PAUSE"))
                    {
                        lock (lockObject) { commandQueue.Enqueue("PAUSE"); }
                    }
                    else if (upperMsg.Contains("RESUME"))
                    {
                        lock (lockObject) { commandQueue.Enqueue("RESUME"); }
                    }
                    else if (msg.StartsWith("Grip: "))
                    {
                        lock (lockObject) { latestRawData = msg.Replace("Grip: ", ""); }
                    }
                }
                catch (SocketException ex)
                {
                    if (ex.SocketErrorCode != SocketError.TimedOut) throw;
                }
            }
        }
        catch (Exception)
        {
        }
    }

    private void LoadSceneSafe(string sceneName)
    {
        if (Application.CanStreamedLevelBeLoaded(sceneName))
        {
            SceneManager.LoadScene(sceneName);
        }
    }

    void OnDisable()
    {
        CloseSockets();
    }

    void OnApplicationQuit()
    {
        CloseSockets();
    }

    private void CloseSockets()
    {
        isAppRunning = false;

        try
        {
            IPEndPoint pythonTargetEP = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 6006);
            byte[] quitData = Encoding.UTF8.GetBytes("QUIT");

            if (client != null)
            {
                for (int i = 0; i < 5; i++)
                {
                    client.Send(quitData, quitData.Length, pythonTargetEP);
                    System.Threading.Thread.Sleep(20);
                }
                UnityEngine.Debug.Log("[GripReceiver] 파이썬 고정 포트(6006)로 QUIT 신호 송신 완료.");
            }
        }
        catch (Exception ex)
        {

            UnityEngine.Debug.LogError($"❌ [GripReceiver] QUIT 신호 송신 오류: {ex.Message}");
        }
        finally
        {
            if (client != null)
            {
                client.Close();
                client = null;
            }

            if (receiveThread != null && receiveThread.IsAlive)
            {
                receiveThread.Abort();
                receiveThread = null;
            }
        }
    }
}

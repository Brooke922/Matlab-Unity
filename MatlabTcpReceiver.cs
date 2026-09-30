using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class MatlabTcpReceiver : MonoBehaviour
{
    [Header("Network Settings")]
    public int port = 55001;

    [Header("Benchmarking Readouts")]
    public bool isConnected = false;
    public float measuredFrequencyHz;
    public float averageLatencyMs;

    private TcpListener listener;
    private Thread listenThread;
    private bool isRunning = false;

    // Thread-safe queue for pose updates from background network thread
    private ConcurrentQueue<PoseData> poseQueue = new ConcurrentQueue<PoseData>();

    // Interpolation targets for smooth rendering
    private Vector3 targetPosition;
    private Quaternion targetRotation;

    // Benchmarking counters
    private int packetCount = 0;
    private float benchmarkTimer = 0f;
    private float totalLatencyMs = 0f;

    private struct PoseData
    {
        public Vector3 position;
        public Vector3 eulerDegrees;
        public double sendTimestamp;
    }

    void Start()
    {
        Application.runInBackground = true;
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 120;

        targetPosition = transform.position;
        targetRotation = transform.rotation;

        isRunning = true;
        listenThread = new Thread(ListenForConnections);
        listenThread.IsBackground = true;
        listenThread.Start();
    }

    void Update()
    {
        // 1. Dequeue incoming network poses on Unity's Main Thread
        while (poseQueue.TryDequeue(out PoseData pose))
        {
            targetPosition = pose.position;
            targetRotation = Quaternion.Euler(pose.eulerDegrees);

            // Calculate network transmission latency
            if (pose.sendTimestamp > 0)
            {
                double currentUnixTime = DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
                float latency = (float)((currentUnixTime - pose.sendTimestamp) * 1000.0);
                totalLatencyMs += Mathf.Max(0, latency);
            }

            packetCount++;
        }

        // 2. Visual Polish: Frame-rate independent Lerp/Slerp interpolation
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * 25f);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 25f);

        // 3. High-Frequency Benchmarking: Print metrics every 1 second
        benchmarkTimer += Time.deltaTime;
        if (benchmarkTimer >= 1.0f)
        {
            measuredFrequencyHz = packetCount / benchmarkTimer;
            averageLatencyMs = packetCount > 0 ? (totalLatencyMs / packetCount) : 0f;

            if (isConnected && packetCount > 0)
            {
                Debug.Log($"[STREAM BENCHMARK] Rate: {measuredFrequencyHz:F1} Hz | Avg Latency: {averageLatencyMs:F2} ms");
            }

            packetCount = 0;
            totalLatencyMs = 0f;
            benchmarkTimer = 0f;
        }
    }

    private void ListenForConnections()
    {
        try
        {
            listener = new TcpListener(IPAddress.Any, port);
            listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            listener.Start();

            while (isRunning)
            {
                if (!listener.Pending())
                {
                    Thread.Sleep(2);
                    continue;
                }

                using (TcpClient client = listener.AcceptTcpClient())
                {
                    client.ReceiveTimeout = 2000;
                    isConnected = true;
                    Debug.Log("<color=green>[TCP Bridge]</color> MATLAB Client Connected!");

                    using (NetworkStream stream = client.GetStream())
                    using (StreamReader reader = new StreamReader(stream, Encoding.ASCII))
                    {
                        while (isRunning && client.Connected)
                        {
                            try
                            {
                                string line = reader.ReadLine();
                                if (line == null) break; // Client closed connection

                                string[] parts = line.Split(',');
                                if (parts.Length >= 6)
                                {
                                    if (float.TryParse(parts[0], out float x) &&
                                        float.TryParse(parts[1], out float y) &&
                                        float.TryParse(parts[2], out float z) &&
                                        float.TryParse(parts[3], out float rx) &&
                                        float.TryParse(parts[4], out float ry) &&
                                        float.TryParse(parts[5], out float rz))
                                    {
                                        double timestamp = 0;
                                        if (parts.Length == 7) double.TryParse(parts[6], out timestamp);

                                        poseQueue.Enqueue(new PoseData
                                        {
                                            position = new Vector3(x, y, z),
                                            eulerDegrees = new Vector3(rx, ry, rz),
                                            sendTimestamp = timestamp
                                        });
                                    }
                                }
                            }
                            catch (IOException)
                            {
                                break; // Handle client restart/disconnect safely
                            }
                            catch (Exception ex)
                            {
                                Debug.LogWarning($"[TCP Read Error] {ex.Message}");
                                break;
                            }
                        }
                    }

                    isConnected = false;
                    Debug.Log("<color=yellow>[TCP Bridge]</color> MATLAB Disconnected. Waiting for reconnection...");
                }
            }
        }
        catch (Exception e)
        {
            if (isRunning) Debug.LogError($"[TCP Exception] {e.Message}");
        }
        finally
        {
            isConnected = false;
        }
    }

    private void Cleanup()
    {
        isRunning = false;
        if (listener != null) { try { listener.Stop(); } catch { } }
        if (listenThread != null && listenThread.IsAlive) { try { listenThread.Abort(); } catch { } }
    }

    void OnDisable() => Cleanup();
    void OnDestroy() => Cleanup();
    void OnApplicationQuit() => Cleanup();
}
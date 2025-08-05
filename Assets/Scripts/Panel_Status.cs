using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// This class manages the status panel in the UI, displaying connection status
/// by listening to events from the ConnectionManager.
/// </summary>
public class Panel_Status : MonoBehaviour
{
    public static Panel_Status Instance { get; private set; }

    [SerializeField] private TMP_Text _statusText;
    [SerializeField] private Color _colorUnconnected = Color.white;
    [SerializeField] private Color _colorConnecting = Color.yellow;
    [SerializeField] private Color _colorConnected = Color.green;

    private Coroutine _connectingRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (_statusText == null)
        {
            _statusText = GetComponentInChildren<TMP_Text>();
        }

        // Subscribe to connection events
        ConnectionManager.OnConnectingStarted += StartConnecting;
        ConnectionManager.OnConnected += SetConnected;
        ConnectionManager.OnDisconnected += SetUnconnected;
    }

    private void OnDestroy()
    {
        // Unsubscribe from connection events
        ConnectionManager.OnConnectingStarted -= StartConnecting;
        ConnectionManager.OnConnected -= SetConnected;
        ConnectionManager.OnDisconnected -= SetUnconnected;

        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Start()
    {
        SetUnconnected();
    }

    public void SetUnconnected()
    {
        StopConnectingAnimation();
        _statusText.text = "Unconnected";
        _statusText.color = _colorUnconnected;
    }

    public void StartConnecting()
    {
        StopConnectingAnimation();
        _connectingRoutine = StartCoroutine(ConnectingAnimation());
    }

    public void SetConnected()
    {
        StopConnectingAnimation();
        _statusText.text = "Connected";
        _statusText.color = _colorConnected;
    }

    private IEnumerator ConnectingAnimation()
    {
        _statusText.color = _colorConnecting;
        const string baseText = "Connecting";
        int dots = 0;
        while (true)
        {
            _statusText.text = baseText + new string('.', dots);
            dots = (dots + 1) % 4;
            yield return new WaitForSeconds(0.5f);
        }
    }

    private void StopConnectingAnimation()
    {
        if (_connectingRoutine != null)
        {
            StopCoroutine(_connectingRoutine);
            _connectingRoutine = null;
        }
    }
}


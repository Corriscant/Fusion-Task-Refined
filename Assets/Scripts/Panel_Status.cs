using UnityEngine;
using TMPro;
using System.Collections;
using static Corris.Loggers.Logger;
using static Corris.Loggers.LogUtils;
using VContainer;

/// <summary>
/// This class manages the status panel in the UI, displaying connection status
/// by listening to events from the connection service.
/// </summary>
public class Panel_Status : MonoBehaviour
{
    [SerializeField] private TMP_Text _statusText;
    [SerializeField] private Color _colorUnconnected = Color.white;
    [SerializeField] private Color _colorConnecting = Color.yellow;
    [SerializeField] private Color _colorConnected = Color.green;

    private Coroutine _connectingRoutine;

    private IConnectionService _connectionService;
    // Called by VContainer to inject the dependency immediately upon its creation.
    [Inject]
    public void Construct(IConnectionService connectionService)
    {
        Log($"{GetLogCallPrefix(GetType())} VContainer called!");

        this._connectionService = connectionService;

        connectionService.ConnectingStarted += StartConnecting;
        connectionService.Connected += SetConnected;
        connectionService.Disconnected += SetUnconnected;
    }

    private void Awake()
    {
        if (_statusText == null)
        {
            _statusText = GetComponentInChildren<TMP_Text>();
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from connection events
        if (_connectionService != null)
        {
            _connectionService.ConnectingStarted -= StartConnecting;
            _connectionService.Connected -= SetConnected;
            _connectionService.Disconnected -= SetUnconnected;
        }
    }

    private void Start()
    {
        if (_connectionService == null)
        {
            LogError($"{GetLogCallPrefix(GetType())} Connection service NIL!");
        }
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


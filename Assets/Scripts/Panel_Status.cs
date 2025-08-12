using UnityEngine;
using TMPro;
using System.Collections;
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

    [Inject] private IConnectionService _connectionService;

    private Coroutine _connectingRoutine;

    private void Awake()
    {
        if (_statusText == null)
        {
            _statusText = GetComponentInChildren<TMP_Text>();
        }

        // Subscribe to connection events
        _connectionService.ConnectingStarted += StartConnecting;
        _connectionService.Connected += SetConnected;
        _connectionService.Disconnected += SetUnconnected;
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


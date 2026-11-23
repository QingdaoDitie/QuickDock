using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Media;
using System.Windows.Threading;
using QuickDock.Services;
using QuickDock.WidgetContracts;
using Windows.Foundation;
using Windows.Media.Control;
using WpfButton = System.Windows.Controls.Button;

namespace QuickDock.Widgets;

internal sealed class MusicWidget : IDockWidget
{
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(2) };
    private readonly MusicWidgetView _view = new();
    private GlobalSystemMediaTransportControlsSessionManager? _manager;
    private GlobalSystemMediaTransportControlsSession? _session;
    private bool _refreshing;
    private bool _refreshRequested;
    private bool _sendingCommand;
    private bool _playing;
    private bool _canToggle;
    private bool _canSkipPrevious;
    private bool _canSkipNext;
    private int _generation;
    private string? _trackTitle;
    private string? _trackArtist;
    public FrameworkElement View => _view;

    public MusicWidget()
    {
        _view.PreviousButton.Click += async (_, _) =>
            await SendCommandAsync(_view.PreviousButton, session => session.TrySkipPreviousAsync());
        _view.ToggleButton.Click += async (_, _) => await SendCommandAsync(_view.ToggleButton, TogglePlayback);
        _view.NextButton.Click += async (_, _) =>
            await SendCommandAsync(_view.NextButton, session => session.TrySkipNextAsync());
        _timer.Tick += async (_, _) => await RefreshAsync();
        UpdateLanguage();
    }

    public void SetActive(bool active)
    {
        if (_timer.IsEnabled == active) return;
        _generation++;
        if (active)
        {
            _timer.Start();
            _ = RefreshAsync();
        }
        else
        {
            _timer.Stop();
            _manager = null;
            ClearPlayback();
            UpdateLanguage();
        }
    }

    private async Task RefreshAsync()
    {
        if (!_timer.IsEnabled) return;
        if (_refreshing)
        {
            _refreshRequested = true;
            return;
        }
        _refreshing = true;
        var generation = _generation;
        try
        {
            var manager = _manager ?? await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
            if (generation != _generation) return;
            _manager = manager;
            var sessions = manager.GetSessions().Where(session => IsQqMusic(session.SourceAppUserModelId)).ToArray();
            var session = sessions.FirstOrDefault(session => IsPlaying(session.GetPlaybackInfo())) ?? sessions.FirstOrDefault();
            var playback = session?.GetPlaybackInfo();
            var media = session == null ? null : await session.TryGetMediaPropertiesAsync();
            if (generation != _generation) return;

            SetSession(session);
            _playing = IsPlaying(playback);
            var controls = playback?.Controls;
            _canToggle = controls != null &&
                (controls.IsPlayPauseToggleEnabled || (_playing ? controls.IsPauseEnabled : controls.IsPlayEnabled));
            _canSkipPrevious = controls?.IsPreviousEnabled == true;
            _canSkipNext = controls?.IsNextEnabled == true;
            _trackTitle = media?.Title;
            _trackArtist = media?.Artist;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"QQ Music session error: {ex.Message}");
            if (generation != _generation) return;
            _manager = null;
            ClearPlayback();
        }
        finally
        {
            _refreshing = false;
            if (generation == _generation) UpdateLanguage();
            if (_refreshRequested)
            {
                _refreshRequested = false;
                _ = RefreshAsync();
            }
        }
    }

    private void SetSession(GlobalSystemMediaTransportControlsSession? session)
    {
        if (Equals(_session, session)) return;
        if (_session != null) _session.MediaPropertiesChanged -= OnMediaPropertiesChanged;
        _session = session;
        if (_session != null) _session.MediaPropertiesChanged += OnMediaPropertiesChanged;
    }

    private void OnMediaPropertiesChanged(GlobalSystemMediaTransportControlsSession sender, MediaPropertiesChangedEventArgs args)
    {
        _view.Dispatcher.InvokeAsync(() =>
        {
            if (Equals(_session, sender)) _ = RefreshAsync();
        });
    }

    private void ClearPlayback()
    {
        SetSession(null);
        _refreshRequested = false;
        _trackTitle = null;
        _trackArtist = null;
        _playing = _canToggle = _canSkipPrevious = _canSkipNext = false;
    }

    private async Task SendCommandAsync(WpfButton button,
        Func<GlobalSystemMediaTransportControlsSession, IAsyncOperation<bool>> command)
    {
        var session = _session;
        if (session == null || !button.IsEnabled || _sendingCommand) return;
        _sendingCommand = true;
        UpdateLanguage();
        try
        {
            await command(session);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"QQ Music command error: {ex.Message}");
        }
        finally
        {
            _sendingCommand = false;
            UpdateLanguage();
            await RefreshAsync();
        }
    }

    private static IAsyncOperation<bool> TogglePlayback(GlobalSystemMediaTransportControlsSession session)
    {
        var playback = session.GetPlaybackInfo();
        if (playback.Controls.IsPlayPauseToggleEnabled) return session.TryTogglePlayPauseAsync();
        return IsPlaying(playback) ? session.TryPauseAsync() : session.TryPlayAsync();
    }

    private static bool IsQqMusic(string id)
    {
        var name = Path.GetFileName(id);
        return name.Equals("QQMusic", StringComparison.OrdinalIgnoreCase) ||
            name.Equals("QQMusic.exe", StringComparison.OrdinalIgnoreCase) ||
            name.StartsWith("QQMusic_", StringComparison.OrdinalIgnoreCase) ||
            name.StartsWith("TencentQQ.QQMusic_", StringComparison.OrdinalIgnoreCase) ||
            name.StartsWith("Tencent.QQMusic_", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsPlaying(GlobalSystemMediaTransportControlsSessionPlaybackInfo? playback) =>
        playback?.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;

    public void UpdateLanguage()
    {
        var hasTitle = !string.IsNullOrWhiteSpace(_trackTitle);
        var details = hasTitle
            ? (string.IsNullOrWhiteSpace(_trackArtist) ? _trackTitle : $"{_trackTitle} · {_trackArtist}")
            : Lang.T(_session == null ? "Widget.MusicUnavailable" : "Widget.MusicIdle");
        _view.TrackTitle.Text = _trackTitle;
        _view.TrackTitle.Visibility = hasTitle ? Visibility.Visible : Visibility.Collapsed;
        _view.TrackTitle.ToolTip = hasTitle ? details : null;
        _view.ToggleIcon.Data = (Geometry)_view.Resources[_playing ? "PauseIcon" : "PlayIcon"];
        var setupHint = _session == null && !hasTitle ? details : null;
        UpdateButton(_view.PreviousButton, "Widget.PreviousTrack", _canSkipPrevious, setupHint);
        UpdateButton(_view.ToggleButton, _playing ? "Widget.Pause" : "Widget.Play", _canToggle, details);
        UpdateButton(_view.NextButton, "Widget.NextTrack", _canSkipNext, setupHint);
    }

    private void UpdateButton(WpfButton button, string label, bool canExecute, string? details)
    {
        var action = Lang.T(label);
        button.ToolTip = details == null ? action : action + Environment.NewLine + details;
        AutomationProperties.SetName(button, action);
        button.IsEnabled = canExecute && !_sendingCommand;
    }

    public void Dispose() => SetActive(false);
}

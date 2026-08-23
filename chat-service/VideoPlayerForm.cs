using chat_service.protocol;
using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using WpfMediaElement = System.Windows.Controls.MediaElement;
using WpfMediaState = System.Windows.Controls.MediaState;
using WpfStretch = System.Windows.Media.Stretch;
using WinFormsTimer = System.Windows.Forms.Timer;

namespace chat_service
{
    /// <summary>
    /// 使用 Windows 系统媒体栈播放 net-server 签名视频流。
    /// MediaElement 在调整 Position 时会对支持 Range 的服务端重新发起分段请求。
    /// </summary>
    public sealed class VideoPlayerForm : Form
    {
        private const int SeekBarMaximum = 1000;

        private readonly long fileId;
        private readonly string fileName;
        private readonly string transferToken;
        private readonly string sessionId;
        private readonly MediaPlaybackService playbackService;
        private readonly WpfMediaElement mediaElement;
        private readonly ElementHost mediaHost;
        private readonly TrackBar seekBar;
        private readonly Button playPauseButton;
        private readonly Button stopButton;
        private readonly Button fullScreenButton;
        private readonly Label timeLabel;
        private readonly Label statusLabel;
        private readonly WinFormsTimer progressTimer;
        private readonly ToolTip toolTip;

        private TimeSpan mediaDuration = TimeSpan.Zero;
        private TimeSpan? pendingPosition;
        private bool pendingAutoPlay;
        private bool mediaReady;
        private bool isPlaying;
        private bool isLoading;
        private bool isDraggingSeekBar;
        private bool isFullScreen;
        private bool isClosing;
        private int loadVersion;
        private DateTime refreshAtUtc = DateTime.MaxValue;
        private Rectangle normalBounds;
        private FormWindowState normalWindowState;
        private FormBorderStyle normalBorderStyle;

        public VideoPlayerForm(long fileId, string fileName, string transferToken, string mediaBaseAddress)
        {
            this.fileId = fileId;
            this.fileName = string.IsNullOrWhiteSpace(fileName) ? "视频在线播放" : fileName;
            this.transferToken = transferToken;
            this.sessionId = Guid.NewGuid().ToString("N");
            this.playbackService = new MediaPlaybackService(mediaBaseAddress);

            Text = this.fileName + " - 在线播放";
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(760, 480);
            ClientSize = new Size(980, 620);
            BackColor = Color.FromArgb(15, 20, 28);
            ForeColor = Color.White;
            KeyPreview = true;
            ShowIcon = false;

            Panel header = BuildHeader(out statusLabel);
            Panel controls = BuildControls(out seekBar, out playPauseButton, out stopButton,
                out fullScreenButton, out timeLabel);

            mediaElement = new WpfMediaElement
            {
                LoadedBehavior = WpfMediaState.Manual,
                UnloadedBehavior = WpfMediaState.Manual,
                Stretch = WpfStretch.Uniform,
                ScrubbingEnabled = true,
                Volume = 0.8D
            };
            mediaElement.MediaOpened += MediaElementMediaOpened;
            mediaElement.MediaEnded += MediaElementMediaEnded;
            mediaElement.MediaFailed += MediaElementMediaFailed;
            mediaElement.MouseLeftButtonDown += delegate(object sender, System.Windows.Input.MouseButtonEventArgs args)
            {
                if (args.ClickCount == 2) ToggleFullScreen();
            };

            mediaHost = new ElementHost
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Black,
                Child = mediaElement
            };

            Controls.Add(mediaHost);
            Controls.Add(controls);
            Controls.Add(header);

            toolTip = new ToolTip();
            toolTip.SetToolTip(playPauseButton, "播放 / 暂停（空格）");
            toolTip.SetToolTip(stopButton, "停止并释放视频流");
            toolTip.SetToolTip(fullScreenButton, "全屏 / 退出全屏（F 或 Esc）");

            playPauseButton.Click += delegate { TogglePlayPause(); };
            stopButton.Click += delegate { StopPlayback(); };
            fullScreenButton.Click += delegate { ToggleFullScreen(); };
            seekBar.MouseDown += delegate { isDraggingSeekBar = true; };
            seekBar.Scroll += delegate { UpdateTimePreview(); };
            seekBar.MouseUp += delegate
            {
                isDraggingSeekBar = false;
                ApplySeekPosition();
            };
            seekBar.KeyUp += SeekBarKeyUp;

            progressTimer = new WinFormsTimer { Interval = 250 };
            progressTimer.Tick += ProgressTimerTick;

            Shown += delegate
            {
                progressTimer.Start();
                BeginLoadMedia(null, true, "正在获取播放地址…");
            };
            KeyDown += VideoPlayerFormKeyDown;
            FormClosing += VideoPlayerFormClosing;
        }

        private Panel BuildHeader(out Label stateLabel)
        {
            Panel header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 48,
                BackColor = Color.FromArgb(23, 30, 42),
                Padding = new Padding(16, 0, 12, 0)
            };
            Label titleLabel = new Label
            {
                Text = fileName,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("微软雅黑", 10.5F, FontStyle.Bold),
                ForeColor = Color.White,
                AutoEllipsis = true
            };
            stateLabel = new Label
            {
                Text = "准备播放",
                Dock = DockStyle.Right,
                Width = 220,
                TextAlign = ContentAlignment.MiddleRight,
                Font = new Font("微软雅黑", 9F),
                ForeColor = Color.FromArgb(171, 185, 202),
                AutoEllipsis = true
            };
            header.Controls.Add(titleLabel);
            header.Controls.Add(stateLabel);
            return header;
        }

        private Panel BuildControls(out TrackBar trackBar, out Button playButton, out Button stopPlaybackButton,
            out Button fullScreenPlaybackButton, out Label playbackTimeLabel)
        {
            Panel controls = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 88,
                BackColor = Color.FromArgb(23, 30, 42),
                Padding = new Padding(12, 4, 12, 8)
            };
            trackBar = new TrackBar
            {
                Dock = DockStyle.Top,
                Height = 36,
                Minimum = 0,
                Maximum = SeekBarMaximum,
                TickStyle = TickStyle.None,
                SmallChange = 5,
                LargeChange = 25,
                Enabled = false,
                TabStop = true
            };
            Panel buttonRow = new Panel { Dock = DockStyle.Fill, BackColor = controls.BackColor };
            FlowLayoutPanel leftActions = new FlowLayoutPanel
            {
                Dock = DockStyle.Left,
                Width = 360,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Padding = new Padding(0, 1, 0, 0),
                BackColor = controls.BackColor
            };
            playButton = CreatePlayerButton("▶", "播放");
            stopPlaybackButton = CreatePlayerButton("■", "停止");
            playbackTimeLabel = new Label
            {
                Text = "00:00 / 00:00",
                AutoSize = false,
                Width = 170,
                Height = 38,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Consolas", 9.5F),
                ForeColor = Color.FromArgb(214, 222, 232),
                Margin = new Padding(8, 0, 0, 0)
            };
            fullScreenPlaybackButton = CreatePlayerButton("⛶", "全屏");
            fullScreenPlaybackButton.Dock = DockStyle.Right;

            leftActions.Controls.Add(playButton);
            leftActions.Controls.Add(stopPlaybackButton);
            leftActions.Controls.Add(playbackTimeLabel);
            buttonRow.Controls.Add(leftActions);
            buttonRow.Controls.Add(fullScreenPlaybackButton);
            controls.Controls.Add(buttonRow);
            controls.Controls.Add(trackBar);
            return controls;
        }

        private Button CreatePlayerButton(string icon, string accessibleName)
        {
            Button button = new Button
            {
                Text = icon,
                Size = new Size(42, 38),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(36, 46, 61),
                ForeColor = Color.White,
                Font = new Font("Segoe UI Symbol", 14F, FontStyle.Regular),
                Cursor = Cursors.Hand,
                TabStop = true,
                AccessibleName = accessibleName,
                Margin = new Padding(0, 0, 8, 0)
            };
            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(51, 65, 84);
            button.FlatAppearance.MouseDownBackColor = Color.FromArgb(66, 82, 104);
            return button;
        }

        private void BeginLoadMedia(TimeSpan? resumePosition, bool autoPlay, string loadingMessage)
        {
            if (isClosing || isLoading) return;
            if (resumePosition.HasValue && mediaElement.Source != null)
            {
                // 刷新短期播放凭据时先冻结当前位置，避免请求新 URL 的时间造成回退。
                mediaElement.Pause();
            }
            isLoading = true;
            mediaReady = false;
            seekBar.Enabled = false;
            SetStatus(loadingMessage, Color.FromArgb(171, 185, 202));
            int requestVersion = Interlocked.Increment(ref loadVersion);

            Task.Run(delegate
            {
                try
                {
                    MediaPlayUrlInfo playInfo = playbackService.RequestPlayUrl(fileId, transferToken, sessionId);
                    RunOnUi(delegate
                    {
                        if (requestVersion != loadVersion || isClosing) return;
                        ApplyPlayUrl(playInfo, resumePosition, autoPlay);
                    });
                }
                catch (Exception ex)
                {
                    RunOnUi(delegate
                    {
                        if (requestVersion != loadVersion || isClosing) return;
                        isLoading = false;
                        mediaReady = false;
                        isPlaying = false;
                        UpdatePlayPauseButton();
                        SetStatus(ex.Message, Color.FromArgb(244, 122, 122));
                    });
                }
            });
        }

        private void ApplyPlayUrl(MediaPlayUrlInfo playInfo, TimeSpan? resumePosition, bool autoPlay)
        {
            if (playInfo == null || string.IsNullOrWhiteSpace(playInfo.PlayUrl))
            {
                isLoading = false;
                SetStatus("播放地址无效", Color.FromArgb(244, 122, 122));
                return;
            }

            Uri playUri;
            if (!Uri.TryCreate(playInfo.PlayUrl, UriKind.Absolute, out playUri))
            {
                isLoading = false;
                SetStatus("播放地址格式错误", Color.FromArgb(244, 122, 122));
                return;
            }

            pendingPosition = resumePosition;
            pendingAutoPlay = autoPlay;
            long expiresIn = Math.Max(1L, playInfo.ExpiresIn);
            long refreshLead = Math.Min(30L, Math.Max(1L, expiresIn / 3L));
            refreshAtUtc = DateTime.UtcNow.AddSeconds(Math.Max(1L, expiresIn - refreshLead));
            mediaElement.Stop();
            mediaElement.Source = playUri;
            // Manual 模式下调用 Play 才会启动媒体探测；MediaOpened 中再恢复目标状态。
            mediaElement.Play();
            SetStatus("正在连接视频流…", Color.FromArgb(171, 185, 202));
        }

        private void MediaElementMediaOpened(object sender, System.Windows.RoutedEventArgs e)
        {
            if (isClosing) return;
            isLoading = false;
            mediaReady = true;
            if (mediaElement.NaturalDuration.HasTimeSpan)
            {
                mediaDuration = mediaElement.NaturalDuration.TimeSpan;
            }
            else
            {
                mediaDuration = TimeSpan.Zero;
            }

            if (pendingPosition.HasValue)
            {
                TimeSpan target = ClampPosition(pendingPosition.Value);
                mediaElement.Position = target;
            }
            pendingPosition = null;
            seekBar.Enabled = mediaDuration.TotalMilliseconds > 0D;

            if (pendingAutoPlay)
            {
                mediaElement.Play();
                isPlaying = true;
                SetStatus("正在播放", Color.FromArgb(109, 219, 151));
            }
            else
            {
                mediaElement.Pause();
                isPlaying = false;
                SetStatus("已暂停", Color.FromArgb(238, 193, 99));
            }
            UpdatePlayPauseButton();
            UpdateProgressDisplay();
        }

        private void MediaElementMediaEnded(object sender, System.Windows.RoutedEventArgs e)
        {
            isPlaying = false;
            mediaElement.Pause();
            if (mediaDuration > TimeSpan.Zero) mediaElement.Position = mediaDuration;
            SetStatus("播放完毕", Color.FromArgb(171, 185, 202));
            UpdatePlayPauseButton();
            UpdateProgressDisplay();
        }

        private void MediaElementMediaFailed(object sender, System.Windows.ExceptionRoutedEventArgs e)
        {
            if (isClosing) return;
            isLoading = false;
            mediaReady = false;
            isPlaying = false;
            seekBar.Enabled = false;
            string detail = e.ErrorException == null ? string.Empty : e.ErrorException.Message;
            SetStatus(string.IsNullOrWhiteSpace(detail)
                ? "视频加载失败，请确认编码格式受 Windows 支持"
                : "视频加载失败: " + detail, Color.FromArgb(244, 122, 122));
            UpdatePlayPauseButton();
        }

        private void TogglePlayPause()
        {
            if (isClosing || isLoading) return;
            if (mediaElement.Source == null || !mediaReady)
            {
                BeginLoadMedia(null, true, "正在重新获取播放地址…");
                return;
            }

            if (isPlaying)
            {
                mediaElement.Pause();
                isPlaying = false;
                SetStatus("已暂停", Color.FromArgb(238, 193, 99));
            }
            else
            {
                if (mediaDuration > TimeSpan.Zero && mediaElement.Position >= mediaDuration)
                {
                    mediaElement.Position = TimeSpan.Zero;
                }
                mediaElement.Play();
                isPlaying = true;
                SetStatus("正在播放", Color.FromArgb(109, 219, 151));
            }
            UpdatePlayPauseButton();
        }

        private void StopPlayback()
        {
            if (isClosing) return;
            Interlocked.Increment(ref loadVersion);
            isLoading = false;
            mediaReady = false;
            isPlaying = false;
            mediaDuration = TimeSpan.Zero;
            pendingPosition = null;
            refreshAtUtc = DateTime.MaxValue;
            try
            {
                mediaElement.Stop();
                mediaElement.Source = null;
            }
            catch (InvalidOperationException)
            {
                // 窗口销毁期间媒体栈可能已卸载，状态已在上方复位。
            }
            seekBar.Enabled = false;
            seekBar.Value = 0;
            timeLabel.Text = "00:00 / 00:00";
            SetStatus("已停止并释放视频流", Color.FromArgb(171, 185, 202));
            UpdatePlayPauseButton();
        }

        private void ApplySeekPosition()
        {
            if (!mediaReady || mediaDuration.TotalMilliseconds <= 0D) return;
            TimeSpan target = TimeSpan.FromMilliseconds(mediaDuration.TotalMilliseconds
                * seekBar.Value / SeekBarMaximum);
            target = ClampPosition(target);
            mediaElement.Position = target;
            UpdateProgressDisplay();

            double targetSeconds = target.TotalSeconds;
            Task.Run(delegate
            {
                try
                {
                    playbackService.NotifySeek(fileId, transferToken, sessionId, targetSeconds);
                }
                catch (MediaPlaybackException)
                {
                    // Seek 本身由 HTTP Range 完成；通知失败不打断当前播放。
                }
            });
        }

        private void ProgressTimerTick(object sender, EventArgs e)
        {
            if (isClosing) return;
            if (!isDraggingSeekBar) UpdateProgressDisplay();
            if (mediaReady && !isLoading && mediaElement.Source != null && DateTime.UtcNow >= refreshAtUtc)
            {
                TimeSpan resumePosition = mediaElement.Position;
                bool resumePlaying = isPlaying;
                BeginLoadMedia(resumePosition, resumePlaying, "正在刷新播放凭据…");
            }
        }

        private void UpdateProgressDisplay()
        {
            TimeSpan current = mediaReady ? mediaElement.Position : TimeSpan.Zero;
            if (!isDraggingSeekBar && mediaDuration.TotalMilliseconds > 0D)
            {
                double ratio = current.TotalMilliseconds / mediaDuration.TotalMilliseconds;
                int value = (int)Math.Round(Math.Max(0D, Math.Min(1D, ratio)) * SeekBarMaximum);
                seekBar.Value = Math.Max(seekBar.Minimum, Math.Min(seekBar.Maximum, value));
            }
            timeLabel.Text = FormatTime(current) + " / " + FormatTime(mediaDuration);
        }

        private void UpdateTimePreview()
        {
            if (mediaDuration.TotalMilliseconds <= 0D) return;
            TimeSpan target = TimeSpan.FromMilliseconds(mediaDuration.TotalMilliseconds
                * seekBar.Value / SeekBarMaximum);
            timeLabel.Text = FormatTime(target) + " / " + FormatTime(mediaDuration);
        }

        private void UpdatePlayPauseButton()
        {
            playPauseButton.Text = isPlaying ? "Ⅱ" : "▶";
            playPauseButton.AccessibleName = isPlaying ? "暂停" : "播放";
        }

        private void ToggleFullScreen()
        {
            if (!isFullScreen)
            {
                normalBounds = WindowState == FormWindowState.Normal ? Bounds : RestoreBounds;
                normalWindowState = WindowState;
                normalBorderStyle = FormBorderStyle;
                Screen targetScreen = Screen.FromControl(this);
                WindowState = FormWindowState.Normal;
                FormBorderStyle = FormBorderStyle.None;
                Bounds = targetScreen.Bounds;
                TopMost = true;
                isFullScreen = true;
                fullScreenButton.AccessibleName = "退出全屏";
                toolTip.SetToolTip(fullScreenButton, "退出全屏（Esc）");
            }
            else
            {
                TopMost = false;
                FormBorderStyle = normalBorderStyle;
                Bounds = normalBounds;
                WindowState = normalWindowState;
                isFullScreen = false;
                fullScreenButton.AccessibleName = "全屏";
                toolTip.SetToolTip(fullScreenButton, "全屏（F）");
            }
        }

        private void VideoPlayerFormKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Space)
            {
                TogglePlayPause();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.F)
            {
                ToggleFullScreen();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.Escape && isFullScreen)
            {
                ToggleFullScreen();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        private void SeekBarKeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Left || e.KeyCode == Keys.Right
                || e.KeyCode == Keys.PageDown || e.KeyCode == Keys.PageUp
                || e.KeyCode == Keys.Home || e.KeyCode == Keys.End)
            {
                ApplySeekPosition();
            }
        }

        private void VideoPlayerFormClosing(object sender, FormClosingEventArgs e)
        {
            isClosing = true;
            Interlocked.Increment(ref loadVersion);
            progressTimer.Stop();
            TopMost = false;
            try
            {
                mediaElement.Stop();
                mediaElement.Source = null;
                mediaHost.Child = null;
            }
            catch (InvalidOperationException)
            {
                // 关闭阶段忽略已卸载媒体栈的状态异常。
            }
            toolTip.Dispose();
            progressTimer.Dispose();
        }

        private TimeSpan ClampPosition(TimeSpan position)
        {
            if (position < TimeSpan.Zero) return TimeSpan.Zero;
            if (mediaDuration > TimeSpan.Zero && position > mediaDuration) return mediaDuration;
            return position;
        }

        private void SetStatus(string message, Color color)
        {
            statusLabel.Text = string.IsNullOrWhiteSpace(message) ? "" : message;
            statusLabel.ForeColor = color;
        }

        private void RunOnUi(Action action)
        {
            if (isClosing || IsDisposed || !IsHandleCreated) return;
            try
            {
                BeginInvoke(new MethodInvoker(delegate { action(); }));
            }
            catch (InvalidOperationException)
            {
                // 窗口已进入销毁阶段，不再投递播放器状态。
            }
        }

        private static string FormatTime(TimeSpan value)
        {
            if (value < TimeSpan.Zero) value = TimeSpan.Zero;
            if (value.TotalHours >= 1D)
            {
                return string.Format("{0:00}:{1:00}:{2:00}", (int)value.TotalHours, value.Minutes, value.Seconds);
            }
            return string.Format("{0:00}:{1:00}", (int)value.TotalMinutes, value.Seconds);
        }
    }
}

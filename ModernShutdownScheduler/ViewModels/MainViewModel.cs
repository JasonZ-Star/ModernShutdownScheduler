using ModernShutdownScheduler.Commands;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;

namespace ModernShutdownScheduler.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private DispatcherTimer? _countdownTimer;
        private int _remainingSeconds;
        private readonly string _stateFilePath;
        private DateTime _targetShutdownUtc;


        private string _shutdownTimeInMinutes = "60";
        public string ShutdownTimeInMinutes
        {
            get => _shutdownTimeInMinutes;
            set
            {
                _shutdownTimeInMinutes = value;
                OnPropertyChanged();
            }
        }

        private string _countdownText = "定时关机已取消";
        public string CountdownText
        {
            get => _countdownText;
            set
            {
                _countdownText = value;
                OnPropertyChanged();
            }
        }

        private bool _isTimerRunning = false;
        public bool IsTimerRunning
        {
            get => _isTimerRunning;
            set
            {
                _isTimerRunning = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsTimerStopped));
            }
        }

        


        public bool IsTimerStopped => !IsTimerRunning;

        public ICommand ScheduleShutdownCommand { get; }
        public ICommand CancelShutdownCommand { get; }
        public ICommand EnterPressedCommand { get; }

        public MainViewModel()
        {
            EnterPressedCommand = new RelayCommand(ExecuteScheduleShutdown, CanExecuteScheduleShutdown);
            ScheduleShutdownCommand = new RelayCommand(ExecuteScheduleShutdown, CanExecuteScheduleShutdown);
            CancelShutdownCommand = new RelayCommand(ExecuteCancelShutdown, CanExecuteCancelShutdown);

            // 设置状态文件路径
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            _stateFilePath = Path.Combine(appDataPath, "ShutdownApp", "state.json");
            Directory.CreateDirectory(Path.GetDirectoryName(_stateFilePath) ?? throw new InvalidOperationException());

            // 尝试恢复状态
            LoadAndRestoreState();
        }

        private bool CanExecuteScheduleShutdown(object? parameter) => !IsTimerRunning;
        private bool CanExecuteCancelShutdown(object? parameter) => IsTimerRunning;


        private void ExecuteScheduleShutdown(object? parameter)
        {
            if (int.TryParse(ShutdownTimeInMinutes, out int minutes) && minutes > 0)
            {
                _remainingSeconds = minutes * 60;
                StartShutdownProcess(_remainingSeconds);
                StartCountdownTimer();
                _targetShutdownUtc = DateTime.UtcNow.AddMinutes(minutes);
                SaveState();
            }
            else
            {
                CountdownText = "请输入有效的分钟数";
            }
        }

        private void ExecuteCancelShutdown(object? parameter)
        {
            StopCountdownTimer();
            CancelShutdownProcess();
            CountdownText = "定时关机已取消";

            if (File.Exists(_stateFilePath))
            {
                File.Delete(_stateFilePath);
            }
        }

        private void StartCountdownTimer()
        {
            IsTimerRunning = true;
            _countdownTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _countdownTimer.Tick += CountdownTimer_Tick;
            _countdownTimer.Start();
            UpdateCountdownText();
        }

        private void StopCountdownTimer()
        {
            _countdownTimer?.Stop();
            IsTimerRunning = false;
        }


        private void CountdownTimer_Tick(object? sender, EventArgs e)
        {
            if (_remainingSeconds > 0)
            {
                _remainingSeconds--;
                UpdateCountdownText();
            }
            else
            {
                StopCountdownTimer();
                CountdownText = "正在关机...";
            }
        }

        private void UpdateCountdownText()
        {
            TimeSpan time = TimeSpan.FromSeconds(_remainingSeconds);
            CountdownText = 
                time.Days <= 0 ? $@"系统将在 {time:hh\:mm\:ss} 后关机" 
                : $@"系统将在 {time:dd\:hh\:mm\:ss} 后关机";
        }

        private void StartShutdownProcess(int seconds)
        {
            ExecuteShutdownCommand($"/s /f /t {seconds}");
        }

        private void CancelShutdownProcess()
        {
            ExecuteShutdownCommand("/a");
        }

        private void ExecuteShutdownCommand(string arguments)
        {
            try
            {
                string winDir = Environment.GetEnvironmentVariable("windir") ?? "C:\\Windows";
                string shutdownPath = System.IO.Path.Combine(winDir, "System32", "shutdown.exe");

                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = shutdownPath,
                    Arguments = arguments,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                CountdownText = $"错误: {ex.Message}";
            }
        }

        private void SaveState()
        {
            // 使用 ISO 8601 "o" 格式保存
            string content = _targetShutdownUtc.ToString("o"); // [34, 36]
            File.WriteAllText(_stateFilePath, content);
        }

        private void LoadAndRestoreState()
        {
            if (File.Exists(_stateFilePath))
            {
                string content = File.ReadAllText(_stateFilePath);
                if (DateTime.TryParse(content, null, System.Globalization.DateTimeStyles.RoundtripKind, out _targetShutdownUtc))
                {
                    if (_targetShutdownUtc > DateTime.UtcNow)
                    {
                        // 状态有效，恢复倒计时
                        IsTimerRunning = true;
                        _remainingSeconds = (int)(_targetShutdownUtc - DateTime.UtcNow).TotalSeconds;
                        StartShutdownProcess(_remainingSeconds);
                        StartCountdownTimer();
                    }
                    else
                    {
                        // 过期的状态文件，清理掉
                        File.Delete(_stateFilePath);
                    }
                }
            }
        }
    }
}

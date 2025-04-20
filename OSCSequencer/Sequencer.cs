using Rug.Osc;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Media;
using System.Text;
using System.Xml.Serialization;

namespace OSCSequencer
{
    public class Sequencer : IDisposable
    {
        public Sequencer(int initialBpm, int initialPatternLength, OscSender oscSender, OscSettings oscSettings)
        {
            _sender = oscSender;
            _settings = oscSettings;
            _project = new()
            {
                Patterns = new List<Pattern>()
                {
                    new Pattern(initialPatternLength)
                },
                CurrentPatternIndex = 0,
                Bpm = initialBpm
            };
            SwitchPlaybackMode(PlaybackMode.Single).Wait();
        }

        #region Members

        private Project _project;

        private readonly OscSender _sender;
        private readonly OscSettings _settings;
        private readonly SemaphoreSlim _lock = new(1, 1);

        private PlaybackMode _playbackMode = PlaybackMode.Single;
        private List<int> _activePatterns = new();
        private readonly ConcurrentDictionary<int, int> _patternSteps = new();

        private int _currentPatternIndex;
        private bool _isPlaying;
        private bool _isPaused;
        private CancellationTokenSource? _playbackCts;

        public Pattern CurrentPattern
        {
            get
            {
                if (_project.Patterns.Count == 0 || _project.Patterns.Count - 1 < _project.CurrentPatternIndex)
                {
                    while (_project.Patterns.Count <= _project.CurrentPatternIndex)
                    {
                        _project.Patterns.Add(new Pattern(16));
                    }
                }
                return _project.Patterns[_project.CurrentPatternIndex];
            }
        }

        #endregion

        #region Methods

        public async Task SwitchPlaybackMode(PlaybackMode? mode = null)
        {
            await _lock.WaitAsync();
            try
            {
                if (mode is not null)
                    _playbackMode = mode.Value;
                else
                {
                    _playbackMode = _playbackMode == PlaybackMode.All
                        ? PlaybackMode.Single
                        : PlaybackMode.All;
                }

                if (_playbackMode == PlaybackMode.All)
                    _activePatterns = Enumerable.Range(0, _project.Patterns.Count).ToList();
                else
                    _activePatterns = new List<int> { _project.CurrentPatternIndex };
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task StartAsync()
        {
            await _lock.WaitAsync();
            try
            {
                if (_isPlaying) return;

                _isPlaying = true;
                _playbackCts = new CancellationTokenSource();
                _ = PlaybackLoop(_playbackCts.Token);
            }
            finally
            {
                _lock.Release();
            }
        }

        private async Task PlaybackLoop(CancellationToken ct)
        {
            var stopwatch = new Stopwatch();

            while (_isPlaying && !ct.IsCancellationRequested)
            {
                if (_isPaused)
                {
                    UpdateVisualization(true);
                    await Task.Delay(100, ct);
                    continue;
                }

                stopwatch.Restart();

                try
                {
                    var patternsToPlay = _activePatterns.ToList();

                    // Воспроизведение всех выбранных паттернов
                    await Parallel.ForEachAsync(patternsToPlay, async (patternIndex, ct) =>
                    {
                        var pattern = _project.Patterns[patternIndex];
                        var step = _patternSteps.GetValueOrDefault(patternIndex, 0);

                        var note = pattern.Notes[step];
                        if (note > 0)
                        {
#if DEBUG
                            DebugSound(note);
#endif

                            SendOsc($"/pattern{patternIndex}/noteon", note);
                            await Task.Delay(50, ct);
                            SendOsc($"/pattern{patternIndex}/noteoff", note);
                        }

                        // Обновляем шаг для паттерна
                        _patternSteps[patternIndex] = (step + 1) % pattern.Length;
                    });

                    // Рассчитываем целевую длительность
                    var interval = 60000 / _project.Bpm;
                    var elapsed = stopwatch.ElapsedMilliseconds;
                    var remainingDelay = interval - (int)elapsed;

                    // Корректируем задержку с учетом выполненной работы
                    if (remainingDelay > 0)
                        await Task.Delay(remainingDelay, ct);

                    UpdateVisualization();
                }
                finally
                {
                    stopwatch.Stop();
                }
            }
        }

        public async Task StopAsync()
        {
            await _lock.WaitAsync();
            try
            {
                _isPlaying = false;
                // Сбрасываем все шаги
                foreach (var key in _patternSteps.Keys.ToList())
                    _patternSteps[key] = 0;
                _playbackCts?.Cancel();
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task PauseAsync()
        {
            await _lock.WaitAsync();
            try
            {
                _isPaused = !_isPaused;
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task RecordNoteAsync(int position, int note)
        {
            await _lock.WaitAsync();
            try
            {
                if (position >= 0 && position < CurrentPattern.Length)
                    CurrentPattern.Notes[position] = note;
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task SetTempoAsync(int bpm)
        {
            await _lock.WaitAsync();
            try
            {
                _project.Bpm = bpm;
                SendOsc(_settings.Addresses["tempo"], bpm);
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task SetPatternLengthAsync(int length)
        {
            await _lock.WaitAsync();
            try
            {
                var newPattern = new Pattern(length);
                Array.Copy(CurrentPattern.Notes, newPattern.Notes, Math.Min(length, CurrentPattern.Length));
                _project.Patterns[_currentPatternIndex] = newPattern;
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task ClearPatternAsync()
        {
            await _lock.WaitAsync();
            try
            {
                Array.Clear(CurrentPattern.Notes, 0, CurrentPattern.Length);
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task SwitchPatternAsync()
        {
            await _lock.WaitAsync();
            try
            {
                _project.CurrentPatternIndex = (_project.CurrentPatternIndex + 1) % _project.Patterns.Count;
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task SelectPattern(int index)
        {
            await _lock.WaitAsync();
            try
            {
                if (index >= 0 && index < _project.Patterns.Count)
                    _project.CurrentPatternIndex = index;
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task CopyPatternAsync()
        {
            await _lock.WaitAsync();
            try
            {
                var copy = CurrentPattern.Clone();
                _project.Patterns.Add(copy);
                _patternSteps[_project.Patterns.Count - 1] = 0;
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task DumpStateAsync()
        {
            await _lock.WaitAsync();
            try
            {
                Console.WriteLine($"Текущий BPM: {_project.Bpm}");
                Console.WriteLine($"Паттерн #{_currentPatternIndex} Длина: {CurrentPattern.Length}");
                Console.WriteLine($"Шаги: [{string.Join(" ", CurrentPattern.Notes)}]");
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task SaveProjectAsync(string filename)
        {
            await _lock.WaitAsync();
            try
            {
                using var writer = new StreamWriter(filename);
                new XmlSerializer(typeof(Project)).Serialize(writer, _project);
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task LoadProjectAsync(string filename)
        {
            await _lock.WaitAsync();
            try
            {
                using var reader = new StreamReader(filename);
                _project = (Project)new XmlSerializer(typeof(Project)).Deserialize(reader)!;
            }
            finally
            {
                _lock.Release();
            }
        }

        private void SendOsc(string address, params object[] args)
        {
            _sender.Send(new OscMessage(address, args));
        }

        public void Dispose()
        {
            _lock.Dispose();
            _playbackCts?.Dispose();
        }

#if DEBUG

        // Обновляем визуализацию
        private void UpdateVisualization(bool paused = false)
        {
            var vis = new StringBuilder();
            vis.AppendLine($"Режим: {_playbackMode} | BPM: {_project.Bpm} | Статус: {(paused ? "PS" : "PL")}");

            for (int i = 0; i < _project.Patterns.Count; i++)
            {
                var pattern = _project.Patterns[i];
                var currentStep = _patternSteps.GetValueOrDefault(i, 0);

                if (_playbackMode == PlaybackMode.Single)
                    vis.Append(i == _project.CurrentPatternIndex ? "!" : " ");
                vis.Append($"# {i}:");

                for (int j = 0; j < pattern.Length; j++)
                {
                    if (j == currentStep)
                        vis.Append($"({pattern.Notes[j]:D3})");
                    else
                        vis.Append(pattern.Notes[j] > 0 ? $"[{pattern.Notes[j]:D3}]" : "[   ]");
                }
                vis.AppendLine();
            }

            // Перемещаем курсор для перезаписи
            Console.SetCursorPosition(0, Console.CursorTop - _project.Patterns.Count - 1);
            Console.Write(vis.ToString());
        }

        private void DebugSound(int note)
        {
            try
            {
                // Укажите путь к вашему .wav файлу
                string filePath = note switch
                {
                    1 => @"Sounds\Hat.wav",
                    2 => @"Sounds\Snare.wav",
                    _ => @"Sounds\Kick.wav"
                };
                filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, filePath);

                // Создание экземпляра SoundPlayer
                SoundPlayer player = new SoundPlayer(filePath);

                // Воспроизведение звука
                player.PlaySync(); // Используйте Play() для асинхронного воспроизведения
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка воспроизведения звука: {ex.Message}");
            }
        }

#endif

        #endregion
    }
}
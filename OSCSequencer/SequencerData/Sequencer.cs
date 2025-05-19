using OSCSequencer.Osc;
using Rug.Osc;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Media;
using System.Text;
using System.Xml.Serialization;
using OscMessage = Rug.Osc.OscMessage;

namespace OSCSequencer.SequencerData
{
    public class Sequencer : IDisposable
    {
        public Sequencer(int initialBpm, int initialPatternLength, OscSender oscSender, OscSettings oscSettings)
        {
            OscSender = oscSender;
            OscSettings = oscSettings;
            Project = new()
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

        public Project Project { get; private set; }
        public OscSender OscSender { get; private set; }
        public OscSettings OscSettings { get; private set; }

        private readonly SemaphoreSlim _lock = new(1, 1);

        public PlaybackMode PlaybackMode { get; private set; }
        private List<int> _activePatterns = new();
        private readonly ConcurrentDictionary<int, int> _patternSteps = new();

        public bool IsVisualizationEnabled { get; set; } = false;

        public bool IsPlaying { get; private set; }
        public bool IsPaused { get; private set; }
        private CancellationTokenSource? _playbackCts;

        public Pattern CurrentPattern
        {
            get
            {
                if (Project.Patterns.Count == 0 || Project.Patterns.Count - 1 < Project.CurrentPatternIndex)
                {
                    while (Project.Patterns.Count <= Project.CurrentPatternIndex)
                    {
                        Project.Patterns.Add(new Pattern(16));
                    }
                }
                return Project.Patterns[Project.CurrentPatternIndex];
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
                    PlaybackMode = mode.Value;
                else
                {
                    PlaybackMode = PlaybackMode == PlaybackMode.All
                        ? PlaybackMode.Single
                        : PlaybackMode.All;
                }

                SetActivePatterns();
            }
            finally
            {
                _lock.Release();
            }
        }

        private void SetActivePatterns()
        {
            if (PlaybackMode == PlaybackMode.All)
                _activePatterns = Enumerable.Range(0, Project.Patterns.Count).ToList();
            else
                _activePatterns = new List<int> { Project.CurrentPatternIndex };
        }

        public async Task StartAsync()
        {
            await _lock.WaitAsync();
            try
            {
                if (IsPlaying) return;

                IsPlaying = true;
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

            while (IsPlaying && !ct.IsCancellationRequested)
            {
                if (IsPaused)
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
                        var pattern = Project.Patterns[patternIndex];
                        var step = _patternSteps.GetValueOrDefault(patternIndex, 0);

                        var note = pattern.Notes[step];
                        if (note > 0)
                        {
#if DEBUG
                            DebugSound(note);
#endif

                            SendOsc(string.Format(OscSettings.Messages["pattern_noteon"], patternIndex), note);
                            await Task.Delay(50, ct);
                            SendOsc(string.Format(OscSettings.Messages["pattern_noteoff"], patternIndex));
                        }

                        // Обновляем шаг для паттерна
                        _patternSteps[patternIndex] = (step + 1) % pattern.Length;
                    });

                    // Рассчитываем целевую длительность
                    var interval = 60000 / Project.Bpm;
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
                IsPlaying = false;
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
                IsPaused = !IsPaused;
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
                Project.Bpm = bpm;
                SendOsc(OscSettings.Messages["tempo"], bpm);
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task AddPatternAsync(int length)
        {
            await _lock.WaitAsync();
            try
            {
                Project.Patterns.Add(new Pattern(length));
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
                Project.Patterns[Project.CurrentPatternIndex] = newPattern;
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
                Project.CurrentPatternIndex = (Project.CurrentPatternIndex + 1) % Project.Patterns.Count;
                SetActivePatterns();
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
                if (index >= 0 && index < Project.Patterns.Count)
                    Project.CurrentPatternIndex = index;
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
                Project.Patterns.Add(copy);
                _patternSteps[Project.Patterns.Count - 1] = 0;
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
                Console.WriteLine($"Текущий BPM: {Project.Bpm}");
                foreach (var pattern in Project.Patterns)
                    Console.WriteLine($"Паттерн #{Project.Patterns.IndexOf(pattern)} Длина: {pattern.Length} Шаги: [{string.Join(" ", pattern.Notes)}]");
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
                new XmlSerializer(typeof(Project)).Serialize(writer, Project);
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
                Project = (Project)new XmlSerializer(typeof(Project)).Deserialize(reader)!;
            }
            finally
            {
                _lock.Release();
            }
        }

        public void SendOsc(string address, params object[] args)
        {
            OscSender.Send(new OscMessage(address, args));
        }

        public void Dispose()
        {
            _lock.Dispose();
            _playbackCts?.Dispose();
        }

        // Обновляем визуализацию
        private void UpdateVisualization(bool paused = false)
        {
            if (!IsVisualizationEnabled)
                return;

            var vis = new StringBuilder();
            vis.AppendLine($"Режим: {PlaybackMode} | BPM: {Project.Bpm} | Статус: {(paused ? "PS" : "PL")}");

            for (int i = 0; i < Project.Patterns.Count; i++)
            {
                var pattern = Project.Patterns[i];
                var currentStep = _patternSteps.GetValueOrDefault(i, 0);

                if (PlaybackMode == PlaybackMode.Single)
                    vis.Append(i == Project.CurrentPatternIndex ? "!" : " ");
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
            Console.SetCursorPosition(0, Console.CursorTop - Project.Patterns.Count - 1);
            Console.Write(vis.ToString());
        }

        internal void SwitchVisualizeMode()
        {
            IsVisualizationEnabled = !IsVisualizationEnabled;
        }

#if DEBUG

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
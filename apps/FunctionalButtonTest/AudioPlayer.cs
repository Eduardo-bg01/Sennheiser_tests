using System;
using System.Threading;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace BluetoothHeadphoneTest
{
    /// <summary>
    /// Mini reproductor con 3 "pistas" de tonos generados internamente.
    /// No requiere archivos de audio externos.
    /// Expone eventos observables para que los paneles de prueba los detecten.
    /// </summary>
    public class AudioPlayer : IDisposable
    {
        public enum PlayerState { Stopped, Playing, Paused }

        public event Action<PlayerState> StateChanged;
        public event Action<int> TrackChanged;   // índice de pista 0-2

        public PlayerState State    { get; private set; } = PlayerState.Stopped;
        public int         Track    { get; private set; } = 0;
        public float       Volume   { get; private set; } = 0.7f;

        private WaveOutEvent     _waveOut;
        private MixingSampleProvider _mixer;
        private SignalGenerator  _signal;
        private readonly object  _lock = new object();

        // Frecuencias de las 3 pistas (Do, Mi, Sol)
        private static readonly double[] Freqs = { 261.63, 329.63, 392.00 };
        private static readonly string[] TrackNames = { "Pista 1 — Do (261 Hz)", "Pista 2 — Mi (330 Hz)", "Pista 3 — Sol (392 Hz)" };

        public string TrackName => TrackNames[Track];

        public AudioPlayer()
        {
            BuildOutput();
        }

        private void BuildOutput()
        {
            _signal = new SignalGenerator(44100, 2)
            {
                Type      = SignalGeneratorType.Sin,
                Frequency = Freqs[Track],
                Gain      = 0f   // empieza en silencio
            };

            _waveOut = new WaveOutEvent { DesiredLatency = 100 };
            _waveOut.Init(_signal);
            _waveOut.Volume = Volume;
            _waveOut.Play();   // dispositivo abierto pero sin sonido hasta Play()
        }

        public void Play()
        {
            lock (_lock)
            {
                _signal.Gain  = 0.4f;
                _waveOut.Volume = Volume;
                State = PlayerState.Playing;
            }
            StateChanged?.Invoke(State);
        }

        public void Pause()
        {
            lock (_lock)
            {
                _signal.Gain = 0f;
                State = PlayerState.Paused;
            }
            StateChanged?.Invoke(State);
        }

        public void TogglePlayPause()
        {
            if (State == PlayerState.Playing) Pause();
            else Play();
        }

        public void NextTrack()
        {
            lock (_lock)
            {
                Track = (Track + 1) % 3;
                _signal.Frequency = Freqs[Track];
            }
            TrackChanged?.Invoke(Track);
        }

        public void PreviousTrack()
        {
            lock (_lock)
            {
                Track = (Track + 2) % 3;   // +2 mod 3 = -1 mod 3
                _signal.Frequency = Freqs[Track];
            }
            TrackChanged?.Invoke(Track);
        }

        public void SetVolume(float vol)
        {
            vol = Math.Max(0f, Math.Min(1f, vol));
            lock (_lock)
            {
                Volume = vol;
                if (_waveOut != null) _waveOut.Volume = vol;
            }
        }

        public void Dispose()
        {
            _signal.Gain = 0f;
            _waveOut?.Stop();
            _waveOut?.Dispose();
        }
    }
}

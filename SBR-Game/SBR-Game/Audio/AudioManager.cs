using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;
using System.IO;

namespace SBR_Game.Audio
{
    public enum SoundType
    {
        Hoofstep,
        BonusPickup,
        FinishLine,
        Player1Hit,
        Player2Hit
    }

    public class AudioManager : IDisposable
    {
        private static readonly WaveFormat MixFormat = WaveFormat.CreateIeeeFloatWaveFormat(44100, 2);

        private readonly ThreadSafeMixer _mixer;
        private readonly WaveOutEvent _output;

        private AudioFileReader? _musicReader;
        private VolumeSampleProvider? _musicGain;
        private bool _musicAdded;

        private readonly Dictionary<SoundType, List<CachedSound>?> _cache = new();

        private float _musicVol = 0.3f;
        private float _effectsVol = 0.3f;
        private float _hoofstepVol = 0.2f;

        public float MusicVolume
        {
            get => _musicVol;
            set { _musicVol = value; if (_musicGain != null) _musicGain.Volume = value; }
        }
        public float EffectsVolume { get => _effectsVol; set => _effectsVol = value; }
        public float HoofstepVolume { get => _hoofstepVol; set => _hoofstepVol = value; }

        private bool _disposed;
        private string? _menuMusicPath;
        private string? _gameMusicPath;
        private string? _finishMusicPath;

        public AudioManager()
        {
            _mixer = new ThreadSafeMixer(MixFormat);
            _output = new WaveOutEvent { DesiredLatency = 250 };
            _output.Init(_mixer);
            _output.Play();
        }

        public void Initialize(string contentPath)
        {
            string audio = Path.Combine(contentPath, "Audio");
            string music = Path.Combine(audio, "Music");
            string sfx = Path.Combine(audio, "SFX");

            _menuMusicPath = Path.Combine(music, "menu_music.mp3");
            _gameMusicPath = Path.Combine(music, "game_music.mp3");
            _finishMusicPath = Path.Combine(music, "finish_music.mp3");

            CacheSoundsFromDirectory(SoundType.Hoofstep, Path.Combine(sfx, "Hoofsteps"));
            CacheSoundsFromDirectory(SoundType.Player1Hit, Path.Combine(sfx, "Player1Hit"));
            CacheSoundsFromDirectory(SoundType.Player2Hit, Path.Combine(sfx, "Player2Hit"));
            CacheSound(SoundType.BonusPickup, Path.Combine(sfx, "bonus_pickup.wav"));
        }

        public void PlayMainMenuMusic() => PlayMusic(_menuMusicPath);
        public void PlayGameMusic() => PlayMusic(_gameMusicPath);
        public void PlayFinishMusic() => PlayMusic(_finishMusicPath);

        public void StopMusic()
        {
            if (!_musicAdded || _musicGain == null) return;
            _mixer.RemoveMixerInput(_musicGain);
            _musicAdded = false;
            _musicGain = null;
            _musicReader?.Dispose();
            _musicReader = null;
        }

        public void PlayHoofstepRandomPitch()
        {
            float pitch = (float)Random.Shared.NextDouble() * 0.2f + 0.9f;
            PlaySfxPitched(SoundType.Hoofstep, _hoofstepVol, pitch);
        }

        public void PlayObstacleHitPlayer1() => PlaySfx(SoundType.Player1Hit, _effectsVol);
        public void PlayObstacleHitPlayer2() => PlaySfx(SoundType.Player2Hit, _effectsVol);
        public void PlayBonusPickup() => PlaySfx(SoundType.BonusPickup, _effectsVol);

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _output.Stop();
            _output.Dispose();
            _musicReader?.Dispose();
        }

        private void PlayMusic(string? path)
        {
            StopMusic();
            if (path == null || !File.Exists(path)) return;

            try
            {
                _musicReader = new AudioFileReader(path);
                LoopStreamProvider looped = new LoopStreamProvider(_musicReader);
                ISampleProvider converted = ToMixFormat(looped);
                _musicGain = new VolumeSampleProvider(converted) { Volume = _musicVol };

                _mixer.AddMixerInput(_musicGain);
                _musicAdded = true;
            }
            catch
            {
                _musicReader?.Dispose();
                _musicReader = null;
            }
        }

        private void PlaySfx(SoundType type, float volume)
        {
            if (!_cache.TryGetValue(type, out var list) || list == null || list.Count == 0) return;

            var cached = list[Random.Shared.Next(list.Count)];
            ISampleProvider src = new CachedSoundSampleProvider(cached);
            src = new VolumeSampleProvider(src) { Volume = volume };
            _mixer.AddMixerInput(src);
        }

        private void PlaySfxPitched(SoundType type, float volume, float pitch)
        {
            if (!_cache.TryGetValue(type, out var list) || list == null || list.Count == 0) return;

            var cached = list[Random.Shared.Next(list.Count)];

            ISampleProvider src = new CachedSoundSampleProvider(cached);

            if (MathF.Abs(pitch - 1f) > 0.02f)
            {
                var pitchedFormat = WaveFormat.CreateIeeeFloatWaveFormat(
                    (int)(MixFormat.SampleRate / pitch), MixFormat.Channels);
                src = new RawSourceWaveStream(
                    new MemoryStream(FloatsToBytes(cached.AudioData)),
                    pitchedFormat).ToSampleProvider();
                src = new WdlResamplingSampleProvider(src, MixFormat.SampleRate);
            }

            src = new VolumeSampleProvider(src) { Volume = volume };
            _mixer.AddMixerInput(src);
        }

        private static byte[] FloatsToBytes(float[] data)
        {
            var bytes = new byte[data.Length * 4];
            Buffer.BlockCopy(data, 0, bytes, 0, bytes.Length);
            return bytes;
        }

        private void CacheSound(SoundType type, string path)
        {
            if (!File.Exists(path)) { _cache[type] = null; return; }
            try { _cache[type] = new List<CachedSound> { new CachedSound(path, MixFormat) }; }
            catch { _cache[type] = null; }
        }

        private void CacheSoundsFromDirectory(SoundType type, string directory)
        {
            if (!Directory.Exists(directory)) { _cache[type] = null; return; }

            var sounds = new List<CachedSound>();
            foreach (var file in Directory.GetFiles(directory, "*.wav"))
            {
                try { sounds.Add(new CachedSound(file, MixFormat)); }
                catch { }
            }
            _cache[type] = sounds.Count > 0 ? sounds : null;
        }

        private static ISampleProvider ToMixFormat(ISampleProvider src)
        {
            if (src.WaveFormat.Channels == 1)
                src = new MonoToStereoSampleProvider(src);

            if (src.WaveFormat.SampleRate != MixFormat.SampleRate)
                src = new WdlResamplingSampleProvider(src, MixFormat.SampleRate);

            return src;
        }
    }

    internal sealed class ThreadSafeMixer : ISampleProvider
    {
        private readonly WaveFormat _waveFormat;
        private volatile List<ISampleProvider> _sources = new();
        private readonly object _lock = new();

        public ThreadSafeMixer(WaveFormat waveFormat) => _waveFormat = waveFormat;
        public WaveFormat WaveFormat => _waveFormat;

        public void AddMixerInput(ISampleProvider input)
        {
            lock (_lock)
            {
                _sources = new List<ISampleProvider>(_sources) { input };
            }
        }

        public void RemoveMixerInput(ISampleProvider input)
        {
            lock (_lock)
            {
                var newList = new List<ISampleProvider>(_sources);
                newList.Remove(input);
                _sources = newList;
            }
        }

        public int Read(float[] buffer, int offset, int count)
        {
            var sources = _sources;
            List<ISampleProvider>? toRemove = null;

            for (int i = offset; i < offset + count; i++)
                buffer[i] = 0f;

            foreach (var source in sources)
            {
                float[] temp = new float[count];
                int read = source.Read(temp, 0, count);

                if (read == 0)
                {
                    toRemove ??= new List<ISampleProvider>();
                    toRemove.Add(source);
                    continue;
                }

                for (int i = 0; i < read; i++)
                {
                    float sample = buffer[offset + i] + temp[i];
                    buffer[offset + i] = sample > 1f ? 1f : sample < -1f ? -1f : sample;
                }
            }

            if (toRemove != null)
            {
                lock (_lock)
                {
                    var newList = new List<ISampleProvider>(_sources);
                    foreach (var s in toRemove)
                        newList.Remove(s);
                    _sources = newList;
                }
            }

            return count;
        }
    }

    public sealed class CachedSound
    {
        public float[] AudioData { get; }
        public WaveFormat WaveFormat { get; }

        public CachedSound(string path, WaveFormat targetFormat)
        {
            WaveFormat = targetFormat;

            using AudioFileReader reader = new AudioFileReader(path);

            ISampleProvider src = reader;

            if (src.WaveFormat.Channels == 1)
                src = new MonoToStereoSampleProvider(src);

            if (src.WaveFormat.SampleRate != targetFormat.SampleRate)
                src = new WdlResamplingSampleProvider(src, targetFormat.SampleRate);

            var samples = new List<float>();
            float[] buf = new float[4096];
            int n;
            while ((n = src.Read(buf, 0, buf.Length)) > 0)
                for (int i = 0; i < n; i++)
                    samples.Add(buf[i]);

            AudioData = samples.ToArray();
        }
    }

    public sealed class CachedSoundSampleProvider : ISampleProvider
    {
        private readonly CachedSound _sound;
        private int _pos;

        public WaveFormat WaveFormat => _sound.WaveFormat;
        public CachedSoundSampleProvider(CachedSound sound) => _sound = sound;

        public int Read(float[] buffer, int offset, int count)
        {
            int available = _sound.AudioData.Length - _pos;
            int toRead = Math.Min(count, available);
            Array.Copy(_sound.AudioData, _pos, buffer, offset, toRead);
            _pos += toRead;
            return toRead;
        }
    }

    internal sealed class LoopStreamProvider : ISampleProvider
    {
        private readonly AudioFileReader _reader;
        public WaveFormat WaveFormat => _reader.WaveFormat;

        public LoopStreamProvider(AudioFileReader reader) => _reader = reader;

        public int Read(float[] buffer, int offset, int count)
        {
            int totalRead = 0;
            int attempts = 0;

            while (totalRead < count && attempts < 2)
            {
                int read = _reader.Read(buffer, offset + totalRead, count - totalRead);
                if (read > 0)
                {
                    totalRead += read;
                    attempts = 0;
                }
                else
                {
                    _reader.Position = 0;
                    attempts++;
                }
            }

            return totalRead;
        }
    }
}
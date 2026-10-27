using UnityEngine;

namespace DisasterResponse.AudioVFX
{
    public class DisasterAudioEngine : MonoBehaviour
    {
        public static DisasterAudioEngine Instance { get; private set; }

        private AudioSource sfxSource;
        private AudioSource loopSource;
        private AudioSource radioSource;

        private AudioClip sfxBeep;
        private AudioClip sfxChime;
        private AudioClip sfxBuzzer;
        private AudioClip sfxEquip;
        private AudioClip sfxSizzle;
        private AudioClip sfxTriage;
        private AudioClip sfxStabilized;
        private AudioClip sfxEvac;
        private AudioClip sfxFlatline;
        private AudioClip sfxValve;
        private AudioClip sfxSpreader;
        private AudioClip loopExtinguisher;
        private AudioClip clipHeartbeat;
        private AudioClip clipBreathing;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;

            loopSource = gameObject.AddComponent<AudioSource>();
            loopSource.playOnAwake = false;
            loopSource.loop = true;

            radioSource = gameObject.AddComponent<AudioSource>();
            radioSource.playOnAwake = false;

            GenerateProceduralAudioClips();
            LoadImportedAudioClips();
        }

        private void LoadImportedAudioClips()
        {
            clipHeartbeat = Resources.Load<AudioClip>("Audio/heartbeat");
            clipBreathing = Resources.Load<AudioClip>("Audio/breathing");
        }

        private void GenerateProceduralAudioClips()
        {
            sfxBeep = CreateToneClip("Beep", 880f, 0.08f, Waveform.Sine);
            sfxChime = CreateChordClip("Chime", new float[] { 523.25f, 659.25f, 783.99f }, 0.25f);
            sfxBuzzer = CreateToneClip("Buzzer", 160f, 0.18f, Waveform.Sawtooth);
            sfxEquip = CreateToneClip("Equip", 440f, 0.06f, Waveform.Square);
            sfxSizzle = CreateNoiseClip("Sizzle", 0.15f);
            sfxTriage = CreateToneClip("Triage", 1046.5f, 0.12f, Waveform.Sine);
            sfxStabilized = CreateChordClip("Stabilized", new float[] { 440f, 554.37f, 659.25f, 880f }, 0.4f);
            sfxEvac = CreateChordClip("Evac", new float[] { 587.33f, 739.99f, 880f, 1174.66f }, 0.5f);
            sfxFlatline = CreateToneClip("Flatline", 440f, 0.8f, Waveform.Sine);
            sfxValve = CreateToneClip("Valve", 220f, 0.12f, Waveform.Triangle);
            sfxSpreader = CreateNoiseClip("Spreader", 0.2f);
            loopExtinguisher = CreateNoiseClip("ExtinguisherLoop", 1.0f);
        }

        private enum Waveform { Sine, Square, Sawtooth, Triangle }

        private AudioClip CreateToneClip(string name, float freq, float duration, Waveform form)
        {
            int sampleRate = 44100;
            int samples = (int)(sampleRate * duration);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = Mathf.Clamp01((samples - i) / (float)samples); // linear decay
                float val = 0f;

                switch (form)
                {
                    case Waveform.Sine:
                        val = Mathf.Sin(2f * Mathf.PI * freq * t);
                        break;
                    case Waveform.Square:
                        val = Mathf.Sin(2f * Mathf.PI * freq * t) >= 0f ? 0.6f : -0.6f;
                        break;
                    case Waveform.Sawtooth:
                        val = 2f * (t * freq - Mathf.Floor(t * freq + 0.5f));
                        break;
                    case Waveform.Triangle:
                        val = Mathf.PingPong(t * freq * 2f, 1f) * 2f - 1f;
                        break;
                }

                data[i] = val * envelope * 0.4f;
            }

            AudioClip clip = AudioClip.Create(name, samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip CreateChordClip(string name, float[] frequencies, float duration)
        {
            int sampleRate = 44100;
            int samples = (int)(sampleRate * duration);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = Mathf.Clamp01((samples - i) / (float)samples);
                float val = 0f;

                for (int f = 0; f < frequencies.Length; f++)
                {
                    val += Mathf.Sin(2f * Mathf.PI * frequencies[f] * t);
                }

                val /= frequencies.Length;
                data[i] = val * envelope * 0.4f;
            }

            AudioClip clip = AudioClip.Create(name, samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip CreateNoiseClip(string name, float duration)
        {
            int sampleRate = 44100;
            int samples = (int)(sampleRate * duration);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                data[i] = (Random.value * 2f - 1f) * 0.25f;
            }

            AudioClip clip = AudioClip.Create(name, samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        public void PlayScannerBeep() => sfxSource.PlayOneShot(sfxBeep, 0.6f);
        public void PlayDiscoveryChime() => sfxSource.PlayOneShot(sfxChime, 0.8f);
        public void PlayWarningBuzzer() => sfxSource.PlayOneShot(sfxBuzzer, 0.7f);
        public void PlayToolEquipSound() => sfxSource.PlayOneShot(sfxEquip, 0.5f);
        public void PlayFireSizzleSound() => sfxSource.PlayOneShot(sfxSizzle, 0.4f);
        public void PlayExtinguishedSound() => sfxSource.PlayOneShot(sfxChime, 0.9f);
        public void PlayTriageConfirmSound() => sfxSource.PlayOneShot(sfxTriage, 0.7f);
        public void PlayStabilizedSound() => sfxSource.PlayOneShot(sfxStabilized, 0.9f);
        public void PlayEvacSuccessSound() => sfxSource.PlayOneShot(sfxEvac, 1.0f);
        public void PlayFlatlineSound() => sfxSource.PlayOneShot(sfxFlatline, 0.8f);
        public void PlayValveTurnSound() => sfxSource.PlayOneShot(sfxValve, 0.6f);
        public void PlayValveLockedSound() => sfxSource.PlayOneShot(sfxChime, 0.8f);
        public void PlaySpreaderWorkSound() => sfxSource.PlayOneShot(sfxSpreader, 0.5f);
        public void PlayRubbleClearedSound() => sfxSource.PlayOneShot(sfxChime, 0.85f);

        public void PlayHeartbeatSound(float pitch = 1.0f)
        {
            if (clipHeartbeat != null)
            {
                sfxSource.pitch = Mathf.Clamp(pitch, 0.6f, 2.0f);
                sfxSource.PlayOneShot(clipHeartbeat, 0.8f);
                sfxSource.pitch = 1.0f;
            }
            else
            {
                PlayScannerBeep();
            }
        }

        public void PlayBreathingSound()
        {
            if (clipBreathing != null)
            {
                sfxSource.PlayOneShot(clipBreathing, 0.7f);
            }
        }

        public void StartExtinguisherLoop()
        {
            if (!loopSource.isPlaying)
            {
                loopSource.clip = loopExtinguisher;
                loopSource.Play();
            }
        }

        public void StopExtinguisherLoop()
        {
            if (loopSource.isPlaying)
            {
                loopSource.Stop();
            }
        }
    }
}

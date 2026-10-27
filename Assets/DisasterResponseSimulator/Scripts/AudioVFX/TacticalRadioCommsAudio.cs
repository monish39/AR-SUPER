using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DisasterResponse.AudioVFX
{
    public class TacticalRadioCommsAudio : MonoBehaviour
    {
        public static TacticalRadioCommsAudio Instance { get; private set; }

        private AudioSource radioSource;
        private AudioClip clipSquelchIn;
        private AudioClip clipSquelchOut;
        private AudioClip clipRadioStatic;
        private Queue<string> transmissionQueue = new Queue<string>();
        private bool isTransmitting = false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            radioSource = gameObject.AddComponent<AudioSource>();
            radioSource.playOnAwake = false;
            radioSource.spatialBlend = 0f; // Direct 2D radio ear-piece comms
            radioSource.volume = 0.85f;

            GenerateProceduralRadioSounds();
        }

        private void GenerateProceduralRadioSounds()
        {
            clipSquelchIn = CreateNoiseBurstClip("Radio_Squelch_In", 0.08f, 2400f);
            clipSquelchOut = CreateNoiseBurstClip("Radio_Squelch_Out", 0.12f, 1800f);
            clipRadioStatic = CreateNoiseBurstClip("Radio_Static", 0.25f, 1200f);
        }

        private AudioClip CreateNoiseBurstClip(string name, float duration, float centerFreq)
        {
            int sampleRate = 22050;
            int samples = (int)(sampleRate * duration);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float noise = Random.Range(-0.8f, 0.8f);
                float envelope = Mathf.Sin(t / duration * Mathf.PI);
                data[i] = noise * envelope * 0.45f;
            }

            AudioClip clip = AudioClip.Create(name, samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        public void DispatchRadioTransmission(string messageText)
        {
            StartCoroutine(PlayRadioSequence(messageText));
        }

        private IEnumerator PlayRadioSequence(string message)
        {
            if (isTransmitting) yield return new WaitForSeconds(1.5f);
            isTransmitting = true;

            // 1. Radio Click / Squelch In
            if (clipSquelchIn != null)
            {
                radioSource.PlayOneShot(clipSquelchIn, 0.7f);
                yield return new WaitForSeconds(0.12f);
            }

            // 2. Play Dispatch Beep Chime
            DisasterAudioEngine.Instance?.PlayDiscoveryChime();

            // 3. Log tactical dispatch into HUD
            var hud = FindFirstObjectByType<UI.ICSHUDController>();
            if (hud != null)
            {
                hud.UpdatePromptBanner($"<color=#FFAA00>[RADIO COMMS]</color> {message}");
            }

            yield return new WaitForSeconds(2.0f);

            // 4. Radio Click / Squelch Out
            if (clipSquelchOut != null)
            {
                radioSource.PlayOneShot(clipSquelchOut, 0.6f);
            }

            isTransmitting = false;
        }

        public void PlayIncidentStartedComms()
        {
            DispatchRadioTransmission("Dispatch to Rescue 1: Scene is active. Establish ICS perimeter and triage all casualties.");
        }

        public void PlayHazardContainedComms(string hazardName)
        {
            DispatchRadioTransmission($"Engine 2 to Command: {hazardName} successfully isolated and contained. Threat suppressed.");
        }

        public void PlayPatientEvacuatedComms(string patientName)
        {
            DispatchRadioTransmission($"EMS Transport: Casualty {patientName} loaded on gurney. En route to Trauma Center.");
        }

        public void PlayStructuralWarningComms()
        {
            DispatchRadioTransmission("Command to all units: Structural integrity weakening. Prioritize Immediate Red casualties!");
        }
    }
}

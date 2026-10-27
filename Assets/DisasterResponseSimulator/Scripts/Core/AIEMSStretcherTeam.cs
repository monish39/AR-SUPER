using System.Collections;
using UnityEngine;
using DisasterResponse.Victims;
using DisasterResponse.AudioVFX;

namespace DisasterResponse.Core
{
    public class AIEMSStretcherTeam : MonoBehaviour
    {
        public static void DeployEvacuationTeam(VictimController victim, Vector3 landingZonePos)
        {
            var teamGo = new GameObject("AI_EMS_Evacuation_Unit");
            var team = teamGo.AddComponent<AIEMSStretcherTeam>();
            team.StartCoroutine(team.ExecuteEvacuationRoutine(victim, landingZonePos));
        }

        private IEnumerator ExecuteEvacuationRoutine(VictimController victim, Vector3 landingZonePos)
        {
            if (victim == null) yield break;

            // Auto-fallback for landing zone position
            if (landingZonePos == Vector3.zero)
            {
                var lz = GameObject.Find("EvacuationLandingZone");
                if (lz != null) landingZonePos = lz.transform.position;
                else landingZonePos = new Vector3(0, 0, -3.8f);
            }

            Vector3 victimPos = victim.transform.position;

            // 1. Construct Tactical Mobile Stretcher Gurney
            var gurney = GameObject.CreatePrimitive(PrimitiveType.Cube);
            gurney.name = "TacticalGurney";
            gurney.transform.position = landingZonePos + Vector3.up * 0.35f;
            gurney.transform.localScale = new Vector3(0.6f, 0.2f, 1.8f);

            var litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var gurneyMat = new Material(litShader);
            gurneyMat.color = new Color(0.9f, 0.45f, 0.1f); // Emergency rescue orange
            if (gurneyMat.HasProperty("_BaseColor")) gurneyMat.SetColor("_BaseColor", new Color(0.9f, 0.45f, 0.1f));
            gurney.GetComponent<MeshRenderer>().material = gurneyMat;

            // Add Emergency Warning Strobe Beacons on Gurney
            var sirenLight = gurney.AddComponent<Light>();
            sirenLight.type = LightType.Point;
            sirenLight.color = Color.red;
            sirenLight.range = 4.0f;
            sirenLight.intensity = 2.0f;

            // 2. Paramedic Figures (Front & Rear)
            var medic1 = CreateMedicFigure(gurney.transform, new Vector3(0, 0.4f, 1.1f), "Medic_Front");
            var medic2 = CreateMedicFigure(gurney.transform, new Vector3(0, 0.4f, -1.1f), "Medic_Rear");

            // Radio Dispatch notification
            TacticalRadioCommsAudio.Instance?.DispatchRadioTransmission($"EMS Team deployed to casualty {victim.Vitals.victimName}. Moving to patient.");
            DisasterAudioEngine.Instance?.PlayDiscoveryChime();

            // 3. Move from Landing Zone to Victim (Travel Phase)
            float travelTime = 2.5f;
            float elapsed = 0f;
            Vector3 startPos = gurney.transform.position;
            Vector3 targetPos = victimPos + Vector3.up * 0.35f;

            while (elapsed < travelTime)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / travelTime);
                gurney.transform.position = Vector3.Lerp(startPos, targetPos, t);
                gurney.transform.LookAt(victimPos + Vector3.up * 0.35f);

                // Alternating red/blue emergency strobe
                sirenLight.color = (Mathf.Repeat(Time.time * 6f, 1f) > 0.5f) ? Color.red : Color.blue;

                // Paramedic stride bobbing
                float bob = Mathf.Sin(Time.time * 12f) * 0.04f;
                medic1.transform.localPosition = new Vector3(0, 0.4f + bob, 1.1f);
                medic2.transform.localPosition = new Vector3(0, 0.4f - bob, -1.1f);

                yield return null;
            }

            // 4. Secure Victim on Gurney
            yield return new WaitForSeconds(0.6f);
            victim.transform.SetParent(gurney.transform, true);
            victim.transform.localPosition = new Vector3(0, 0.15f, 0);
            victim.transform.localRotation = Quaternion.identity;

            TacticalRadioCommsAudio.Instance?.DispatchRadioTransmission($"Casualty {victim.Vitals.victimName} secured on transport. Returning to evacuation zone.");

            // 5. Return to Landing Zone
            elapsed = 0f;
            startPos = gurney.transform.position;
            Vector3 returnTarget = landingZonePos + Vector3.up * 0.35f;

            while (elapsed < travelTime)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / travelTime);
                gurney.transform.position = Vector3.Lerp(startPos, returnTarget, t);
                gurney.transform.LookAt(returnTarget);

                // Alternating red/blue emergency strobe
                sirenLight.color = (Mathf.Repeat(Time.time * 6f, 1f) > 0.5f) ? Color.red : Color.blue;

                // Paramedic stride bobbing
                float bob = Mathf.Sin(Time.time * 12f) * 0.04f;
                medic1.transform.localPosition = new Vector3(0, 0.4f + bob, 1.1f);
                medic2.transform.localPosition = new Vector3(0, 0.4f - bob, -1.1f);

                yield return null;
            }

            // 6. Complete Evacuation
            DisasterAudioEngine.Instance?.PlayEvacSuccessSound();
            TacticalRadioCommsAudio.Instance?.PlayPatientEvacuatedComms(victim.Vitals.victimName);

            // Hide victim and clean up gurney team
            victim.gameObject.SetActive(false);
            Destroy(gurney, 1.2f);
            Destroy(gameObject, 1.5f);
        }

        private GameObject CreateMedicFigure(Transform parent, Vector3 localPos, string name)
        {
            var medicRoot = new GameObject(name);
            medicRoot.transform.SetParent(parent, false);
            medicRoot.transform.localPosition = localPos;

            var litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var medicMat = new Material(litShader);
            medicMat.color = new Color(0.12f, 0.35f, 0.85f); // High-visibility paramedic uniform
            if (medicMat.HasProperty("_BaseColor")) medicMat.SetColor("_BaseColor", new Color(0.12f, 0.35f, 0.85f));

            // Body
            var body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            body.transform.SetParent(medicRoot.transform, false);
            body.transform.localPosition = new Vector3(0, 0.3f, 0);
            body.transform.localScale = new Vector3(0.3f, 0.45f, 0.3f);
            body.GetComponent<MeshRenderer>().material = medicMat;
            var col = body.GetComponent<Collider>();
            if (col != null) Destroy(col);

            // Head with helmet
            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.transform.SetParent(medicRoot.transform, false);
            head.transform.localPosition = new Vector3(0, 0.85f, 0);
            head.transform.localScale = new Vector3(0.24f, 0.24f, 0.24f);
            var hcol = head.GetComponent<Collider>();
            if (hcol != null) Destroy(hcol);

            return medicRoot;
        }
    }
}

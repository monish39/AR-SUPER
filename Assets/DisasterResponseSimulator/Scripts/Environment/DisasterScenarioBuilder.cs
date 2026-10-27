using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DisasterResponse.Core;
using DisasterResponse.Hazards;
using DisasterResponse.Victims;
using DisasterResponse.AudioVFX;

namespace DisasterResponse.Environment
{
    public class DisasterScenarioBuilder : MonoBehaviour
    {
        private Material floorMat;
        private Material rubbleMat;
        private Material metalMat;
        private Material gownMat;
        private Material skinMat;
        private Material bandageMat;
        private Material bloodMat;
        private Material boundaryMat;

        private void InitializeMaterials()
        {
            Shader litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Universal Render Pipeline/Simple Lit") ?? Shader.Find("Standard");
            Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");

            // Semi-transparent AR Tactical floor so real room/table is visible underneath!
            if (floorMat == null)
            {
                floorMat = new Material(litShader);
                floorMat.name = "AR_TransparentFloorMat";
                floorMat.SetFloat("_Surface", 1); // Transparent
                floorMat.SetFloat("_Blend", 0);   // Alpha
                floorMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                floorMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                floorMat.SetInt("_ZWrite", 0);
                floorMat.renderQueue = 3000;
                Color arFloorCol = new Color(0.08f, 0.12f, 0.18f, 0.35f);
                floorMat.color = arFloorCol;
                if (floorMat.HasProperty("_BaseColor")) floorMat.SetColor("_BaseColor", arFloorCol);
                if (floorMat.HasProperty("_Smoothness")) floorMat.SetFloat("_Smoothness", 0.8f);
            }

            if (boundaryMat == null)
            {
                boundaryMat = new Material(litShader);
                boundaryMat.name = "AR_BoundaryMat";
                Color pylonCol = new Color(0.1f, 0.75f, 1.0f, 0.9f);
                boundaryMat.color = pylonCol;
                if (boundaryMat.HasProperty("_BaseColor")) boundaryMat.SetColor("_BaseColor", pylonCol);
                if (boundaryMat.HasProperty("_EmissionColor"))
                {
                    boundaryMat.EnableKeyword("_EMISSION");
                    boundaryMat.SetColor("_EmissionColor", pylonCol * 1.5f);
                }
            }

            if (rubbleMat == null) rubbleMat = CreateLitMaterial(litShader, new Color(0.55f, 0.52f, 0.48f), 0.05f, 0.2f);
            if (metalMat == null) metalMat = CreateLitMaterial(litShader, new Color(0.38f, 0.42f, 0.46f), 0.75f, 0.55f);
            if (gownMat == null) gownMat = CreateLitMaterial(litShader, new Color(0.20f, 0.55f, 0.85f), 0.0f, 0.3f);
            if (skinMat == null) skinMat = CreateLitMaterial(litShader, new Color(0.88f, 0.72f, 0.60f), 0.0f, 0.4f);
            if (bandageMat == null) bandageMat = CreateLitMaterial(litShader, new Color(0.92f, 0.90f, 0.86f), 0.0f, 0.2f);
            if (bloodMat == null)
            {
                bloodMat = CreateLitMaterial(litShader, new Color(0.68f, 0.03f, 0.03f, 0.95f), 0.1f, 0.95f);
            }
        }

        private Material CreateLitMaterial(Shader shader, Color color, float metallic = 0.1f, float smoothness = 0.4f)
        {
            var mat = new Material(shader);
            mat.color = color;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            return mat;
        }

        public void BuildScenario(DisasterScenarioType scenarioType, Transform root)
        {
            InitializeMaterials();

            // Clear old children
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                Destroy(root.GetChild(i).gameObject);
            }

            // Create Base Disaster Zone Platform
            CreateZonePlatform(root);

            switch (scenarioType)
            {
                case DisasterScenarioType.EarthquakeCollapse:
                    BuildEarthquakeScenario(root);
                    break;
                case DisasterScenarioType.GasLeakAndFire:
                    BuildGasAndFireScenario(root);
                    break;
                case DisasterScenarioType.HospitalMassCasualty:
                    BuildHospitalMCIScenario(root);
                    break;
            }
        }

        private void CreateZonePlatform(Transform root)
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "ZonePlatform";
            floor.transform.SetParent(root, false);
            floor.transform.localPosition = new Vector3(0, 0.01f, 0);
            floor.transform.localScale = new Vector3(9.5f, 0.02f, 9.5f);
            floor.GetComponent<MeshRenderer>().material = floorMat;

            // AR Soft Shadow Receiver Plane (Captures 3D object shadows on top of WebCam passthrough!)
            var shadowGo = GameObject.CreatePrimitive(PrimitiveType.Plane);
            shadowGo.name = "AR_Shadow_Receiver";
            shadowGo.transform.SetParent(root, false);
            shadowGo.transform.localPosition = new Vector3(0, 0.025f, 0);
            shadowGo.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
            var sCol = shadowGo.GetComponent<Collider>();
            if (sCol != null) Destroy(sCol);

            var smr = shadowGo.GetComponent<MeshRenderer>();
            var shadowShader = Shader.Find("DisasterResponse/ARShadowReceiver");
            if (shadowShader != null)
            {
                smr.material = new Material(shadowShader);
            }
            else
            {
                smr.enabled = false;
            }

            // Holographic corner brackets
            Vector3[] corners = new Vector3[]
            {
                new Vector3(-4.6f, 0.15f, -4.6f),
                new Vector3(4.6f, 0.15f, -4.6f),
                new Vector3(-4.6f, 0.15f, 4.6f),
                new Vector3(4.6f, 0.15f, 4.6f)
            };

            for (int i = 0; i < corners.Length; i++)
            {
                var post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                post.name = $"BoundaryPylon_{i}";
                post.transform.SetParent(root, false);
                post.transform.localPosition = corners[i];
                post.transform.localScale = new Vector3(0.12f, 0.15f, 0.12f);
                var mr = post.GetComponent<MeshRenderer>();
                mr.material = boundaryMat;

                // Subtle beacon light on corner pylons
                var pylonLight = post.AddComponent<Light>();
                pylonLight.type = LightType.Point;
                pylonLight.color = new Color(0.1f, 0.75f, 1f);
                pylonLight.range = 2.0f;
                pylonLight.intensity = 0.8f;
            }
        }

        private void BuildEarthquakeScenario(Transform root)
        {
            // 1. Fallen Cracked Concrete Pillars (Lying horizontally as collapsed structural debris)
            InstantiateModelOrFallback("Pillar", root, new Vector3(-2.2f, 0.20f, 0.6f), new Vector3(85f, 25f, 0), 1.8f);
            InstantiateModelOrFallback("Pillar", root, new Vector3(1.6f, 0.20f, -1.0f), new Vector3(88f, -40f, 0), 1.8f);
            InstantiateModelOrFallback("Pillar", root, new Vector3(0.3f, 0.20f, 2.6f), new Vector3(82f, 75f, 0), 1.5f);

            // 2. Twisted Steel Pipes & Debris Crates
            InstantiateModelOrFallback("Pipe", root, new Vector3(-1.2f, 0.15f, -2.0f), new Vector3(0, 35f, 85f), 1.3f);
            InstantiateModelOrFallback("Pipe", root, new Vector3(2.5f, 0.15f, 1.6f), new Vector3(15f, -40f, 10f), 1.2f);
            InstantiateModelOrFallback("WheelChair", root, new Vector3(-3.0f, 0.15f, -1.5f), new Vector3(65f, 30f, 45f), 0.95f);
            InstantiateModelOrFallback("Box_V1", root, new Vector3(1.1f, 0.15f, -2.8f), new Vector3(0, 20f, 0), 0.65f);
            InstantiateModelOrFallback("SmallMetalicCase", root, new Vector3(-2.4f, 0.15f, 2.6f), new Vector3(0, 60f, 0), 0.55f);

            // 3. Trapped Victim under heavy concrete rubble
            var trappedVictim = CreateRealisticVictim(root, new Vector3(-1.8f, 0.12f, 1.8f), "Marcus Reed",
                InjuryType.CrushInjury, TriageCategory.Immediate, 75, 92f, 130, isPinned: true, hasBleeding: true);

            CreateRubbleObstacle(root, new Vector3(-1.8f, 0.22f, 1.8f), trappedVictim);

            // 4. Victim with severe hemorrhage & blood pool
            CreateRealisticVictim(root, new Vector3(2.0f, 0.12f, 1.2f), "Elena Rostova",
                InjuryType.SevereHemorrhage, TriageCategory.Immediate, 65, 95f, 145, isPinned: false, hasBleeding: true);

            // 5. Victim with compound limb fracture
            CreateRealisticVictim(root, new Vector3(-0.8f, 0.12f, -2.2f), "David Chen",
                InjuryType.Fracture, TriageCategory.Delayed, 85, 97f, 120, isPinned: false, hasBleeding: false);

            // 6. Walking wounded casualty with shock
            CreateRealisticVictim(root, new Vector3(2.8f, 0.12f, -2.0f), "Sarah Miller",
                InjuryType.MinorLacerations, TriageCategory.Minimal, 95, 99f, 115, isPinned: false, hasBleeding: false);

            // 7. Dust particle atmospheric effect
            ProceduralVFXBuilder.CreateDustParticles(root, new Vector3(0, 1.2f, 0));

            // 8. Dedicated Tactical Evacuation Landing Zone
            CreateEvacuationZoneMarker(root, new Vector3(0, 0.05f, -3.8f));
        }

        private void BuildGasAndFireScenario(Transform root)
        {
            // 1. Industrial Boiler Unit & Fuel Tanks (3D Models)
            InstantiateModelOrFallback("BigBoiler", root, new Vector3(-2.5f, 0.1f, -0.5f), new Vector3(0, 45f, 0), 2.2f);
            InstantiateModelOrFallback("Tank", root, new Vector3(2.4f, 0.1f, -2.2f), Vector3.zero, 1.7f);
            InstantiateModelOrFallback("Tank_V2", root, new Vector3(3.0f, 0.1f, -1.0f), new Vector3(0, 90f, 0), 1.5f);
            InstantiateModelOrFallback("Heater", root, new Vector3(0.5f, 0.1f, -2.0f), new Vector3(0, -30f, 0), 1.0f);

            // 2. Ruptured Industrial Gas Line with Interactive Isolation Valve
            CreateGasLeakStructure(root, new Vector3(-2.0f, 0.1f, 0f));

            // 3. Active Structural Fire Hazards (Engulfing debris with volumetric smoke & light)
            CreateFireStructure(root, new Vector3(1.8f, 0.1f, 1.5f));
            CreateFireStructure(root, new Vector3(0.5f, 0.1f, -1.8f));

            // 4. Smoke Inhalation Victim with Oxygen Distress
            CreateRealisticVictim(root, new Vector3(-1.0f, 0.12f, 0.8f), "Arthur Pendelton",
                InjuryType.SmokeInhalation, TriageCategory.Immediate, 70, 84f, 135, isPinned: false, hasBleeding: false, airwayObstructed: true);

            // 5. Thermal Burns Victim
            CreateRealisticVictim(root, new Vector3(1.0f, 0.12f, 0.4f), "Jackson Vance",
                InjuryType.SevereBurns, TriageCategory.Immediate, 60, 93f, 150, isPinned: false, hasBleeding: false);

            // 6. Worker in shock
            CreateRealisticVictim(root, new Vector3(2.5f, 0.12f, -1.5f), "Chloe Bennett",
                InjuryType.MinorLacerations, TriageCategory.Minimal, 90, 98f, 118, isPinned: false, hasBleeding: false);

            // 7. Tactical Evacuation Landing Zone
            CreateEvacuationZoneMarker(root, new Vector3(0, 0.05f, -3.8f));
        }

        private void BuildHospitalMCIScenario(Transform root)
        {
            // 1. Realistic Hospital Beds (3D Models)
            InstantiateModelOrFallback("SurgeryBed", root, new Vector3(-2.6f, 0.1f, 1.8f), new Vector3(0, 90f, 0), 2.1f);
            InstantiateModelOrFallback("SurgeryBed", root, new Vector3(2.6f, 0.1f, 1.8f), new Vector3(0, -90f, 0), 2.1f);
            InstantiateModelOrFallback("SurgeryWheelBad", root, new Vector3(-2.6f, 0.1f, -1.8f), new Vector3(0, 90f, 0), 2.1f);
            InstantiateModelOrFallback("SurgeryBed", root, new Vector3(2.6f, 0.1f, -1.8f), new Vector3(0, -90f, 0), 2.1f);

            // 2. Hospital Ward Medical Equipment
            InstantiateModelOrFallback("WheelChair", root, new Vector3(0.5f, 0.1f, 2.4f), new Vector3(0, -35f, 0), 0.95f);
            InstantiateModelOrFallback("MedRack", root, new Vector3(-4.0f, 0.1f, 0f), new Vector3(0, 90f, 0), 1.6f);
            InstantiateModelOrFallback("MedRack_V2", root, new Vector3(4.0f, 0.1f, 0f), new Vector3(0, -90f, 0), 1.6f);
            InstantiateModelOrFallback("Crutches", root, new Vector3(1.4f, 0.15f, -1.8f), new Vector3(85f, 15f, 0), 1.0f);

            // 3. Central Evacuation Landing Zone
            CreateEvacuationZoneMarker(root, new Vector3(0, 0.05f, 0));

            // 4. Multi-Casualty Patients Lying on Hospital Beds
            CreateRealisticVictim(root, new Vector3(-2.6f, 0.78f, 1.8f), "Patient #101: Blunt Trauma",
                InjuryType.BluntTrauma, TriageCategory.Immediate, 50, 88f, 140, isPinned: false, hasBleeding: true);

            CreateRealisticVictim(root, new Vector3(2.6f, 0.78f, 1.8f), "Patient #102: Respiratory Distress",
                InjuryType.SmokeInhalation, TriageCategory.Delayed, 80, 91f, 125, isPinned: false, hasBleeding: false, airwayObstructed: true);

            CreateRealisticVictim(root, new Vector3(-2.6f, 0.78f, -1.8f), "Patient #103: Compound Fracture",
                InjuryType.Fracture, TriageCategory.Delayed, 88, 96f, 118, isPinned: false, hasBleeding: false);

            CreateRealisticVictim(root, new Vector3(2.6f, 0.78f, -1.8f), "Patient #104: Post-Op Walking Wounded",
                InjuryType.MinorLacerations, TriageCategory.Minimal, 98, 99f, 112, isPinned: false, hasBleeding: false);

            // Localized electrical fire hazard
            CreateFireStructure(root, new Vector3(0, 0.1f, 2.8f));
        }

        #region Helper Creators

        private GameObject InstantiateModelOrFallback(string modelName, Transform parent, Vector3 localPos, Vector3 localEuler, float targetSize, PrimitiveType fallbackType = PrimitiveType.Cube)
        {
            GameObject prefab = Resources.Load<GameObject>($"Models/{modelName}");
            GameObject instance = null;

            if (prefab != null)
            {
                instance = Instantiate(prefab, parent);
                instance.name = modelName;
                instance.transform.localPosition = localPos;
                instance.transform.localEulerAngles = localEuler;
                instance.transform.localScale = Vector3.one;

                // Normalize model scale to real-world meters
                NormalizeModelSize(instance, targetSize);
            }
            else
            {
                instance = GameObject.CreatePrimitive(fallbackType);
                instance.name = $"{modelName}_Fallback";
                instance.transform.SetParent(parent, false);
                instance.transform.localPosition = localPos;
                instance.transform.localEulerAngles = localEuler;
                instance.transform.localScale = Vector3.one * targetSize;
                var mr = instance.GetComponent<MeshRenderer>();
                if (mr != null) mr.material = metalMat;
            }

            // Sanitize all materials to URP Lit with realistic PBR properties (fixes bright pink shader errors!)
            SanitizeAndStyleModel(instance, modelName);

            // Ensure colliders exist for raycasting
            if (instance.GetComponentInChildren<Collider>() == null)
            {
                var col = instance.AddComponent<BoxCollider>();
            }

            return instance;
        }

        private static void NormalizeModelSize(GameObject instance, float maxDimension)
        {
            var renderers = instance.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;

            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                b.Encapsulate(renderers[i].bounds);
            }

            float currentMax = Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
            if (currentMax > 0.02f)
            {
                float ratio = maxDimension / currentMax;
                instance.transform.localScale *= ratio;
            }
        }

        private void SanitizeAndStyleModel(GameObject instance, string modelName)
        {
            Shader litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Universal Render Pipeline/Simple Lit") ?? Shader.Find("Standard");

            Color defaultColor;
            float metallic = 0.1f;
            float smoothness = 0.4f;

            string s = modelName.ToLower();
            if (s.Contains("pillar"))
            {
                defaultColor = new Color(0.55f, 0.52f, 0.48f); // Aged concrete rubble
                metallic = 0.05f;
                smoothness = 0.2f;
            }
            else if (s.Contains("boiler") || s.Contains("tank"))
            {
                defaultColor = new Color(0.32f, 0.38f, 0.42f); // Industrial painted steel
                metallic = 0.8f;
                smoothness = 0.6f;
            }
            else if (s.Contains("pipe"))
            {
                defaultColor = new Color(0.72f, 0.60f, 0.22f); // Hazard yellow-gold gas line
                metallic = 0.7f;
                smoothness = 0.5f;
            }
            else if (s.Contains("bed") || s.Contains("wheelbad"))
            {
                defaultColor = new Color(0.85f, 0.88f, 0.90f); // Clean hospital steel & vinyl
                metallic = 0.5f;
                smoothness = 0.6f;
            }
            else if (s.Contains("wheelchair"))
            {
                defaultColor = new Color(0.24f, 0.26f, 0.28f); // Dark leather & steel frame
                metallic = 0.6f;
                smoothness = 0.5f;
            }
            else if (s.Contains("box") || s.Contains("case"))
            {
                defaultColor = new Color(0.52f, 0.44f, 0.36f); // Industrial composite crate
                metallic = 0.3f;
                smoothness = 0.3f;
            }
            else if (s.Contains("crutch"))
            {
                defaultColor = new Color(0.76f, 0.80f, 0.84f); // Aluminum crutches
                metallic = 0.85f;
                smoothness = 0.7f;
            }
            else if (s.Contains("medrack"))
            {
                defaultColor = new Color(0.80f, 0.83f, 0.86f); // Medical enamel cabinet
                metallic = 0.4f;
                smoothness = 0.6f;
            }
            else
            {
                defaultColor = new Color(0.55f, 0.55f, 0.55f);
            }

            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
            foreach (var rend in renderers)
            {
                Material[] mats = rend.materials;
                for (int i = 0; i < mats.Length; i++)
                {
                    Material m = mats[i];
                    Color col = defaultColor;
                    Texture mainTex = null;

                    if (m != null)
                    {
                        if (m.HasProperty("_Color")) col = m.color;
                        else if (m.HasProperty("_BaseColor")) col = m.GetColor("_BaseColor");

                        // If color is error magenta (r > 0.85, b > 0.85, g < 0.3), replace with tuned model color
                        if (col.r > 0.80f && col.b > 0.80f && col.g < 0.30f)
                        {
                            col = defaultColor;
                        }

                        if (m.HasProperty("_MainTex") && m.mainTexture != null) mainTex = m.mainTexture;
                        else if (m.HasProperty("_BaseMap")) mainTex = m.GetTexture("_BaseMap");
                    }

                    // Create fresh URP Lit material
                    Material cleanLitMat = new Material(litShader);
                    cleanLitMat.name = $"{modelName}_URP_Lit";
                    cleanLitMat.color = col;
                    if (cleanLitMat.HasProperty("_BaseColor")) cleanLitMat.SetColor("_BaseColor", col);
                    if (cleanLitMat.HasProperty("_Metallic")) cleanLitMat.SetFloat("_Metallic", metallic);
                    if (cleanLitMat.HasProperty("_Smoothness")) cleanLitMat.SetFloat("_Smoothness", smoothness);

                    if (mainTex != null)
                    {
                        if (cleanLitMat.HasProperty("_BaseMap")) cleanLitMat.SetTexture("_BaseMap", mainTex);
                        cleanLitMat.mainTexture = mainTex;
                    }

                    mats[i] = cleanLitMat;
                }
                rend.materials = mats;
            }
        }

        private RubbleObstacle CreateRubbleObstacle(Transform parent, Vector3 localPos, VictimController pinnedVictim)
        {
            // Use real Pillar model or heavy stone block as the fallen crushing weight
            var rubbleGo = InstantiateModelOrFallback("Pillar", parent, localPos, new Vector3(82f, 35f, -10f), 1.1f);
            rubbleGo.name = "HeavyRubbleObstacle";

            var rb = rubbleGo.GetComponent<Rigidbody>();
            if (rb == null) rb = rubbleGo.AddComponent<Rigidbody>();
            rb.isKinematic = true;

            var rubbleComp = rubbleGo.AddComponent<RubbleObstacle>();
            rubbleComp.BindVictim(pinnedVictim);

            return rubbleComp;
        }

        private GameObject CreateGasLeakStructure(Transform parent, Vector3 localPos)
        {
            var gasRoot = new GameObject("GasLeakHazardZone");
            gasRoot.transform.SetParent(parent, false);
            gasRoot.transform.localPosition = localPos;

            // Real 3D Pipe Model
            var pipe = InstantiateModelOrFallback("Pipe", gasRoot.transform, new Vector3(0, 0.35f, 0), new Vector3(0, 0, 90f), 1.3f);

            // Valve wheel
            var valve = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            valve.name = "IsolationValveWheel";
            valve.transform.SetParent(gasRoot.transform, false);
            valve.transform.localPosition = new Vector3(0, 0.75f, 0);
            valve.transform.localScale = new Vector3(0.4f, 0.06f, 0.4f);
            var vmr = valve.GetComponent<MeshRenderer>();
            var valveMat = new Material(metalMat) { color = new Color(0.95f, 0.15f, 0.15f) };
            if (valveMat.HasProperty("_BaseColor")) valveMat.SetColor("_BaseColor", new Color(0.95f, 0.15f, 0.15f));
            vmr.material = valveMat;

            // Swirling toxic gas particles
            var gasParticles = ProceduralVFXBuilder.CreateGasLeakParticles(gasRoot.transform, new Vector3(0, 0.65f, 0));

            // Hazard component
            var leakComp = gasRoot.AddComponent<GasLeakHazard>();
            SetPrivateField(leakComp, "valveHandle", valve.transform);
            SetPrivateField(leakComp, "gasParticles", gasParticles);

            var collider = gasRoot.AddComponent<BoxCollider>();
            collider.center = new Vector3(0, 0.5f, 0);
            collider.size = new Vector3(1.2f, 1.2f, 1.2f);

            return gasRoot;
        }

        private GameObject CreateFireStructure(Transform parent, Vector3 localPos)
        {
            var fireRoot = new GameObject("StructuralFireHazard");
            fireRoot.transform.SetParent(parent, false);
            fireRoot.transform.localPosition = localPos;

            // Burnt debris base
            var debris = InstantiateModelOrFallback("Box_V1", fireRoot.transform, new Vector3(0, 0.15f, 0), new Vector3(15f, 25f, 0), 0.7f);

            // Fire and smoke particles
            var fireParticles = ProceduralVFXBuilder.CreateFireParticles(fireRoot.transform, new Vector3(0, 0.3f, 0));
            var smokeParticles = ProceduralVFXBuilder.CreateSmokeParticles(fireRoot.transform, new Vector3(0, 0.65f, 0));

            // Dynamic flickering fire light
            var lightGo = new GameObject("FireLight");
            lightGo.transform.SetParent(fireRoot.transform, false);
            lightGo.transform.localPosition = new Vector3(0, 0.7f, 0);
            var pointLight = lightGo.AddComponent<Light>();
            pointLight.type = LightType.Point;
            pointLight.color = new Color(1f, 0.55f, 0.12f);
            pointLight.range = 5.0f;
            pointLight.intensity = 2.5f;
            pointLight.shadows = LightShadows.Soft;

            var fireComp = fireRoot.AddComponent<FireHazard>();
            SetPrivateField(fireComp, "fireParticles", fireParticles);
            SetPrivateField(fireComp, "smokeParticles", smokeParticles);
            SetPrivateField(fireComp, "fireLight", pointLight);

            var collider = fireRoot.AddComponent<BoxCollider>();
            collider.center = new Vector3(0, 0.5f, 0);
            collider.size = new Vector3(1.4f, 1.4f, 1.4f);

            return fireRoot;
        }

        private GameObject CreateEvacuationZoneMarker(Transform parent, Vector3 localPos)
        {
            var evacGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            evacGo.name = "EvacuationLandingZone";
            evacGo.transform.SetParent(parent, false);
            evacGo.transform.localPosition = localPos;
            evacGo.transform.localScale = new Vector3(2.6f, 0.02f, 2.6f);

            Shader litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var mat = new Material(litShader);
            Color evacCol = new Color(0.10f, 0.75f, 0.35f, 0.85f);
            mat.color = evacCol;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", evacCol);
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", evacCol * 0.4f);
            }
            evacGo.GetComponent<MeshRenderer>().material = mat;

            // Green Staging Landing Beacon Light
            var lzLight = evacGo.AddComponent<Light>();
            lzLight.type = LightType.Point;
            lzLight.color = new Color(0.2f, 1f, 0.4f);
            lzLight.range = 3.5f;
            lzLight.intensity = 1.2f;

            return evacGo;
        }

        private VictimController CreateRealisticVictim(Transform parent, Vector3 localPos, string name,
            InjuryType injury, TriageCategory trueCat, float initialHealth, float spO2, int hr,
            bool isPinned = false, bool hasBleeding = false, bool airwayObstructed = false)
        {
            var victimRoot = new GameObject($"Casualty_{name.Replace(' ', '_')}");
            victimRoot.transform.SetParent(parent, false);
            victimRoot.transform.localPosition = localPos;
            victimRoot.transform.localRotation = Quaternion.Euler(0, Random.Range(-30f, 30f), 0);

            // Construct anatomical patient body
            // 1. Torso
            var torso = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            torso.name = "Torso";
            torso.transform.SetParent(victimRoot.transform, false);
            torso.transform.localPosition = new Vector3(0, 0.12f, 0);
            torso.transform.localRotation = Quaternion.Euler(90f, 0, 0);
            torso.transform.localScale = new Vector3(0.38f, 0.42f, 0.25f);
            torso.GetComponent<MeshRenderer>().material = gownMat;

            // 2. Head & Neck
            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(victimRoot.transform, false);
            head.transform.localPosition = new Vector3(0, 0.14f, 0.48f);
            head.transform.localScale = new Vector3(0.22f, 0.24f, 0.22f);
            head.GetComponent<MeshRenderer>().material = skinMat;

            // 3. Legs
            CreateLimb(victimRoot.transform, new Vector3(-0.11f, 0.10f, -0.55f), new Vector3(0.13f, 0.38f, 0.13f), gownMat);
            CreateLimb(victimRoot.transform, new Vector3(0.11f, 0.10f, -0.55f), new Vector3(0.13f, 0.38f, 0.13f), gownMat);

            // 4. Arms
            CreateLimb(victimRoot.transform, new Vector3(-0.25f, 0.10f, 0.05f), new Vector3(0.11f, 0.32f, 0.11f), skinMat);
            CreateLimb(victimRoot.transform, new Vector3(0.25f, 0.10f, 0.05f), new Vector3(0.11f, 0.32f, 0.11f), skinMat);

            // 5. Bandages / Splints based on trauma
            if (injury == InjuryType.Fracture)
            {
                var splint = GameObject.CreatePrimitive(PrimitiveType.Cube);
                splint.name = "FractureSplint";
                splint.transform.SetParent(victimRoot.transform, false);
                splint.transform.localPosition = new Vector3(0.11f, 0.12f, -0.55f);
                splint.transform.localScale = new Vector3(0.16f, 0.05f, 0.42f);
                splint.GetComponent<MeshRenderer>().material = bandageMat;
            }

            // 6. Blood Pool Decal for severe hemorrhage
            if (hasBleeding)
            {
                var bloodPool = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                bloodPool.name = "BloodPoolDecal";
                bloodPool.transform.SetParent(victimRoot.transform, false);
                bloodPool.transform.localPosition = new Vector3(0.20f, 0.01f, 0.15f);
                bloodPool.transform.localScale = new Vector3(0.7f, 0.001f, 0.7f);
                var bpmr = bloodPool.GetComponent<MeshRenderer>();
                bpmr.material = bloodMat;
                var col = bloodPool.GetComponent<Collider>();
                if (col != null) Destroy(col);
            }

            // Add Interaction Collider
            var boxCol = victimRoot.AddComponent<BoxCollider>();
            boxCol.center = new Vector3(0, 0.12f, 0);
            boxCol.size = new Vector3(0.75f, 0.35f, 1.5f);

            var victimComp = victimRoot.AddComponent<VictimController>();
            victimRoot.AddComponent<VictimChestBreathing>();
            var vitals = victimComp.Vitals;
            vitals.victimName = name;
            vitals.primaryInjury = injury;
            vitals.trueCorrectCategory = trueCat;
            vitals.healthPercentage = initialHealth;
            vitals.bloodOxygen = spO2;
            vitals.heartRate = hr;
            vitals.isPinnedUnderRubble = isPinned;
            vitals.hasActiveBleeding = hasBleeding;
            vitals.isAirwayObstructed = airwayObstructed;

            // Create floating world space UI for vitals
            CreateWorldSpaceVitalsUI(victimRoot, victimComp);

            IncidentCommandSystem.Instance?.RegisterVictim(victimComp);

            return victimComp;
        }

        private GameObject CreateLimb(Transform parent, Vector3 localPos, Vector3 scale, Material mat)
        {
            var limb = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            limb.transform.SetParent(parent, false);
            limb.transform.localPosition = localPos;
            limb.transform.localRotation = Quaternion.Euler(90f, 0, 0);
            limb.transform.localScale = scale;
            limb.GetComponent<MeshRenderer>().material = mat;
            return limb;
        }

        private void CreateWorldSpaceVitalsUI(GameObject victimGo, VictimController victimComp)
        {
            var uiGo = new GameObject("WorldSpaceVitalsCanvas");
            uiGo.transform.SetParent(victimGo.transform, false);
            uiGo.transform.localPosition = new Vector3(0, 0.65f, 0); // Floats above patient
            uiGo.transform.localScale = Vector3.one * 0.005f;

            var canvas = uiGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            uiGo.AddComponent<CanvasScaler>();

            var vitalsUI = uiGo.AddComponent<WorldSpaceVitalsUI>();

            // Background panel with holographic border
            var panelGo = new GameObject("Panel");
            panelGo.transform.SetParent(uiGo.transform, false);
            var rect = panelGo.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(280, 195);
            var img = panelGo.AddComponent<Image>();
            img.color = new Color(0.06f, 0.09f, 0.14f, 0.92f);

            // Name Text
            var nameGo = new GameObject("VictimNameText");
            nameGo.transform.SetParent(panelGo.transform, false);
            var nameTmp = nameGo.AddComponent<TextMeshProUGUI>();
            nameTmp.rectTransform.anchoredPosition = new Vector2(0, 52);
            nameTmp.rectTransform.sizeDelta = new Vector2(260, 30);
            nameTmp.fontSize = 18;
            nameTmp.alignment = TextAlignmentOptions.Center;
            nameTmp.fontStyle = FontStyles.Bold;
            nameTmp.color = Color.white;

            // Vitals Text
            var vitalsTextGo = new GameObject("VitalsText");
            vitalsTextGo.transform.SetParent(panelGo.transform, false);
            var vitalsTmp = vitalsTextGo.AddComponent<TextMeshProUGUI>();
            vitalsTmp.rectTransform.anchoredPosition = new Vector2(0, 22);
            vitalsTmp.rectTransform.sizeDelta = new Vector2(260, 26);
            vitalsTmp.fontSize = 14;
            vitalsTmp.alignment = TextAlignmentOptions.Center;
            vitalsTmp.color = new Color(0.25f, 0.95f, 1f);

            // Status Text
            var statusGo = new GameObject("StatusText");
            statusGo.transform.SetParent(panelGo.transform, false);
            var statusTmp = statusGo.AddComponent<TextMeshProUGUI>();
            statusTmp.rectTransform.anchoredPosition = new Vector2(0, -8);
            statusTmp.rectTransform.sizeDelta = new Vector2(260, 26);
            statusTmp.fontSize = 13;
            statusTmp.alignment = TextAlignmentOptions.Center;
            statusTmp.color = new Color(1f, 0.85f, 0.2f);

            // Triage Badge
            var badgeGo = new GameObject("TriageBadge");
            badgeGo.transform.SetParent(panelGo.transform, false);
            var badgeRect = badgeGo.AddComponent<RectTransform>();
            badgeRect.anchoredPosition = new Vector2(0, -45);
            badgeRect.sizeDelta = new Vector2(240, 30);
            var badgeImg = badgeGo.AddComponent<Image>();
            badgeImg.color = new Color(0.3f, 0.35f, 0.45f, 0.8f);

            var badgeTextGo = new GameObject("BadgeText");
            badgeTextGo.transform.SetParent(badgeGo.transform, false);
            var badgeTmp = badgeTextGo.AddComponent<TextMeshProUGUI>();
            badgeTmp.rectTransform.sizeDelta = new Vector2(240, 30);
            badgeTmp.fontSize = 14;
            badgeTmp.alignment = TextAlignmentOptions.Center;
            badgeTmp.fontStyle = FontStyles.Bold;
            badgeTmp.color = Color.white;

            // Health Bar
            var healthBarGo = new GameObject("HealthBar");
            healthBarGo.transform.SetParent(panelGo.transform, false);
            var hbRect = healthBarGo.AddComponent<RectTransform>();
            hbRect.anchoredPosition = new Vector2(0, -66);
            hbRect.sizeDelta = new Vector2(240, 6);
            var hbBg = healthBarGo.AddComponent<Image>();
            hbBg.color = new Color(0.15f, 0.15f, 0.15f, 0.7f);

            var hbFillGo = new GameObject("HealthFill");
            hbFillGo.transform.SetParent(healthBarGo.transform, false);
            var hbFillRect = hbFillGo.AddComponent<RectTransform>();
            hbFillRect.anchorMin = Vector2.zero;
            hbFillRect.anchorMax = Vector2.one;
            hbFillRect.sizeDelta = Vector2.zero;
            var hbFillImg = hbFillGo.AddComponent<Image>();
            hbFillImg.color = new Color(0.2f, 0.85f, 0.35f, 0.9f);

            var healthSlider = healthBarGo.AddComponent<Slider>();
            healthSlider.fillRect = hbFillRect;
            healthSlider.minValue = 0f;
            healthSlider.maxValue = 1f;
            healthSlider.value = 1f;

            // Stabilization Progress Bar (Fills up when applying MedKit treatment!)
            var stabBarGo = new GameObject("StabilizationBar");
            stabBarGo.transform.SetParent(panelGo.transform, false);
            var sbRect = stabBarGo.AddComponent<RectTransform>();
            sbRect.anchoredPosition = new Vector2(0, -76);
            sbRect.sizeDelta = new Vector2(240, 8);
            var sbBg = stabBarGo.AddComponent<Image>();
            sbBg.color = new Color(0.12f, 0.18f, 0.24f, 0.85f);

            var sbFillGo = new GameObject("StabFill");
            sbFillGo.transform.SetParent(stabBarGo.transform, false);
            var sbFillRect = sbFillGo.AddComponent<RectTransform>();
            sbFillRect.anchorMin = Vector2.zero;
            sbFillRect.anchorMax = Vector2.one;
            sbFillRect.sizeDelta = Vector2.zero;
            var sbFillImg = sbFillGo.AddComponent<Image>();
            sbFillImg.color = new Color(0.1f, 0.75f, 1f, 0.95f); // High-tech cyan fill

            var stabSlider = stabBarGo.AddComponent<Slider>();
            stabSlider.fillRect = sbFillRect;
            stabSlider.minValue = 0f;
            stabSlider.maxValue = 1f;
            stabSlider.value = 0f;

            // Bind references via reflection
            SetPrivateField(vitalsUI, "canvas", canvas);
            SetPrivateField(vitalsUI, "victimNameText", nameTmp);
            SetPrivateField(vitalsUI, "vitalsText", vitalsTmp);
            SetPrivateField(vitalsUI, "statusText", statusTmp);
            SetPrivateField(vitalsUI, "triageBadge", badgeImg);
            SetPrivateField(vitalsUI, "triageBadgeText", badgeTmp);
            SetPrivateField(vitalsUI, "healthSlider", healthSlider);
            SetPrivateField(vitalsUI, "stabilizationSlider", stabSlider);

            vitalsUI.BindVictim(victimComp);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null) field.SetValue(target, value);
        }

        #endregion
    }
}

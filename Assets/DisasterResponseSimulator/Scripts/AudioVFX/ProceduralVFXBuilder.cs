using UnityEngine;

namespace DisasterResponse.AudioVFX
{
    public static class ProceduralVFXBuilder
    {
        private static Material particleAlphaMaterial;
        private static Material particleAdditiveMaterial;
        private static Texture2D softCircleTexture;
        private static Texture2D ringTexture;

        private static Texture2D GetSoftCircleTexture()
        {
            if (softCircleTexture == null)
            {
                int size = 64;
                softCircleTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
                softCircleTexture.name = "VFX_SoftCircle_Procedural";
                Color[] pixels = new Color[size * size];
                float center = (size - 1) * 0.5f;

                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center)) / center;
                        float alpha = Mathf.Clamp01(1f - dist);
                        // Smooth Hermite interpolation for soft organic falloff
                        alpha = alpha * alpha * (3f - 2f * alpha);
                        pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                    }
                }

                softCircleTexture.SetPixels(pixels);
                softCircleTexture.Apply(false);
            }
            return softCircleTexture;
        }

        private static Texture2D GetRingTexture()
        {
            if (ringTexture == null)
            {
                int size = 128;
                ringTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
                ringTexture.name = "VFX_SonarRing_Procedural";
                Color[] pixels = new Color[size * size];
                float center = (size - 1) * 0.5f;

                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center)) / center;
                        // Sharp ring at radius 0.85 to 0.95 with smooth inner/outer falloff
                        float ringVal = 1f - Mathf.Abs(dist - 0.90f) / 0.10f;
                        float alpha = Mathf.Clamp01(ringVal);
                        alpha = alpha * alpha;
                        pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                    }
                }

                ringTexture.SetPixels(pixels);
                ringTexture.Apply(false);
            }
            return ringTexture;
        }

        private static Material GetAlphaParticleMaterial()
        {
            if (particleAlphaMaterial == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit") 
                             ?? Shader.Find("Particles/Standard Unlit") 
                             ?? Shader.Find("Sprites/Default");

                particleAlphaMaterial = new Material(shader);
                particleAlphaMaterial.name = "URP_VFX_AlphaBlended";
                particleAlphaMaterial.SetFloat("_Surface", 1); // Transparent
                particleAlphaMaterial.SetFloat("_Blend", 0);   // Alpha
                particleAlphaMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                particleAlphaMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                particleAlphaMaterial.SetInt("_ZWrite", 0);
                particleAlphaMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                
                var tex = GetSoftCircleTexture();
                if (particleAlphaMaterial.HasProperty("_BaseMap")) particleAlphaMaterial.SetTexture("_BaseMap", tex);
                particleAlphaMaterial.mainTexture = tex;
            }
            return particleAlphaMaterial;
        }

        private static Material GetAdditiveParticleMaterial(bool isRing = false)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit") 
                         ?? Shader.Find("Particles/Standard Unlit") 
                         ?? Shader.Find("Mobile/Particles/Additive")
                         ?? Shader.Find("Sprites/Default");

            var mat = new Material(shader);
            mat.name = isRing ? "URP_VFX_Additive_Ring" : "URP_VFX_Additive_Glow";
            mat.SetFloat("_Surface", 1); // Transparent
            mat.SetFloat("_Blend", 1);   // Additive
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
            mat.SetInt("_ZWrite", 0);
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

            var tex = isRing ? GetRingTexture() : GetSoftCircleTexture();
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
            mat.mainTexture = tex;
            return mat;
        }

        public static ParticleSystem CreateFireParticles(Transform parent, Vector3 localPos)
        {
            var go = new GameObject("FireCore_VFX");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;

            var ps = go.AddComponent<ParticleSystem>();
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.material = GetAdditiveParticleMaterial(false);
            renderer.renderMode = ParticleSystemRenderMode.Billboard;

            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.9f, 1.4f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 2.0f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.45f, 0.85f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            // HDR vibrant flame colors (triggers post-processing bloom)
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(2.0f, 1.4f, 0.4f, 1.0f), 
                new Color(2.0f, 0.4f, 0.05f, 0.85f));

            var emission = ps.emission;
            emission.rateOverTime = 42f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 12f;
            shape.radius = 0.22f;

            // Organic size curve: grows then dissipates
            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            AnimationCurve curve = new AnimationCurve();
            curve.AddKey(0.0f, 0.35f);
            curve.AddKey(0.35f, 1.0f);
            curve.AddKey(1.0f, 0.02f);
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1.0f, curve);

            // Thermal turbulence noise module
            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.45f;
            noise.frequency = 0.85f;
            noise.scrollSpeed = 1.2f;

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] { 
                    new GradientColorKey(new Color(2.2f, 1.8f, 0.8f), 0.0f), 
                    new GradientColorKey(new Color(2.0f, 0.6f, 0.1f), 0.4f), 
                    new GradientColorKey(new Color(0.8f, 0.1f, 0.02f), 0.8f),
                    new GradientColorKey(new Color(0.2f, 0.05f, 0.02f), 1.0f) 
                },
                new GradientAlphaKey[] { 
                    new GradientAlphaKey(0.95f, 0.0f), 
                    new GradientAlphaKey(0.85f, 0.5f), 
                    new GradientAlphaKey(0.0f, 1.0f) 
                });
            colorOverLifetime.color = grad;

            // Floating luminous embers
            CreateFireEmbers(go.transform, Vector3.zero);

            return ps;
        }

        public static ParticleSystem CreateFireEmbers(Transform parent, Vector3 localPos)
        {
            var go = new GameObject("FireEmbers_VFX");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;

            var ps = go.AddComponent<ParticleSystem>();
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.material = GetAdditiveParticleMaterial(false);

            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.6f, 3.0f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 2.8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.10f);
            main.startColor = new Color(2.5f, 1.8f, 0.4f, 1.0f); // High HDR intensity for bloom
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = 25f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 26f;
            shape.radius = 0.30f;

            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.65f;
            noise.frequency = 1.1f;
            noise.scrollSpeed = 1.4f;

            return ps;
        }

        public static ParticleSystem CreateSmokeParticles(Transform parent, Vector3 localPos)
        {
            var go = new GameObject("SmokeColumn_VFX");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;

            var ps = go.AddComponent<ParticleSystem>();
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.material = GetAlphaParticleMaterial();

            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(2.8f, 4.2f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.7f, 1.3f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.6f, 1.3f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
            main.startColor = new Color(0.14f, 0.14f, 0.16f, 0.60f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = 16f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 22f;
            shape.radius = 0.28f;

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            AnimationCurve curve = new AnimationCurve();
            curve.AddKey(0.0f, 0.45f);
            curve.AddKey(1.0f, 2.8f); // Expands heavily into the air
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1.0f, curve);

            var rotationOverLifetime = ps.rotationOverLifetime;
            rotationOverLifetime.enabled = true;
            rotationOverLifetime.z = new ParticleSystem.MinMaxCurve(-45f * Mathf.Deg2Rad, 45f * Mathf.Deg2Rad);

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(new Color(0.18f, 0.18f, 0.18f), 0.0f), new GradientColorKey(new Color(0.28f, 0.28f, 0.30f), 1.0f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0.0f, 0.0f), new GradientAlphaKey(0.55f, 0.25f), new GradientAlphaKey(0.0f, 1.0f) });
            colorOverLifetime.color = grad;

            return ps;
        }

        public static ParticleSystem CreateGasLeakParticles(Transform parent, Vector3 localPos)
        {
            var go = new GameObject("ToxicGasLeak_VFX");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;

            var ps = go.AddComponent<ParticleSystem>();
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.material = GetAlphaParticleMaterial();

            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 1.8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(3.2f, 5.0f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.25f, 0.55f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
            main.startColor = new Color(0.68f, 0.95f, 0.25f, 0.65f); // Toxic sulfur aerosol
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = 60f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 8f;
            shape.radius = 0.05f;

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            AnimationCurve curve = new AnimationCurve();
            curve.AddKey(0.0f, 0.2f);
            curve.AddKey(1.0f, 1.8f);
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1.0f, curve);

            var rotationOverLifetime = ps.rotationOverLifetime;
            rotationOverLifetime.enabled = true;
            rotationOverLifetime.z = new ParticleSystem.MinMaxCurve(-60f * Mathf.Deg2Rad, 60f * Mathf.Deg2Rad);

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(new Color(0.7f, 1f, 0.2f), 0.0f), new GradientColorKey(new Color(0.4f, 0.7f, 0.1f), 1.0f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0.7f, 0.0f), new GradientAlphaKey(0.5f, 0.6f), new GradientAlphaKey(0.0f, 1.0f) });
            colorOverLifetime.color = grad;

            return ps;
        }

        public static ParticleSystem CreateFoamStreamParticles(Transform parent)
        {
            var go = new GameObject("ExtinguisherFoam_VFX");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = Vector3.forward * 0.35f;

            var ps = go.AddComponent<ParticleSystem>();
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.material = GetAlphaParticleMaterial();

            var main = ps.main;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 0.9f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(8.5f, 12.0f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.18f, 0.42f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
            main.startColor = new Color(0.96f, 0.98f, 1.0f, 0.90f);

            var emission = ps.emission;
            emission.rateOverTime = 110f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 6f;
            shape.radius = 0.05f;

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            AnimationCurve curve = new AnimationCurve();
            curve.AddKey(0.0f, 0.25f);
            curve.AddKey(1.0f, 1.4f); // Expands into thick blanket
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1.0f, curve);

            ps.Stop();
            return ps;
        }

        public static ParticleSystem CreateDustParticles(Transform parent, Vector3 localPos)
        {
            var go = new GameObject("AtmosphericDust_VFX");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;

            var ps = go.AddComponent<ParticleSystem>();
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.material = GetAlphaParticleMaterial();

            var main = ps.main;
            main.playOnAwake = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(3.0f, 5.0f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.22f);
            main.startColor = new Color(0.72f, 0.68f, 0.62f, 0.35f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = 20f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(8f, 3f, 8f);

            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.3f;
            noise.frequency = 0.5f;

            return ps;
        }

        public static void CreateSonarPingEffect(Transform parent, Vector3 worldOrigin)
        {
            var pingGo = new GameObject("AR_SonarPing_Wave_FX");
            pingGo.transform.position = worldOrigin + Vector3.up * 0.04f;

            var ps = pingGo.AddComponent<ParticleSystem>();
            var renderer = pingGo.GetComponent<ParticleSystemRenderer>();
            renderer.material = GetAdditiveParticleMaterial(true); // Concentric ring texture

            var main = ps.main;
            main.startLifetime = 1.4f;
            main.startSpeed = 0f;
            main.startSize = 0.2f;
            main.startColor = new Color(0.2f, 2.0f, 1.6f, 0.95f); // High-tech cyan glow
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0.0f, 1) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.02f;
            shape.rotation = new Vector3(90f, 0, 0); // Flat on the floor

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            AnimationCurve curve = new AnimationCurve();
            curve.AddKey(0.0f, 0.1f);
            curve.AddKey(1.0f, 14.0f); // Expands up to 14m across room
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1.0f, curve);

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(new Color(0.2f, 2.0f, 1.6f), 0.0f), new GradientColorKey(new Color(0.05f, 0.8f, 1.0f), 0.7f), new GradientColorKey(new Color(0.01f, 0.2f, 0.5f), 1.0f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0.95f, 0.0f), new GradientAlphaKey(0.70f, 0.5f), new GradientAlphaKey(0.0f, 1.0f) });
            colorOverLifetime.color = grad;

            ps.Play();
            Object.Destroy(pingGo, 2.2f);
        }
    }
}

using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class MainMenuSceneBuilder
{
    [MenuItem("Tools/Rebuild 3D Main Menu Environment")]
    public static void BuildMenuEnvironment()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity", OpenSceneMode.Single);
        SceneManager.SetActiveScene(scene);

        Debug.Log("<color=cyan><b>=== Building 3D Main Menu Environment ===</b></color>");

        // 1. Setup Camera
        Camera cam = null;
        var camGO = GameObject.Find("Main Camera");
        if (camGO == null)
        {
            camGO = new GameObject("Main Camera");
            cam = camGO.AddComponent<Camera>();
        }
        else
        {
            cam = camGO.GetComponent<Camera>();
            if (cam == null) cam = camGO.AddComponent<Camera>();
        }

        camGO.tag = "MainCamera";
        cam.clearFlags = CameraClearFlags.Skybox;
        cam.fieldOfView = 60f;
        cam.nearClipPlane = 0.3f;
        cam.farClipPlane = 1000f;
        cam.transform.position = new Vector3(0f, 2.3f, -5.8f);
        cam.transform.rotation = Quaternion.Euler(11f, 0f, 0f);

        // Add or update smooth camera sway script
        var sway = cam.GetComponent<MainMenuCameraPan>();
        if (sway == null) sway = cam.gameObject.AddComponent<MainMenuCameraPan>();
        sway.lookTarget = new Vector3(0f, 1.3f, 0.8f);
        sway.swaySpeed = 0.2f;
        sway.swayAmountX = 0.8f;
        sway.swayAmountY = 0.25f;

        // Ensure AudioListener on Camera
        var listener = cam.GetComponent<AudioListener>();
        if (listener == null) cam.gameObject.AddComponent<AudioListener>();

        // 2. Setup Directional Light (Sun)
        Light sun = null;
        foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
        {
            if (l.type == LightType.Directional) { sun = l; break; }
        }
        if (sun == null)
        {
            GameObject sunGO = new GameObject("Directional Light");
            sun = sunGO.AddComponent<Light>();
            sun.type = LightType.Directional;
        }
        sun.transform.position = new Vector3(0f, 15f, 0f);
        sun.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
        sun.color = new Color(1f, 0.96f, 0.88f);
        sun.intensity = 1.3f;
        sun.shadows = LightShadows.Soft;

        // Setup Skybox and Ambient Lighting
        Material skyMat = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Skybox.mat");
        if (skyMat != null) RenderSettings.skybox = skyMat;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Skybox;
        RenderSettings.ambientIntensity = 1.1f;

        // 3. Remove existing Environment_3D if rebuilding
        GameObject oldEnv = GameObject.Find("Environment_3D");
        if (oldEnv != null) Object.DestroyImmediate(oldEnv);

        GameObject envRoot = new GameObject("Environment_3D");

        // 4. Ground Plane
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = "Ground";
        ground.transform.SetParent(envRoot.transform, false);
        ground.transform.position = new Vector3(0f, -0.25f, 3f);
        ground.transform.localScale = new Vector3(45f, 0.5f, 45f);

        // Ground Material
        Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
        if (litShader == null) litShader = Shader.Find("Standard");
        Material grassMat = new Material(litShader);
        grassMat.name = "Menu_Grass_Mat";
        Texture2D grassTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/grassy_texture.png");
        if (grassTex != null)
        {
            grassMat.mainTexture = grassTex;
            grassMat.mainTextureScale = new Vector2(16f, 16f);
        }
        grassMat.color = new Color(0.42f, 0.65f, 0.32f);
        if (grassMat.HasProperty("_BaseColor")) grassMat.SetColor("_BaseColor", new Color(0.42f, 0.65f, 0.32f));
        ground.GetComponent<MeshRenderer>().sharedMaterial = grassMat;

        // 5. Centerpiece Dais
        Material stoneMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Rocks and Vegetation Pack/Materials/Rock.mat");
        if (stoneMat == null) stoneMat = grassMat;

        GameObject dais1 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        dais1.name = "Dais_Tier1";
        dais1.transform.SetParent(envRoot.transform, false);
        dais1.transform.position = new Vector3(0f, 0.15f, 0.8f);
        dais1.transform.localScale = new Vector3(3.2f, 0.3f, 3.2f);
        dais1.GetComponent<MeshRenderer>().sharedMaterial = stoneMat;

        GameObject dais2 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        dais2.name = "Dais_Tier2";
        dais2.transform.SetParent(envRoot.transform, false);
        dais2.transform.position = new Vector3(0f, 0.45f, 0.8f);
        dais2.transform.localScale = new Vector3(2.4f, 0.3f, 2.4f);
        dais2.GetComponent<MeshRenderer>().sharedMaterial = stoneMat;

        // Centerpiece Gold Model (from FoodLowPolyBigPack_01 coin model or chest)
        GameObject coinFBX = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Props/coin/FoodLowPolyBigPack_01.fbx");
        Material goldMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Chest_Gold_URP.mat");
        if (coinFBX != null)
        {
            GameObject showcaseCoin = Object.Instantiate(coinFBX, envRoot.transform);
            showcaseCoin.name = "Showcase_CenterCoin";
            showcaseCoin.transform.position = new Vector3(0f, 1.25f, 0.8f);
            showcaseCoin.transform.localScale = new Vector3(3.5f, 3.5f, 3.5f);
            if (goldMat != null)
            {
                foreach (var r in showcaseCoin.GetComponentsInChildren<MeshRenderer>()) r.sharedMaterial = goldMat;
            }
            var fc = showcaseCoin.AddComponent<FloatingCoin>();
            fc.rotationSpeed = 60f;
            fc.bobAmplitude = 0.15f;
            fc.bobFrequency = 1.8f;

            // Small orbiting coins
            for (int i = 0; i < 4; i++)
            {
                float angle = i * Mathf.PI * 0.5f;
                float ox = Mathf.Cos(angle) * 1.1f;
                float oz = Mathf.Sin(angle) * 1.1f;

                GameObject orbCoin = Object.Instantiate(coinFBX, envRoot.transform);
                orbCoin.name = $"OrbitCoin_{i}";
                orbCoin.transform.position = new Vector3(ox, 0.85f + (i % 2) * 0.15f, 0.8f + oz);
                orbCoin.transform.localScale = new Vector3(1.8f, 1.8f, 1.8f);
                if (goldMat != null)
                {
                    foreach (var r in orbCoin.GetComponentsInChildren<MeshRenderer>()) r.sharedMaterial = goldMat;
                }
                var ofc = orbCoin.AddComponent<FloatingCoin>();
                ofc.rotationSpeed = 90f + i * 20f;
                ofc.bobAmplitude = 0.1f;
                ofc.bobFrequency = 2.2f + i * 0.3f;
            }
        }

        // 6. Spawn Nature Props (Trees, Rocks, Bushes, Ferns)
        GameObject tree1Prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Rocks and Vegetation Pack/Prefabs/Maple Tree 1.prefab");
        GameObject tree2Prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Rocks and Vegetation Pack/Prefabs/Maple Tree 2.prefab");
        GameObject rock1Prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Rocks and Vegetation Pack/Prefabs/Rock 1.prefab");
        GameObject rock2Prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Rocks and Vegetation Pack/Prefabs/Rock 2.prefab");
        GameObject rock3Prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Rocks and Vegetation Pack/Prefabs/Rock 3.prefab");
        GameObject bushPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Rocks and Vegetation Pack/Prefabs/Bush.prefab");
        GameObject fernPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Rocks and Vegetation Pack/Prefabs/Fern.prefab");

        // Trees
        SpawnProp(tree1Prefab, envRoot.transform, new Vector3(-5.2f, 0f, 4.5f), Quaternion.Euler(0f, 25f, 0f), 1.1f);
        SpawnProp(tree1Prefab, envRoot.transform, new Vector3(5.6f, 0f, 5.0f), Quaternion.Euler(0f, -40f, 0f), 1.05f);
        SpawnProp(tree2Prefab ?? tree1Prefab, envRoot.transform, new Vector3(-8.5f, 0f, 8.0f), Quaternion.Euler(0f, 60f, 0f), 1.3f);
        SpawnProp(tree2Prefab ?? tree1Prefab, envRoot.transform, new Vector3(8.0f, 0f, 8.5f), Quaternion.Euler(0f, -80f, 0f), 1.25f);
        SpawnProp(tree1Prefab, envRoot.transform, new Vector3(-2.5f, 0f, 9.5f), Quaternion.Euler(0f, 15f, 0f), 1.2f);
        SpawnProp(tree1Prefab, envRoot.transform, new Vector3(2.8f, 0f, 10.0f), Quaternion.Euler(0f, -30f, 0f), 1.15f);

        // Rocks
        SpawnProp(rock1Prefab, envRoot.transform, new Vector3(-3.2f, 0f, -0.6f), Quaternion.Euler(0f, 45f, 0f), 0.9f);
        SpawnProp(rock2Prefab ?? rock1Prefab, envRoot.transform, new Vector3(3.3f, 0f, -0.2f), Quaternion.Euler(0f, -60f, 0f), 0.85f);
        SpawnProp(rock3Prefab ?? rock1Prefab, envRoot.transform, new Vector3(-4.6f, 0f, 2.2f), Quaternion.Euler(0f, 110f, 0f), 1.05f);
        SpawnProp(rock1Prefab, envRoot.transform, new Vector3(4.8f, 0f, 2.8f), Quaternion.Euler(0f, -135f, 0f), 1.1f);

        // Bushes & Ferns
        SpawnProp(bushPrefab, envRoot.transform, new Vector3(-2.5f, 0f, -1.2f), Quaternion.Euler(0f, 10f, 0f), 0.9f);
        SpawnProp(bushPrefab, envRoot.transform, new Vector3(2.7f, 0f, -0.9f), Quaternion.Euler(0f, 40f, 0f), 0.95f);
        SpawnProp(bushPrefab, envRoot.transform, new Vector3(-3.8f, 0f, 1.2f), Quaternion.Euler(0f, -70f, 0f), 1.0f);
        SpawnProp(bushPrefab, envRoot.transform, new Vector3(4.1f, 0f, 1.6f), Quaternion.Euler(0f, 85f, 0f), 1.0f);
        SpawnProp(fernPrefab, envRoot.transform, new Vector3(-1.9f, 0f, -0.5f), Quaternion.identity, 0.85f);
        SpawnProp(fernPrefab, envRoot.transform, new Vector3(2.0f, 0f, -0.3f), Quaternion.identity, 0.9f);

        // 7. Golden Sparkle Particle Effect
        GameObject fxGO = new GameObject("GoldenSparkles");
        fxGO.transform.SetParent(envRoot.transform, false);
        fxGO.transform.position = new Vector3(0f, 0.6f, 0.8f);

        var ps = fxGO.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime = 3.5f;
        main.startSpeed = 0.5f;
        main.startSize = 0.12f;
        main.startColor = new Color(1f, 0.9f, 0.4f, 0.8f);
        main.maxParticles = 50;

        var emission = ps.emission;
        emission.rateOverTime = 12f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 1.8f;

        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.x = new ParticleSystem.MinMaxCurve(0f, 0f);
        vel.y = new ParticleSystem.MinMaxCurve(0.4f, 0.8f);
        vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);

        var psRenderer = fxGO.GetComponent<ParticleSystemRenderer>();
        if (psRenderer != null)
        {
            var pMat = AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.render-pipelines.universal/Runtime/Materials/ParticlesUnlit.mat");
            if (pMat != null) psRenderer.sharedMaterial = pMat;
        }

        // 8. Rebuild & Polish UI Canvas
        SetupPolishedUI();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("<color=green><b>=== 3D Main Menu Environment Built & Saved Successfully! ===</b></color>");
    }

    private static void SpawnProp(GameObject prefab, Transform parent, Vector3 pos, Quaternion rot, float scale)
    {
        if (prefab == null) return;
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        if (instance == null) instance = Object.Instantiate(prefab, parent);
        instance.transform.position = pos;
        instance.transform.rotation = rot;
        instance.transform.localScale = Vector3.one * scale;
    }

    private static void SetupPolishedUI()
    {
        Canvas canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null) return;

        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = mainCam;
            canvas.planeDistance = 2.5f;
        }

        // Setup EventSystem with InputSystemUIInputModule
        var esGO = GameObject.Find("EventSystem");
        if (esGO == null) esGO = new GameObject("EventSystem");
        var es = esGO.GetComponent<UnityEngine.EventSystems.EventSystem>();
        if (es == null) es = esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();

#if ENABLE_INPUT_SYSTEM
        var standalone = esGO.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        if (standalone != null) Object.DestroyImmediate(standalone);

        var inputModule = esGO.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        if (inputModule == null)
        {
            inputModule = esGO.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            inputModule.AssignDefaultActions();
        }
#endif

        // Ensure CanvasScaler is ScaleWithScreenSize 1920x1080
        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null) scaler = canvas.gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        // Ensure MainMenuManager is on Canvas
        var menuMgr = canvas.GetComponent<MainMenuManager>();
        if (menuMgr == null) menuMgr = canvas.gameObject.AddComponent<MainMenuManager>();

        // Style Title
        Transform titleTrans = canvas.transform.Find("Title");
        if (titleTrans != null)
        {
            RectTransform trt = titleTrans.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0.5f, 0.5f);
            trt.anchorMax = new Vector2(0.5f, 0.5f);
            trt.pivot = new Vector2(0.5f, 0.5f);
            trt.anchoredPosition = new Vector2(0f, 230f);
            trt.sizeDelta = new Vector2(1000f, 130f);

            Text tText = titleTrans.GetComponent<Text>();
            if (tText != null)
            {
                tText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (tText.font == null) tText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                tText.fontSize = 86;
                tText.alignment = TextAnchor.MiddleCenter;
                tText.color = new Color(1f, 0.88f, 0.28f, 1f); // Rich gold
                tText.supportRichText = true;
                tText.text = "<b>COIN CHASE 3D</b>\n<size=28><color=#E8EFFF>◆  THE LABYRINTH ESCAPE  ◆</color></size>";
            }

            // Add subtle shadow component
            var shadow = titleTrans.GetComponent<Shadow>();
            if (shadow == null) shadow = titleTrans.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
            shadow.effectDistance = new Vector2(3f, -3f);
        }

        // Find or create MenuCard panel
        Transform menuCardTrans = canvas.transform.Find("MenuCard");
        GameObject menuCard;
        if (menuCardTrans == null)
        {
            menuCard = new GameObject("MenuCard");
            menuCard.transform.SetParent(canvas.transform, false);
        }
        else
        {
            menuCard = menuCardTrans.gameObject;
        }

        RectTransform cardRT = menuCard.GetComponent<RectTransform>();
        if (cardRT == null) cardRT = menuCard.AddComponent<RectTransform>();
        cardRT.anchorMin = new Vector2(0.5f, 0.5f);
        cardRT.anchorMax = new Vector2(0.5f, 0.5f);
        cardRT.pivot = new Vector2(0.5f, 0.5f);
        cardRT.anchoredPosition = new Vector2(0f, 0f);
        cardRT.sizeDelta = new Vector2(380f, 320f);

        Image cardImg = menuCard.GetComponent<Image>();
        if (cardImg == null) cardImg = menuCard.AddComponent<Image>();
        cardImg.color = new Color(0.05f, 0.08f, 0.12f, 0.82f); // Sleek dark slate glass

        var cardOutline = menuCard.GetComponent<Outline>();
        if (cardOutline == null) cardOutline = menuCard.AddComponent<Outline>();
        cardOutline.effectColor = new Color(1f, 0.85f, 0.35f, 0.45f); // Soft golden border
        cardOutline.effectDistance = new Vector2(1.5f, -1.5f);

        // Reparent and style PlayButton
        Transform playTrans = canvas.transform.Find("PlayButton");
        if (playTrans == null && menuCardTrans != null) playTrans = menuCardTrans.Find("PlayButton");
        if (playTrans != null)
        {
            playTrans.SetParent(menuCard.transform, false);
            RectTransform prt = playTrans.GetComponent<RectTransform>();
            prt.anchorMin = new Vector2(0.5f, 0.5f);
            prt.anchorMax = new Vector2(0.5f, 0.5f);
            prt.pivot = new Vector2(0.5f, 0.5f);
            prt.anchoredPosition = new Vector2(0f, 92f);
            prt.sizeDelta = new Vector2(300f, 52f);
            prt.sizeDelta = new Vector2(300f, 58f);

            Image pImg = playTrans.GetComponent<Image>();
            if (pImg != null) pImg.color = new Color(0.14f, 0.68f, 0.32f, 1f); // Vibrant emerald green

            Button pBtn = playTrans.GetComponent<Button>();
            if (pBtn != null)
            {
                ColorBlock cb = pBtn.colors;
                cb.normalColor = new Color(0.14f, 0.68f, 0.32f, 1f);
                cb.highlightedColor = new Color(0.22f, 0.84f, 0.42f, 1f);
                cb.pressedColor = new Color(0.10f, 0.52f, 0.24f, 1f);
                pBtn.colors = cb;
            }

            Text pText = playTrans.GetComponentInChildren<Text>();
            if (pText != null)
            {
                pText.text = "<b>▶   PLAY GAME</b>";
                pText.fontSize = 26;
                pText.alignment = TextAnchor.MiddleCenter;
                pText.color = Color.white;
            }
        }

        // Setup SettingsButton
        Transform settingsTrans = menuCard.transform.Find("SettingsButton");
        GameObject settingsBtnGO;
        if (settingsTrans == null)
        {
            settingsBtnGO = new GameObject("SettingsButton");
            settingsBtnGO.transform.SetParent(menuCard.transform, false);
            settingsBtnGO.AddComponent<RectTransform>();
            settingsBtnGO.AddComponent<CanvasRenderer>();
            settingsBtnGO.AddComponent<Image>();
            settingsBtnGO.AddComponent<Button>();
            settingsTrans = settingsBtnGO.transform;
        }
        else
        {
            settingsBtnGO = settingsTrans.gameObject;
        }

        RectTransform srt = settingsTrans.GetComponent<RectTransform>();
        srt.anchorMin = new Vector2(0.5f, 0.5f);
        srt.anchorMax = new Vector2(0.5f, 0.5f);
        srt.pivot = new Vector2(0.5f, 0.5f);
        srt.anchoredPosition = new Vector2(0f, 32f);
        srt.sizeDelta = new Vector2(300f, 48f);

        Image sImg = settingsTrans.GetComponent<Image>();
        sImg.color = new Color(0.14f, 0.28f, 0.45f, 0.95f);

        Button sBtn = settingsTrans.GetComponent<Button>();
        ColorBlock scb = sBtn.colors;
        scb.normalColor = new Color(0.14f, 0.28f, 0.45f, 0.95f);
        scb.highlightedColor = new Color(0.20f, 0.40f, 0.65f, 1f);
        scb.pressedColor = new Color(0.10f, 0.20f, 0.32f, 1f);
        sBtn.colors = scb;

        var sOutline = settingsTrans.GetComponent<Outline>();
        if (sOutline == null) sOutline = settingsTrans.gameObject.AddComponent<Outline>();
        sOutline.effectColor = new Color(0.4f, 0.8f, 1f, 0.6f);
        sOutline.effectDistance = new Vector2(1.5f, -1.5f);

        Text sText = settingsTrans.GetComponentInChildren<Text>();
        if (sText == null)
        {
            GameObject stGO = new GameObject("Text");
            stGO.transform.SetParent(settingsTrans, false);
            stGO.AddComponent<RectTransform>();
            stGO.AddComponent<CanvasRenderer>();
            sText = stGO.AddComponent<Text>();
            RectTransform stRT = stGO.GetComponent<RectTransform>();
            stRT.anchorMin = Vector2.zero; stRT.anchorMax = Vector2.one; stRT.sizeDelta = Vector2.zero;
        }
        sText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
        sText.fontSize = 20;
        sText.alignment = TextAnchor.MiddleCenter;
        sText.color = Color.white;
        sText.supportRichText = true;
        sText.text = "<b>⚙   SETTINGS</b>";

        if (menuMgr != null)
        {
            sBtn.onClick.RemoveAllListeners();
            UnityEditor.Events.UnityEventTools.AddPersistentListener(sBtn.onClick, menuMgr.OpenSettings);
        }

        // Setup DeviceButton
        Transform deviceTrans = menuCard.transform.Find("DeviceButton");
        GameObject deviceBtnGO;
        if (deviceTrans == null)
        {
            deviceBtnGO = new GameObject("DeviceButton");
            deviceBtnGO.transform.SetParent(menuCard.transform, false);
            deviceBtnGO.AddComponent<RectTransform>();
            deviceBtnGO.AddComponent<CanvasRenderer>();
            deviceBtnGO.AddComponent<Image>();
            deviceBtnGO.AddComponent<Button>();
            deviceTrans = deviceBtnGO.transform;
        }
        else
        {
            deviceBtnGO = deviceTrans.gameObject;
        }

        RectTransform drt = deviceTrans.GetComponent<RectTransform>();
        drt.anchorMin = new Vector2(0.5f, 0.5f);
        drt.anchorMax = new Vector2(0.5f, 0.5f);
        drt.pivot = new Vector2(0.5f, 0.5f);
        drt.anchoredPosition = new Vector2(0f, -28f);
        drt.sizeDelta = new Vector2(300f, 50f);

        Image dImg = deviceTrans.GetComponent<Image>();
        dImg.color = new Color(0.10f, 0.25f, 0.40f, 0.95f);

        Button dBtn = deviceTrans.GetComponent<Button>();
        ColorBlock dcb = dBtn.colors;
        dcb.normalColor = new Color(0.10f, 0.25f, 0.40f, 0.95f);
        dcb.highlightedColor = new Color(0.16f, 0.38f, 0.60f, 1f);
        dcb.pressedColor = new Color(0.06f, 0.16f, 0.28f, 1f);
        dBtn.colors = dcb;

        var dOutline = deviceTrans.GetComponent<Outline>();
        if (dOutline == null) dOutline = deviceTrans.gameObject.AddComponent<Outline>();
        dOutline.effectColor = new Color(0.35f, 0.75f, 1f, 0.6f);
        dOutline.effectDistance = new Vector2(1.5f, -1.5f);

        Text dText = deviceTrans.GetComponentInChildren<Text>();
        if (dText == null)
        {
            GameObject dtGO = new GameObject("Text");
            dtGO.transform.SetParent(deviceTrans, false);
            dtGO.AddComponent<RectTransform>();
            dtGO.AddComponent<CanvasRenderer>();
            dText = dtGO.AddComponent<Text>();
            RectTransform dtrt = dtGO.GetComponent<RectTransform>();
            dtrt.anchorMin = Vector2.zero;
            dtrt.anchorMax = Vector2.one;
            dtrt.sizeDelta = Vector2.zero;
        }
        dText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (dText.font == null) dText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        dText.fontSize = 18;
        dText.alignment = TextAnchor.MiddleCenter;
        dText.color = Color.white;
        dText.supportRichText = true;
        dText.text = DeviceSettings.IsMobile
            ? "DEVICE: <color=#FFE040><b>MOBILE (TOUCH)</b></color>"
            : "DEVICE: <color=#55DDFF><b>PC (KEYBOARD)</b></color>";

        if (menuMgr != null)
        {
            dBtn.onClick.RemoveAllListeners();
            UnityEditor.Events.UnityEventTools.AddPersistentListener(dBtn.onClick, menuMgr.OnDeviceButtonClicked);
        }

        // Reparent and style QuitButton
        Transform quitTrans = canvas.transform.Find("QuitButton");
        if (quitTrans == null && menuCardTrans != null) quitTrans = menuCardTrans.Find("QuitButton");
        if (quitTrans != null)
        {
            quitTrans.SetParent(menuCard.transform, false);
            RectTransform qrt = quitTrans.GetComponent<RectTransform>();
            qrt.anchorMin = new Vector2(0.5f, 0.5f);
            qrt.anchorMax = new Vector2(0.5f, 0.5f);
            qrt.pivot = new Vector2(0.5f, 0.5f);
            qrt.anchoredPosition = new Vector2(0f, -86f);
            qrt.sizeDelta = new Vector2(300f, 48f);

            Image qImg = quitTrans.GetComponent<Image>();
            if (qImg != null) qImg.color = new Color(0.58f, 0.18f, 0.20f, 0.95f); // Crimson

            Button qBtn = quitTrans.GetComponent<Button>();
            if (qBtn != null)
            {
                ColorBlock cb = qBtn.colors;
                cb.normalColor = new Color(0.58f, 0.18f, 0.20f, 0.95f);
                cb.highlightedColor = new Color(0.78f, 0.24f, 0.28f, 1f);
                cb.pressedColor = new Color(0.42f, 0.12f, 0.14f, 1f);
                qBtn.colors = cb;
            }

            Text qText = quitTrans.GetComponentInChildren<Text>();
            if (qText != null)
            {
                qText.text = "<b>✕   QUIT</b>";
                qText.fontSize = 22;
                qText.alignment = TextAnchor.MiddleCenter;
                qText.color = new Color(1f, 0.95f, 0.95f, 1f);
            }
        }

        // Style or create Bottom Info Bar
        Transform infoTrans = canvas.transform.Find("ControlsInfo");
        if (infoTrans == null)
        {
            GameObject infoGO = new GameObject("ControlsInfo");
            infoGO.transform.SetParent(canvas.transform, false);
            infoTrans = infoGO.transform;
        }

        RectTransform irt = infoTrans.GetComponent<RectTransform>();
        if (irt == null) irt = infoTrans.gameObject.AddComponent<RectTransform>();
        irt.anchorMin = new Vector2(0.5f, 0f);
        irt.anchorMax = new Vector2(0.5f, 0f);
        irt.pivot = new Vector2(0.5f, 0f);
        irt.anchoredPosition = new Vector2(0f, 35f);
        irt.sizeDelta = new Vector2(980f, 75f);

        Image infoBg = infoTrans.GetComponent<Image>();
        if (infoBg == null) infoBg = infoTrans.gameObject.AddComponent<Image>();
        infoBg.color = new Color(0.04f, 0.06f, 0.10f, 0.85f); // Dark translucent bar

        var infoOutline = infoTrans.GetComponent<Outline>();
        if (infoOutline == null) infoOutline = infoTrans.gameObject.AddComponent<Outline>();
        infoOutline.effectColor = new Color(0.2f, 0.8f, 0.4f, 0.35f);
        infoOutline.effectDistance = new Vector2(1f, -1f);

        Text infoText = infoTrans.GetComponentInChildren<Text>();
        if (infoText == null) infoText = infoTrans.gameObject.AddComponent<Text>();
        infoText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (infoText.font == null) infoText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        infoText.fontSize = 20;
        infoText.alignment = TextAnchor.MiddleCenter;
        infoText.color = new Color(0.92f, 0.96f, 1f, 0.95f);
        infoText.supportRichText = true;
        infoText.text = "<b>CONTROLS:</b> WASD to Move  |  SPACE to Jump  |  SHIFT to Sprint  |  ESC to Pause\n<color=#40FF70><b>MAZE CHEAT:</b> Press <b>[M]</b> in the maze to reveal glowing guide arrows to victory!</color>";
    }
}

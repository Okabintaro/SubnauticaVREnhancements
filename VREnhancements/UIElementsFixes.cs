﻿using HarmonyLib;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR;
using UnityEngine.PostProcessing;

namespace VREnhancements
{
    class UIElementsFixes
    {
        static RectTransform CameraCyclopsHUD;
        static RectTransform CameraDroneHUD;
        static float CameraHUDScaleFactor = 0.75f;
        static uGUI_SceneHUD sceneHUD;
        static bool seaglideEquipped = false;
        static Transform barsPanel;
        static Transform quickSlots;
        static Transform compass;
        static Transform sunbeamCountdown;
        static bool fadeBarsPanel = true;
        static float lastHealth = -1;
        static float lastOxygen = -1;
        static float lastFood = -1;
        static float lastWater = -1;
        static Rect defaultSafeRect;
        static float menuDistance = 1.5f;

        public static void SetDynamicHUD(bool enabled)
        {
            UIFader qsFader = quickSlots.gameObject.GetComponent<UIFader>();
            UIFader barsFader = barsPanel.gameObject.GetComponent<UIFader>();
            if (qsFader && barsFader)
            {
                qsFader.SetAutoFade(enabled);
                barsFader.SetAutoFade(enabled);
            }
        }
        public static void UpdateHUDOpacity(float alpha)
        {
            if (sceneHUD)
            {
                sceneHUD.GetComponent<CanvasGroup>().alpha = alpha;
                if(VehicleHUDManager.vehicleCanvas)
                    VehicleHUDManager.vehicleCanvas.GetComponent<CanvasGroup>().alpha = alpha;
                if(sunbeamCountdown)
                    sunbeamCountdown.Find("Background").GetComponent<CanvasRenderer>().SetAlpha(0f);//make sure the background remains hidden
                //make sure the reticle isn't affected by HUD alpha setting
                if(HandReticle.main && HandReticle.main.GetComponent<CanvasGroup>())
                    HandReticle.main.GetComponent<CanvasGroup>().alpha = 0.8f;
            }
        }
        public static void UpdateHUDDistance(float distance)
        {
            if (sceneHUD)
            {
                Transform screenCanvas = sceneHUD.transform.parent;
                Camera uicamera = ManagedCanvasUpdate.GetUICamera();
                if (uicamera != null)
                {
                    Transform transform = uicamera.transform;
                    //move the screen canvas instead of just the HUD so all on screen elements like blips etc are also affect by the distance update.
                    screenCanvas.transform.localPosition = screenCanvas.transform.parent.transform.InverseTransformPoint(transform.position + transform.forward * distance);
                    //make sure the elements are still facing the camera after changing position
                    quickSlots.rotation = Quaternion.LookRotation(quickSlots.position);
                    compass.rotation = Quaternion.LookRotation(compass.position);
                    barsPanel.rotation = Quaternion.LookRotation(barsPanel.position);
                }
            }
        }
        public static void UpdateHUDScale(float scale)
        {
            if (sceneHUD)
            {
                sceneHUD.GetComponent<RectTransform>().localScale = Vector3.one * scale;
            }
        }
        public static void UpdateHUDSeparation(float separation)
        {
            if (sceneHUD)
            {
                Rect safeAreaRect;
                //to make sure that the Rect is centered the width should be 1 - 2x
                switch (separation)
                {
                    case 0:
                        safeAreaRect = defaultSafeRect;
                        break;
                    case 1:
                        safeAreaRect = new Rect(0.3f,0.3f,0.4f,0.3f);
                        break;
                    case 2:
                        safeAreaRect = new Rect(0.2f, 0.2f, 0.6f, 0.5f);
                        break;
                    case 3:
                        safeAreaRect = new Rect(0.15f, 0.15f, 0.7f, 0.6f);
                        break;
                    default:
                        safeAreaRect = defaultSafeRect;
                        break;
                }
                sceneHUD.GetComponent<uGUI_SafeAreaScaler>().vrSafeRect = safeAreaRect;
                //the position of element in front the UI Camera would change if the Rect size changes so making sure the elements still face the camera.
                quickSlots.rotation = Quaternion.LookRotation(quickSlots.position);
                compass.rotation = Quaternion.LookRotation(compass.position);
                barsPanel.rotation = Quaternion.LookRotation(barsPanel.position);

            }
        }

        public static void InitHUD()
        {
            //add CanvasGroup to the HUD to be able to set the alpha of all HUD elements
            if (!sceneHUD.gameObject.GetComponent<CanvasGroup>())
                sceneHUD.gameObject.AddComponent<CanvasGroup>();
            UpdateHUDOpacity(AdditionalVROptions.HUD_Alpha);
            UpdateHUDDistance(AdditionalVROptions.HUD_Distance);
            UpdateHUDScale(AdditionalVROptions.HUD_Scale);
            //UpdateHUDSeparation done in uGUI_SceneLoading.End instead
            if (!quickSlots.GetComponent<UIFader>())
            {
                UIFader qsFader = quickSlots.gameObject.AddComponent<UIFader>();
                if (qsFader)
                    qsFader.SetAutoFade(AdditionalVROptions.dynamicHUD);
            }
            if (!barsPanel.GetComponent<UIFader>())
            {
                UIFader barsFader = barsPanel.gameObject.AddComponent<UIFader>();
                if (barsFader)
                {
                    barsFader.SetAutoFade(AdditionalVROptions.dynamicHUD);
                    barsFader.autoFadeDelay = 2;
                }
            }
        }
        public static void SetSubtitleHeight(float percentage)
        {
            Subtitles.main.popup.oy = Subtitles.main.GetComponent<RectTransform>().rect.height * percentage / 100;
        }
        public static void SetSubtitleScale(float scale)
        {
            Subtitles.main.popup.GetComponent<RectTransform>().localScale = Vector3.one * scale;
        }

        [HarmonyPatch(typeof(Subtitles), nameof(Subtitles.Show))]
        class SubtitlesPosition_Patch
        {
            static bool Prefix(Subtitles __instance)
            {
                __instance.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0.5f);//to keep subtitles centered when scaling.
                __instance.popup.text.alignment = TextAnchor.MiddleLeft;
                SetSubtitleScale(AdditionalVROptions.subtitleScale);
                SetSubtitleHeight(AdditionalVROptions.subtitleHeight);
                return true;
            }
        }

        [HarmonyPatch(typeof(ErrorMessage), nameof(ErrorMessage.AddMessage))]
        class AddErrorMessage_Patch
        {
            //disables error messages while loading to prevent the ugly overlapping error messages
            static bool Prefix()
            {
                if (uGUI.main.loading.IsLoading)
                {
                    return false;
                }
                else
                    return true;
            }
        }

        [HarmonyPatch(typeof(uGUI_PlayerDeath), nameof(uGUI_PlayerDeath.Start))]
        class uGUI_PlayerDeath_Start_Patch
        {
            static void Postfix(uGUI_PlayerDeath __instance)
            {
                __instance.blackOverlay.gameObject.GetComponent<RectTransform>().localScale = Vector3.one * 2;
            }
        }
        [HarmonyPatch(typeof(uGUI_PlayerSleep), nameof(uGUI_PlayerSleep.Start))]
        class uGUI_PlayerSleep_Start_Patch
        {
            static void Postfix(uGUI_PlayerSleep __instance)
            {
                __instance.blackOverlay.gameObject.GetComponent<RectTransform>().localScale = Vector3.one * 2;
            }
        }
        [HarmonyPatch(typeof(uGUI_SceneIntro), nameof(uGUI_SceneIntro.Start))]
        class uGUI_uGUI_SceneIntro_Start_Patch
        {
            static void Postfix(uGUI_SceneIntro __instance)
            {
                __instance.gameObject.GetComponent<RectTransform>().sizeDelta = Vector2.one * 2000;
            }
        }

        [HarmonyPatch(typeof(Seaglide), nameof(Seaglide.OnDraw))]
        class Seaglide_OnDraw_Patch
        {
            static void Postfix()
            {
                seaglideEquipped = true;
            }
        }
        [HarmonyPatch(typeof(Seaglide), nameof(Seaglide.OnHolster))]
        class Seaglide_OnHolster_Patch
        {
            static void Postfix()
            {
                seaglideEquipped = false;
            }
        }

        [HarmonyPatch(typeof(uGUI), nameof(uGUI.Awake))]
        class LoadingScreen_Patch
        {
            static void Postfix(uGUI __instance)
            {
                if (!__instance.loading.GetComponent<VRLoadingScreen>())
                    __instance.loading.gameObject.AddComponent<VRLoadingScreen>();
            }
        }

        /*[HarmonyPatch(typeof(uGUI_SceneLoading), nameof(uGUI_SceneLoading.Begin))]
        class uGUI_Awake_Patch
        {
            static void Postfix()
            {
                //TODO: Figure out why the Screen Canvas distance gets reset to 1 when loading a save. (Resets because the UI camera changes during loading)
                //resetting distance at the start of loading a save to make sure it doesn't get reset to 1
                UpdateHUDDistance(AdditionalVROptions.HUD_Distance);
            }
        }*/
        [HarmonyPatch(typeof(uGUI_SceneLoading), nameof(uGUI_SceneLoading.ShowLoadingScreen))]
        class uGUI_ShowLoading_Patch
        {
            static bool Prefix()
            {
                GameObject mainCam = GameObject.Find("Main Camera");
                if (mainCam)
                {
                    //MainCameraV2 doesn't exist in the main menu so I'm not sure what they were doing in uGUI_SceneLoading.OnFadeFinished in the original code
                    //this will disable the actual main camera immediately at the start of loading. This camera is replaced after the scene loads.
                    mainCam.GetComponent<Camera>().enabled = false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(uGUI_SceneLoading), nameof(uGUI_SceneLoading.End))]
        class SceneLoading_End_Patch
        {
            static void Postfix(uGUI_SceneLoading __instance)
            {
                VRUtil.Recenter();
                quickSlots.rotation = Quaternion.LookRotation(quickSlots.position);
                compass.rotation = Quaternion.LookRotation(compass.position);
                barsPanel.rotation = Quaternion.LookRotation(barsPanel.position);
                UpdateHUDSeparation(AdditionalVROptions.HUD_Separation);//wasn't working in HUD awake so put it here instead
            }
        }

        [HarmonyPatch(typeof(uGUI_SceneHUD), nameof(uGUI_SceneHUD.Awake))]
        class SceneHUD_Awake_Patch
        {
            static void Postfix(uGUI_SceneHUD __instance)
            {
                sceneHUD = __instance;
                barsPanel = __instance.transform.Find("Content/BarsPanel");
                quickSlots = __instance.transform.Find("Content/QuickSlots");
                compass = __instance.transform.Find("Content/DepthCompass");
                defaultSafeRect = sceneHUD.GetComponent<uGUI_SafeAreaScaler>().vrSafeRect;
                __instance.gameObject.AddComponent<VehicleHUDManager>();
                InitHUD();
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.Update))]
        class Player_Update_Patch
        {
            static void Postfix()
            {
                UIFader barsFader = barsPanel.GetComponent<UIFader>();
                UIFader qsFader = quickSlots.GetComponent<UIFader>();
                Player player = Player.main;
                Survival survival = player.GetComponent<Survival>();
                fadeBarsPanel = AdditionalVROptions.dynamicHUD;
                float fadeInStart = 10;
                float fadeRange = 10;//max alpha at start+range degrees

                if (AdditionalVROptions.dynamicHUD && !player.GetPDA().isInUse && survival && barsFader)
                { 
                    if(Mathf.Abs(player.liveMixin.health-lastHealth)/player.liveMixin.maxHealth > 0.05f || player.liveMixin.GetHealthFraction() < 0.33f ||
                        player.GetOxygenAvailable() < (player.GetOxygenCapacity() / 3) || player.GetOxygenAvailable() > lastOxygen ||
                        survival.food < 50 || survival.food > lastFood ||
                        survival.water < 50 || survival.water > lastWater)
                    {
                        fadeBarsPanel = false;
                    }
                    lastHealth = player.liveMixin.health;
                    lastOxygen = player.GetOxygenAvailable();
                    lastFood = survival.food;
                    lastWater = survival.water;
                    barsFader.SetAutoFade(fadeBarsPanel);
                    qsFader.SetAutoFade(!Player.main.inExosuit && !Player.main.inSeamoth);
                }
                //if the PDA is in use turn on look down for hud
                if (player.GetPDA().isInUse)
                {
                    barsFader.SetAutoFade(false);
                    qsFader.SetAutoFade(false);
                    //fades the hud in based on the view pitch. Forward is 360/0 degrees and straight down is 90 degrees.
                    if (MainCamera.camera.transform.localEulerAngles.x < 180)
                        UpdateHUDOpacity(Mathf.Clamp((MainCamera.camera.transform.localEulerAngles.x - fadeInStart) / fadeRange, 0, 1) * AdditionalVROptions.HUD_Alpha);
                    else
                        UpdateHUDOpacity(0);
                }//opacity is set back to HUDAlpha in PDA.Close Postfix
            }
        }

        [HarmonyPatch(typeof(PDA), nameof(PDA.Close))]
        class PDA_Close_Patch
        {
            static void Postfix()
            {
                UpdateHUDOpacity(AdditionalVROptions.HUD_Alpha);
            }
        }

        
        [HarmonyPatch(typeof(QuickSlots), nameof(QuickSlots.NotifySelect))]
        class QuickSlots_NotifySelect_Patch
        {
            static void Postfix()
            {
                UIFader qsFader = quickSlots.GetComponent<UIFader>();
                qsFader.Fade(AdditionalVROptions.HUD_Alpha, 0, 0, true);//make quickslot visible as soon as the slot changes. Using Fade to cancel any running fades.
                if (!seaglideEquipped)
                    qsFader.autoFadeDelay = 2;
                else
                    qsFader.autoFadeDelay = 1; ;//fade with shorter delay if seaglide is active.
                //keep the slots visible if piloting the seamoth or suit
                qsFader.SetAutoFade((AdditionalVROptions.dynamicHUD || seaglideEquipped));
            }
        }

        [HarmonyPatch(typeof(HandReticle), nameof(HandReticle.Start))]
        class HandReticle_Start_Patch
        {
            static void Postfix()
            {
                //add CanvasGroup to the HandReticle to be able to override the HUD CanvasGroup alpha settings to keep the Reticle always opaque.
                if (HandReticle.main)
                {
                    HandReticle.main.gameObject.AddComponent<CanvasGroup>().ignoreParentGroups = true;//not sure if this will cause issues when changes are made to the ScreenCanvas CanvasGroup;
                }
                   
            }
        }
        [HarmonyPatch(typeof(uGUI_SunbeamCountdown), nameof(uGUI_SunbeamCountdown.Start))]
        class SunbeamCountdown_Start_Patch
        {
            //makes the Sunbeam timer visible by moving it from the top right to bottom middle. Also hides the timer background.
            public static void Postfix(uGUI_SunbeamCountdown __instance)
            {
                sunbeamCountdown = __instance.transform;
                sunbeamCountdown.SetParent(quickSlots.parent, false);
                sunbeamCountdown.localPosition = new Vector3(0, -150, 0);
                RectTransform SunbeamRect = __instance.countdownHolder.GetComponent<RectTransform>();
                SunbeamRect.anchorMax = SunbeamRect.anchorMin = SunbeamRect.pivot = new Vector2(0.5f, 0.5f);
                SunbeamRect.anchoredPosition = new Vector2(0f, -275f);
                SunbeamRect.localScale = Vector3.one * 0.75f;
                sunbeamCountdown.Find("Background").GetComponent<CanvasRenderer>().SetAlpha(0f);//hide background
            }

        }

        [HarmonyPatch(typeof(uGUI_CameraDrone), nameof(uGUI_CameraDrone.Awake))]
        class CameraDrone_Awake_Patch
        {
            //Reduce the size of the HUD in the Drone Camera to make edges visible
            static void Postfix(uGUI_CameraDrone __instance)
            {
                CameraDroneHUD = __instance.transform.Find("Content/CameraScannerRoom").GetComponent<RectTransform>();
                if (CameraDroneHUD)
                {
                    CameraDroneHUD.localScale = new Vector3(CameraHUDScaleFactor * AdditionalVROptions.HUD_Scale, CameraHUDScaleFactor * AdditionalVROptions.HUD_Scale, 1f);
                }
            }
        }
        [HarmonyPatch(typeof(uGUI_CameraDrone), nameof(uGUI_CameraDrone.OnEnable))]
        class CameraDrone_OnEnable_Patch
        {
            //make sure the camera HUD is visible
            static void Postfix(uGUI_CameraDrone __instance)
            {
                UpdateHUDOpacity(AdditionalVROptions.HUD_Alpha);
            }
        }

        [HarmonyPatch(typeof(uGUI_CameraCyclops), nameof(uGUI_CameraCyclops.Awake))]
        class CameraCyclops_Awake_Patch
        {
            //Reduce the size of the HUD in the Cyclops Camera to make edges visible
            static void Postfix(uGUI_CameraCyclops __instance)
            {
                CameraCyclopsHUD = __instance.transform.Find("Content/CameraCyclops").GetComponent<RectTransform>();
                if (CameraCyclopsHUD)
                {
                    CameraCyclopsHUD.localScale = new Vector3(CameraHUDScaleFactor * AdditionalVROptions.HUD_Scale, CameraHUDScaleFactor * AdditionalVROptions.HUD_Scale, 1f);
                }

            }
        }

        [HarmonyPatch(typeof(uGUI_CameraCyclops), nameof(uGUI_CameraCyclops.OnEnable))]
        class CameraCyclops_OnEnable_Patch
        {
            //make sure the camera HUD is visible
            static void Postfix(uGUI_CameraCyclops __instance)
            {
                UpdateHUDOpacity(AdditionalVROptions.HUD_Alpha);
            }
        }

        [HarmonyPatch(typeof(HandReticle), nameof(HandReticle.LateUpdate))]
        class HR_LateUpdate_Patch
        {
            //fixes the reticle distance being locked to the interaction distance after interaction. eg Entering Seamoth and piloting Cyclops
            static bool Prefix(HandReticle __instance)
            {
                if (Player.main)
                {
                    Targeting.GetTarget(Player.main.gameObject, 2f, out GameObject activeTarget, out float reticleDistance, null);
                    SubRoot currSub = Player.main.GetCurrentSub();
                    //if piloting the cyclops and not using cyclops cameras
                    //TODO: find a way to use the raycast distance for the ui elements instead of the fixed value of 1.55
                    if (Player.main.isPiloting && currSub && currSub.isCyclops && !CameraCyclopsHUD.gameObject.activeInHierarchy)
                    {
                        __instance.SetTargetDistance(reticleDistance > 1.55f ? 1.55f : reticleDistance);
                    }
                    else if (Player.main.GetMode() == Player.Mode.LockedPiloting || CameraCyclopsHUD.gameObject.activeInHierarchy)
                    {
                        __instance.SetTargetDistance(AdditionalVROptions.HUD_Distance);
                    }
                }
                return true;
            }
            static void Postfix(HandReticle __instance)
            {
                //this fixes reticle alignment in menus etc
                __instance.transform.position = new Vector3(0f, 0f, __instance.transform.position.z);
            }
        }

        static bool actualGazedBasedCursor;
        [HarmonyPatch(typeof(FPSInputModule), nameof(FPSInputModule.GetCursorScreenPosition))]
        class GetCursorScreenPosition_Patch
        {
            static void Postfix(FPSInputModule __instance, ref Vector2 __result)
            {
                if (XRSettings.enabled)
                {
                    if (Cursor.lockState == CursorLockMode.Locked)
                    {
                        __result = GraphicsUtil.GetScreenSize() * 0.5f;
                    }
                    else if (!actualGazedBasedCursor)
                        //fix cursor snapping to middle of view when cursor goes off canvas due to hack in UpdateCursor
                        //Screen.width gives monitor width and GraphicsUtil.GetScreenSize().x will give either monitor or VR eye texture width
                        __result = new Vector2(Input.mousePosition.x / Screen.width * GraphicsUtil.GetScreenSize().x, Input.mousePosition.y / Screen.height * GraphicsUtil.GetScreenSize().y);

                }
            }
        }

        [HarmonyPatch(typeof(FPSInputModule), nameof(FPSInputModule.UpdateCursor))]
        class UpdateCursor_Patch
        {
            static void Prefix()
            {
                //save the original value so we can set it back in the postfix
                actualGazedBasedCursor = VROptions.gazeBasedCursor;
                //trying make flag in UpdateCursor be true if Cursor.lockState != CursorLockMode.Locked
                if (Cursor.lockState != CursorLockMode.Locked)
                {
                    VROptions.gazeBasedCursor = true;
                }

            }
            static void Postfix(FPSInputModule __instance)
            {
                VROptions.gazeBasedCursor = actualGazedBasedCursor;
                //Fix the problem with the cursor rendering behind UI elements.
                //TODO: Check if this is the best way to fix this. The cursor still goes invisible if you click off the canvas. Check lastgroup variable in FPSInputModule
                Canvas cursorCanvas = __instance._cursor.GetComponentInChildren<Graphic>().canvas;
                RaycastResult lastRaycastResult = Traverse.Create(__instance).Field("lastRaycastResult").GetValue<RaycastResult>();
                if (cursorCanvas && lastRaycastResult.isValid)
                {
                    cursorCanvas.sortingLayerID = lastRaycastResult.sortingLayer;//put the cursor on the same layer as whatever was hit by the cursor raycast.
                }
            }
        }
        static Transform screenCanvas;
        static Transform overlayCanvas;
        static Transform mainMenuUICam;
        static Transform mainMenu;
        [HarmonyPatch(typeof(uGUI_MainMenu), nameof(uGUI_MainMenu.Awake))]
        class MM_Awake_Patch
        {
            static void Postfix(uGUI_MainMenu __instance)
            {
                GameObject mainCam = GameObject.Find("Main Camera");
                mainMenuUICam = ManagedCanvasUpdate.GetUICamera().transform;
                mainMenu = __instance.transform.Find("Panel/MainMenu");
                screenCanvas = GameObject.Find("ScreenCanvas").transform;
                overlayCanvas = GameObject.Find("OverlayCanvas").transform;
                //disabling the canvas scaler to prevent it from messing up the custom distance and scale
                __instance.gameObject.GetComponent<uGUI_CanvasScaler>().enabled = false;
                __instance.transform.position = new Vector3(mainMenuUICam.transform.position.x + menuDistance,-0.8f,0);
                __instance.transform.localScale = Vector3.one * 0.0035f;
                //add AA to the main menu UI. The shimmering edges are better but the text is blurry.
                /*PostProcessingBehaviour postB = ManagedCanvasUpdate.GetUICamera().gameObject.AddComponent<PostProcessingBehaviour>();
                postB.profile = mainCam.GetComponent<UwePostProcessingManager>().defaultProfile;
                postB.profile.depthOfField.enabled = false;
                postB.profile.bloom.enabled = false;
                AntialiasingModel.Settings AAsettings = postB.profile.antialiasing.settings;
                //extreme performance give sharper text
                AAsettings.fxaaSettings.preset = AntialiasingModel.FxaaPreset.Performance;
                postB.profile.antialiasing.settings = AAsettings;*/
                VRUtil.Recenter();
            }
        }
        [HarmonyPatch(typeof(uGUI_MainMenu), nameof(uGUI_MainMenu.Update))]
        class MM_Update_Patch
        {
            static void Postfix(uGUI_MainMenu __instance)
            {
                //keep the main menu tilted towards the camera.
                mainMenu.transform.root.rotation = Quaternion.LookRotation(mainMenu.position - new Vector3(mainMenuUICam.position.x, mainMenuUICam.position.y, mainMenu.position.z));
                //match screen and overlay canvas position and rotation to main menu
                screenCanvas.localPosition = __instance.transform.localPosition;
                screenCanvas.position = __instance.transform.position;
                screenCanvas.rotation = __instance.transform.rotation;
                overlayCanvas.localPosition = __instance.transform.localPosition;
                overlayCanvas.position = __instance.transform.position;
                overlayCanvas.rotation = __instance.transform.rotation;
                //try to keep the main menu visible if the HMD is moved more than 0.5 after starting the game.
                if (mainMenuUICam.localPosition.magnitude > 0.5f)
                    VRUtil.Recenter();
                }
        }
        [HarmonyPatch(typeof(IngameMenu), nameof(IngameMenu.Open))]
        class InGameMenu_Open_Patch
        {
            static bool Prefix(IngameMenu __instance)
            {
                uGUI_CanvasScaler canvasScaler = __instance.gameObject.GetComponent<uGUI_CanvasScaler>();
                canvasScaler.distance = menuDistance;
                __instance.transform.localScale = Vector3.one * 0.002f;
                return true;
            }
        }

        [HarmonyPatch(typeof(uGUI_BuildWatermark), nameof(uGUI_BuildWatermark.UpdateText))]
        class BWM_UpdateText_Patch
        {
            static void Postfix(uGUI_BuildWatermark __instance)
            {
                //make the version watermark more visible
                __instance.GetComponent<Text>().color = new Vector4(1,1,1,0.5f);
                //TODO: fix this after moving the main menu back and scaling up or consider reparenting to main menu
                __instance.transform.localPosition = new Vector3(950, -450, 0);
                
            }
        }
        [HarmonyPatch(typeof(uGUI_CanvasScaler), nameof(uGUI_CanvasScaler.SetScaleFactor))]
        class Canvas_ScaleFactor_Patch
        {
            static bool Prefix(ref float scaleFactor)
            {
                //any scale factor less than 1 reduces the quality of UI elements.
                if (scaleFactor < 1)
                    scaleFactor = 1;
                return true;
            }
        }
        [HarmonyPatch(typeof(uGUI_CanvasScaler), nameof(uGUI_CanvasScaler.UpdateTransform))]
        class Canvas_UpdateTransform_Patch
        {
            static bool Prefix(uGUI_CanvasScaler __instance)
            {
                if(__instance.gameObject.name == "ScreenCanvas")
                    __instance.distance = AdditionalVROptions.HUD_Distance;
                return true;
            }
        }
        [HarmonyPatch(typeof(uGUI_CanvasScaler), nameof(uGUI_CanvasScaler.UpdateFrustum))]
        class Canvas_UpdateFrustum_Patch
        {
            static float originalDistance=1;
            static bool Prefix(uGUI_CanvasScaler __instance)
            {
                if (__instance.gameObject.name == "ScreenCanvas")
                {
                    originalDistance = __instance.distance;
                    __instance.distance = 1;
                }
                return true;
            }
            static void Postfix(uGUI_CanvasScaler __instance)
            {
                if (__instance.gameObject.name == "ScreenCanvas")
                    __instance.distance = originalDistance;
            }
        }
    }
}

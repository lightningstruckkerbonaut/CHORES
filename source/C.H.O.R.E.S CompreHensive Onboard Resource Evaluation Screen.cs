using KSP.UI.Screens;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace C.H.O.R.E.S
{

    [KSPAddon(KSPAddon.Startup.FlightAndEditor, false)]
    public class CHORES : MonoBehaviour
    {
        ApplicationLauncherButton button;
        Rect windowRect = new Rect(100, 100, 650, 400);
        private void CreateButtonIcon()
        {

            Texture buttonTexture = GameDatabase.Instance.GetTexture("CHORES/icon", false);
            button = ApplicationLauncher.Instance.AddModApplication(ToggleOn, ToggleOff, null, null, null, null, ApplicationLauncher.AppScenes.FLIGHT | ApplicationLauncher.AppScenes.MAPVIEW | ApplicationLauncher.AppScenes.SPH | ApplicationLauncher.AppScenes.VAB, buttonTexture);
        }
        // STORING THE RESOURCES IN A DICTIONARY SO THE MOD DOESNT HAVE TO ASK KSP EVERY TIME
        Dictionary<string, PartResourceDefinition> discoveredResources = new Dictionary<string, PartResourceDefinition>();// Makes a new Dictionary called discoveredResources, and defines that the dictionary contains a string and a part resource definition


        // find all the resources on the start state and also make the button appear
        public void Start()
        {

            GetEverysingleResourceIntheGame(); // n the start state, this public override void makes GetEverysingleResourceIntheGame() run.

            GameEvents.onVesselChange.Add(OnVesselChange); // When the player changes vessels it makes the OnVesselChange() run
            GameEvents.onEditorShipModified.Add(OnEditorShipModified);
            Vessel activeVessel = FlightGlobals.ActiveVessel; // Defines the active vessel as the vessel that is currently being controlled by the player.
            if (activeVessel != null)
            {
                OnVesselChange(activeVessel); // n the start state, this thing makes it scan the active vessel (see the section // SCAN VESSEL FOR RESOURCES EVERY TIME YOU SWITCH VESSELS)
                                              // but only if there IS an active vessel
            }
            if (HighLogic.LoadedSceneIsEditor && EditorLogic.fetch != null)
            {
                if (EditorLogic.fetch.ship != null)
                    OnEditorShipModified(EditorLogic.fetch.ship); // n the start state, this thing makes it scan the active vessel (see the section // SCAN VESSEL FOR RESOURCES EVERY TIME YOU SWITCH VESSELS)
                                                                  // but only if there IS an active vessel
            }
            if (!ApplicationLauncher.Ready)
            {
                GameEvents.onGUIApplicationLauncherReady.Add(OnLauncherReady);
            }
            else
            {
                OnLauncherReady();
            }
        }
        public void OnLauncherReady()
        {
            if (!button) //if theres no button
            {
                CreateButtonIcon(); //it makes the button
                GameEvents.onGUIApplicationLauncherReady.Remove(OnLauncherReady); //and it also stops itself from running again
            }

        }




        // ACTUALLY GETTING EVERY RESOURCE

        public void GetEverysingleResourceIntheGame()
        {
            discoveredResources.Clear(); // clears the discoveredResources so that it has no duplicates
            foreach (var resource in PartResourceLibrary.Instance.resourceDefinitions)
            {
                discoveredResources.Add(resource.name, resource); // Adds the resource to the dictionary or list of discovered resources
            }
        }



        bool scanningOn = true;

        float passedTime = 0f;
        float scanInterval = 2f;
        public void Update()
        {
            if (HighLogic.LoadedSceneIsFlight)
            {
                passedTime += Time.deltaTime;
                if (passedTime >= scanInterval)
                {
                    if (FlightGlobals.ActiveVessel != null && scanningOn)
                    {
                        ScanParts(FlightGlobals.ActiveVessel.parts);
                    }
                    passedTime = 0f;
                }
            }

        }

        public void OnEditorShipModified(ShipConstruct ship) // shipconstruct is the thingy in the editor and the vessel being built is called ship
        {
            if (HighLogic.LoadedSceneIsEditor)
            {
                if (scanningOn && ship != null && ship.parts != null)
                {
                    ScanParts(ship.parts); // calls the scanparts to scan the ship parts
                }
            }
        }

        // SCAN VESSEL FOR RESOURCES
        public void OnVesselChange(Vessel vessel)
        {

            if (vessel != null)
            {
                ScanParts(vessel.parts); // scan vessel when it changes
            }

        }
        public void ScanParts(IEnumerable<Part> parts) // scan vessel
        {
            if (parts != null)
            {
                vesselResourceMasses.Clear(); // so the vesselResourceMasses dictionary doesnt keep the masses of the resources from the craft you were on before


                foreach (Part part in parts)
                {
                    foreach (PartResource resource in part.Resources)
                    {
                        if (discoveredResources.TryGetValue(resource.resourceName, out PartResourceDefinition resourceDefinition))
                        {
                            var resourceMassPerUnit = resourceDefinition.density * 1000; // Calculation for the mass of the resource.density x 1000 to get mass in kg because its in tons


                            var resourceTotalMassInPart = resource.amount * resourceMassPerUnit; // Calculation for the mass of all of the resource in the part


                            if (vesselResourceMasses.TryGetValue(resource.resourceName, out double existingMass)) // if it can find the resource name and the existing mass of the resource in the dictionary.... (if there is no existingmass it also makes one)...
                            {
                                vesselResourceMasses[resource.resourceName] = existingMass + resourceTotalMassInPart; // ...it sets that the mass of the resource in the vessel is the already counted mass of the resource in the vessel + the mass of the resource in the part being counted.
                            }
                            else // otherwise...
                            {
                                vesselResourceMasses.Add(resource.resourceName, resourceTotalMassInPart); // ...it adds the resource to the dictionary and the mass of the part being scanned.
                            }
                        }
                        else
                        {
                            Debug.Log("CHORES: Resource Definition Not Found for: " + resource.resourceName); // just in case
                        }
                    }
                }
            }
        }
        public void OnDestroy() // this public void defines what OnDestroy() actually does
        {
            GameEvents.onVesselChange.Remove(OnVesselChange); // When the player changes vessels it makes the OnVesselChange() stop running, this bit of code is AFTER the add(onvesselchange) so it runs AFTER it.
            if (ApplicationLauncher.Instance != null && button != null)
            {
                ApplicationLauncher.Instance.RemoveModApplication(button);

            }
            GameEvents.onGUIApplicationLauncherReady.Remove(OnLauncherReady);
            GameEvents.onEditorShipModified.Remove(OnEditorShipModified);
        }

        // STUFF TO FIND THE MASS OF EVERY RESOURCE IN THE VESSEL COMBINED or something
        Dictionary<string, double> vesselResourceMasses = new Dictionary<string, double>(); // stores the vessel resource masses

        public bool On = false;



        public void ToggleOn() // when button is pressed
        {
            On = true;

        }
        public void OnGUI()
        {
            if (On)
            {
                windowRect = GUILayout.Window(239057, windowRect, DrawWindow, "C.H.O.R.E.S. Onboard Resource Evaluation");
            }

        }
        string searchQuery = "";
        GUIStyle resourceNameStyle;
        Vector2 scrollPosition = Vector2.zero;
        public void DrawWindow(int windowID)
        {
            searchQuery = GUILayout.TextField(searchQuery);

            if (resourceNameStyle == null)
            {
                resourceNameStyle = new GUIStyle(GUI.skin.label);
                resourceNameStyle.fontSize = 15;
                resourceNameStyle.fontStyle = FontStyle.BoldAndItalic;
            }
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Width(600), GUILayout.Height(300));
            foreach (var resourceTotals in vesselResourceMasses.OrderBy(pair => pair.Key))
            {
                if (resourceTotals.Key.IndexOf(searchQuery, StringComparison.OrdinalIgnoreCase) == -1)
                {
                    continue;
                }

                var onboardResDefinition = discoveredResources[resourceTotals.Key];
                var resourcemassPerUnit = onboardResDefinition.density * 1000;

                GUILayout.BeginVertical();
                GUILayout.BeginHorizontal();
                GUI.contentColor = Color.green;

                GUILayout.Label(resourceTotals.Key + ": " + resourceTotals.Value.ToString("F2") + "kg in vessel", resourceNameStyle);
                GUILayout.EndHorizontal();
                GUI.contentColor = Color.white;
                GUILayout.Label("Resource Details: " + onboardResDefinition.volume.ToString("F2") + " L/u volume | " + resourcemassPerUnit.ToString("F2") + "kg / " + onboardResDefinition.density.ToString("F4") + " tonnes mass per unit");
                GUILayout.EndVertical();

            }
            GUILayout.EndScrollView();
            if (GUILayout.Button("Close"))
            {
                ToggleOff();
            }
            if (GUILayout.Button("Refresh"))
            {
                if (HighLogic.LoadedSceneIsEditor && EditorLogic.fetch != null)
                {
                    if (EditorLogic.fetch.ship != null)
                    {
                        ScanParts(EditorLogic.fetch.ship.parts);
                    }
                }
                else if (HighLogic.LoadedSceneIsFlight && FlightGlobals.ActiveVessel != null)
                {
                    ScanParts(FlightGlobals.ActiveVessel.parts);
                }

            }
            scanningOn = (GUILayout.Toggle(scanningOn, "Toggle Scans"));
            if (HighLogic.LoadedSceneIsFlight)
            {
                scanInterval = GUILayout.HorizontalSlider(scanInterval, 0.5f, 10f);
                GUILayout.Label("Refresh Interval: " + scanInterval.ToString("F2") + "s");
            }


            GUI.DragWindow();


        }
        public void ToggleOff()
        {
            On = false;
        }
    }
}

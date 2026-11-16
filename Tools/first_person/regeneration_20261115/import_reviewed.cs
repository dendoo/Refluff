// Generated from reviewed art settings by prepare_import.py. No global refresh or scene rebuild.
var report = new System.Collections.Generic.List<string>();
report.Add(CarpetCleaning.FirstPersonModelImporter.ImportContact(CarpetCleaning.ToolId.Scraper,"Assets/CarpetCleaning/Models/FirstPerson/Tool_Scraper.glb",new UnityEngine.Vector3(0f,90f,0f),new UnityEngine.Vector3(-0.025101f,0.074177f,0.015297f),-1f,0.15f));
report.Add(CarpetCleaning.FirstPersonModelImporter.Import(CarpetCleaning.ToolId.Hose,"Assets/CarpetCleaning/Models/FirstPerson/Tool_Hose.glb",new UnityEngine.Vector3(0f,-90f,0f),0.3f));
report.Add(CarpetCleaning.FirstPersonModelImporter.FitNozzle(CarpetCleaning.ToolId.Hose,0.004f,1f,new UnityEngine.Vector3(0.23f,-0.26f,0.48f)));
report.Add(CarpetCleaning.FirstPersonModelImporter.SetHoseSocket(CarpetCleaning.ToolId.Hose,new UnityEngine.Vector3(-0.007962f,-0.116374f,-0.120958f)));
report.Add(CarpetCleaning.FirstPersonModelImporter.ImportContact(CarpetCleaning.ToolId.Brush,"Assets/CarpetCleaning/Models/FirstPerson/Tool_Brush.glb",new UnityEngine.Vector3(0f,90f,0f),new UnityEngine.Vector3(0f,0.0342f,0f),0.0152f,0.15f));
report.Add(CarpetCleaning.FirstPersonModelImporter.ImportContact(CarpetCleaning.ToolId.Squeegee,"Assets/CarpetCleaning/Models/FirstPerson/Tool_Squeegee.glb",new UnityEngine.Vector3(0f,90f,0f),new UnityEngine.Vector3(-0.013764f,0.114549f,0.016579f),-1f,0.15f));
report.Add(CarpetCleaning.FirstPersonModelImporter.ImportContact(CarpetCleaning.ToolId.WetVacuum,"Assets/CarpetCleaning/Models/FirstPerson/Tool_WetVacuum.glb",new UnityEngine.Vector3(0f,90f,0f),new UnityEngine.Vector3(-0.058686f,0.097243f,0.016493f),-1f,0.15f));
report.Add(CarpetCleaning.FirstPersonModelImporter.Import(CarpetCleaning.ToolId.FloorCleaner,"Assets/CarpetCleaning/Models/FirstPerson/Tool_FloorCleaner.glb",new UnityEngine.Vector3(0f,-90f,0f),0.4f));
report.Add(CarpetCleaning.FirstPersonModelImporter.FitNozzle(CarpetCleaning.ToolId.FloorCleaner,0.004f,1f,new UnityEngine.Vector3(0.23f,-0.26f,0.48f)));
report.Add(CarpetCleaning.FirstPersonModelImporter.SetHoseSocket(CarpetCleaning.ToolId.FloorCleaner,new UnityEngine.Vector3(-0f,-0.126448f,-0.170685f)));
report.Add(CarpetCleaning.FirstPersonModelImporter.ImportContact(CarpetCleaning.ToolId.Mop,"Assets/CarpetCleaning/Models/FirstPerson/Tool_Mop.glb",new UnityEngine.Vector3(0f,90f,0f),new UnityEngine.Vector3(-0.044036f,0.118698f,0.018735f),-1f,0.15f));
return string.Join("\n",report);

var set=CarpetCleaning.FirstPersonModelImporter.EnsureSet();
if(!set.TryGet(CarpetCleaning.ToolId.FoamSprayer,out var entry))return "Missing accepted foam body";
entry.id=CarpetCleaning.ToolId.FloorCleaner;entry.scale=.85f;
var entries=new System.Collections.Generic.List<CarpetCleaning.FirstPersonToolSet.Entry>(set.entries);
int found=entries.FindIndex(e=>e.id==CarpetCleaning.ToolId.FloorCleaner);
if(found>=0)entries[found]=entry;else entries.Add(entry);
set.entries=entries.ToArray();UnityEditor.EditorUtility.SetDirty(set);UnityEditor.AssetDatabase.SaveAssets();
return "FloorCleaner: accepted foam-gun body, scale0.85, exact nozzle marker preserved";

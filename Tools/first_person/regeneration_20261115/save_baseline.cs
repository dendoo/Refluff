string path=CarpetCleaning.SaveStore.FilePath;
var bytes=System.IO.File.ReadAllBytes(path);
System.IO.File.WriteAllBytes("Tools/first_person/regeneration_20261115/baseline/save_before_model_captures.bin",bytes);
System.IO.File.WriteAllText("Tools/first_person/regeneration_20261115/baseline/save_path.txt",path);
return path+"; bytes="+bytes.Length;

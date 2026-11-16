# Capture revision 4

Use the running Unity Editor and Unity MCP execute_code with compiler codedom. Keep scripts outside Assets;
execute their contents in memory. Preserve fresh copies of carpet_save_editor.json and carpet_save.json before Play.

1. Enter Play; execute ../stage.cs (parent macro_20261025 folder, two levels above this file's sibling take folders).
2. Execute ../revision2/prepare_intro_v2.cs. This prepares the shared target Dirt snapshot, linear ramp texture,
   temporary 96-shell mesh and parked pointer. The staging script already created the microscopic 384-shell mesh.
3. Execute mf01d_intro.cs. Continue StoreGifApi.Run(1800) until done:true, then SeqEnd(2000) until pendingPng:0.
   Final result must be 645 frames/Steps, 10.750 seconds, errors:0. A completed existing take is guarded against overwrite.
4. Preserve the AppDomain mfCameraLog and mfDirtRampLog StringBuilders as camera.csv and dirt_ramp.csv beside the take.
5. Execute restore_v4.cs, stop Play, restore the fresh save files and compare their hashes.

Keys: forward translation 0–2 s (+z 9 mm with a matching target translation); full-rug view at 5.05 s;
hold to 5.75 s; approach to the existing vacuum framing at 10.45 s; hold to 10.75 s.
Dirt smoothstep 5.95–10.25 s. All later take files are reused unchanged. Output/EDL commands are in README.md.

Actual forward travel, height, FOV and direction are recorded in camera.csv at every captured frame, including
the initial pose0. The transition is staged only for the trailer; the gameplay simulation source is unchanged.

The two-second dolly has exactly triple v3's displacement at the same timestamps and constant FOV/height.
Speed proof and first-frame contact comparisons are in SteamStore/_work/macro_20261025/revision4/qa.


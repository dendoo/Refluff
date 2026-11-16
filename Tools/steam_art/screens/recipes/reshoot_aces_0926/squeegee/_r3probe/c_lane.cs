// EXPERIMENT: lane C variant (stop point from the file name convention: edit STOPU)
UnityEditor.EditorApplication.isPaused = true;
float STOPU = 0.925f; float STOPV = 0.30f + (0.94f - STOPU) * (1200f / 1500f) * (0.551f / 0.834f) * 0f;
// same line as r3_t4_lane: direction (0.834, -0.551) mm -> per unit u: dv = -(0.551/0.834) * 1200/1500 * du
float du = STOPU - 0.349f; float dv = -(0.551f / 0.834f) * (1200f / 1500f) * du;
float v1 = 0.612f + dv;
int frames = Mathf.RoundToInt(73f * du / (0.94f - 0.349f));
CarpetCleaning.CarpetDebugApi.StrokeNow(5, 0.349f,0.612f, STOPU, v1, frames, 1f);
CarpetCleaning.CarpetDebugApi.PinToolHead(5, STOPU, v1, true);
return "stop " + STOPU + "," + v1 + " frames " + frames + " " + CarpetCleaning.CarpetDebugApi.PlowSpillJson();

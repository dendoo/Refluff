// Physical simulation frame. Keep in step with CarpetSimulation and FloorConsts.
// Dimensions are mm; CMM (0,0) is the carpet's lower-left corner. RT resolution does not scale tools or pile.
#ifndef CARPET_SIMULATION_DOMAIN_INCLUDED
#define CARPET_SIMULATION_DOMAIN_INCLUDED
#define CARPET_W_MM 3600.0
#define CARPET_H_MM 4500.0
#define CARPET_MM float2(CARPET_W_MM, CARPET_H_MM)
#define CARPET_CENTRE_MM (0.5 * CARPET_MM)
#define CARPET_SIM_W 512
#define CARPET_SIM_H 640
#define CARPET_DXC (CARPET_W_MM / (float)CARPET_SIM_W)
#define FLOOR_Y (-3.0)
#define FLOOR_SIZE 12000.0
#define FLOOR_W 1280
#define FLOOR_MIN (CARPET_CENTRE_MM - 0.5 * FLOOR_SIZE)
#define FLOOR_DX (FLOOR_SIZE / (float)FLOOR_W)
#define FLOOR_AREA (FLOOR_DX * FLOOR_DX)
// Particle recipes were tuned on the original 3.125 mm cells; preserve events per physical square mm.
#define EMIT_REFERENCE_DX 3.125
#define EMIT_CELL_AREA_SCALE ((CARPET_DXC * CARPET_DXC) / (EMIT_REFERENCE_DX * EMIT_REFERENCE_DX))
#endif

#!/bin/bash
# usage: mk4.sh out take "W, H, CX, CY, CW, CH" DU V1 V2 V3 BURST REST MU MV DAB AWAY0 HOLD
cd "$(dirname "$0")"
sed "s/\"TAKE\"/\"$2\"/; s/W, H, CX, CY, CW, CH/$3/; s/du = DU, v1 = V1, v2 = V2, v3 = V3/du = $4f, v1 = $5f, v2 = $6f, v3 = $7f/; s/b = BURST/b = $8f/; s/Up(REST)/Up($9f)/; s/mu = MU, mv = MV/mu = ${10}f, mv = ${11}f/; s/Hold(mu, mv, DAB)/Hold(mu, mv, ${12}f)/; s/Away(AWAY0)/Away(${13}f)/; s/Away(HOLD)/Away(${14}f)/" take4.tpl > $1.cs

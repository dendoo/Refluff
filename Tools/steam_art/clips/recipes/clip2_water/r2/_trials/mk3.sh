#!/bin/bash
# usage: mk3.sh out take "W, H, CX, CY, CW, CH" SPRAYV DU SPRAY REST MU MV DAB AWAY0 HOLD
cd "$(dirname "$0")"
sed "s/\"TAKE\"/\"$2\"/; s/W, H, CX, CY, CW, CH/$3/; s/SPRAYV/$4f/g; s/= DU;/= $5f;/; s/SPRAY,/$6f,/g; s/Up(REST)/Up($7f)/; s/= MU,/= $8f,/; s/mv = MV/mv = $9f/; s/DAB)/${10}f)/; s/Away(AWAY0)/Away(${11}f)/; s/Away(HOLD)/Away(${12}f)/" take3.tpl > $1.cs

#!/bin/bash
# usage: mk2.sh out take "W, H, CX, CY, CW, CH" SPRAYV SPRAYS REST D1U D1V D2U D2V DAB1 GLIDE DAB2 AWAY0 HOLD
cd "$(dirname "$0")"
sed "s/\"TAKE\"/\"$2\"/; s/W, H, CX, CY, CW, CH/$3/; s/SPRAYV/$4f/g; s/SPRAYS/$5f/g; s/REST/$6f/g; s/D1U/$7f/; s/D1V/$8f/; s/D2U/$9f/; s/D2V/${10}f/; s/DAB1/${11}f/g; s/GLIDE/${12}f/g; s/DAB2/${13}f/g; s/AWAY0/${14}f/g; s/Away(HOLD)/Away(${15}f)/" take2.tpl > $1.cs

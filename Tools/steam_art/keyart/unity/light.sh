#!/bin/bash
# light.sh pitch yaw intensity kelvin ambR ambG ambB
cat <<CS
{
var L = UnityEngine.GameObject.Find("Directional Light").GetComponent<UnityEngine.Light>();
L.transform.rotation = UnityEngine.Quaternion.Euler(${1}f, ${2}f, 0f);
L.intensity = ${3}f; L.colorTemperature = ${4}f; L.shadows = UnityEngine.LightShadows.Soft;
UnityEngine.RenderSettings.ambientSkyColor = new UnityEngine.Color(${5}f, ${6}f, ${7}f);
}
CS

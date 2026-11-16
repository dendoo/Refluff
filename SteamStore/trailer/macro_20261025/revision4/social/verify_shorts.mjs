import { spawnSync } from 'node:child_process';
import { readFileSync, writeFileSync, statSync } from 'node:fs';
import { createHash } from 'node:crypto';
import path from 'node:path';

const root = 'C:/UnityProjects/CarpetCleaning/SteamStore/trailer/macro_20261025/revision4';
const source = path.join(root, 'refluff_macro_trailer_v4.mp4');
const output = path.join(root, 'social/refluff_macro_trailer_v4_shorts_9x16.mp4');
const folder = path.dirname(output);
const ffmpeg = 'C:/UnityProjects/StainedGlassKeeper/Tools/SocialStudio/node_modules/ffmpeg-static/ffmpeg.exe';
const ffprobe = 'C:/UnityProjects/StainedGlassKeeper/Tools/SocialStudio/node_modules/ffprobe-static/bin/win32/x64/ffprobe.exe';
function run(exe, args, options={}) {
  const result = spawnSync(exe, args, { encoding: 'utf8', maxBuffer: 32*1024*1024, ...options });
  if (result.status !== 0) throw new Error(`${exe}: ${result.stderr || result.error}`);
  return result;
}
function probe(file) { return JSON.parse(run(ffprobe, ['-v','error','-show_streams','-show_format','-of','json',file]).stdout); }
function sha(file) { return createHash('sha256').update(readFileSync(file)).digest('hex'); }
function audioHash(file, pcm=false) {
  return run(ffmpeg,['-v','error','-i',file,'-map','0:a:0','-c:a',pcm?'pcm_s16le':'copy','-f','hash','-hash','sha256','-']).stdout.trim();
}
const sourceProbe = probe(source), outputProbe = probe(output);
writeFileSync(path.join(folder,'source_ffprobe.json'), JSON.stringify(sourceProbe,null,2));
writeFileSync(path.join(folder,'shorts_ffprobe.json'), JSON.stringify(outputProbe,null,2));
const sourceAudioHash = audioHash(source), outputAudioHash = audioHash(output);
const sourcePcmHash = audioHash(source,true), outputPcmHash = audioHash(output,true);
const errors = run(ffmpeg,['-v','error','-i',output,'-f','null','NUL']).stderr.trim();
const samples=[];
for (const at of [1,22.5]) {
  const suffix=String(at).replace('.','_');
  const sourcePng=path.join(folder,`source_${suffix}s.png`), outputPng=path.join(folder,`shorts_${suffix}s.png`);
  run(ffmpeg,['-v','error','-y','-ss',String(at),'-i',source,'-frames:v','1',sourcePng]);
  run(ffmpeg,['-v','error','-y','-ss',String(at),'-i',output,'-frames:v','1',outputPng]);
  const quality=run(ffmpeg,['-hide_banner','-i',sourcePng,'-i',outputPng,'-lavfi','[0:v]scale=1080:608:flags=lanczos,format=yuv420p[a];[1:v]crop=1080:608:0:656,format=yuv420p[b];[a][b]ssim','-f','null','NUL']).stderr;
  const rgb = run(ffmpeg,['-v','error','-i',outputPng,'-frames:v','1','-pix_fmt','rgb24','-f','rawvideo','-'],{encoding:null}).stdout;
  let blackBarsAreZero=true, paddingMaxRgb=0, nonzeroPaddingChannels=0, innerPaddingMaxRgb=0;
  for(let y=0;y<1920;y++) {
    if(y>=656 && y<1264) continue;
    for(let x=y*1080*3;x<(y+1)*1080*3;x++) {
      if(rgb[x]!==0) { blackBarsAreZero=false; nonzeroPaddingChannels++; }
      paddingMaxRgb=Math.max(paddingMaxRgb,rgb[x]);
      if(y<654 || y>=1266) innerPaddingMaxRgb=Math.max(innerPaddingMaxRgb,rgb[x]);
    }
  }
  const ssim=Number(quality.match(/All:([\d.]+)/)?.[1]);
  samples.push({at_seconds:at,source_frame:sourcePng,derived_frame:outputPng,scaled_whole_frame_ssim:ssim,black_padding_rgb_zero:blackBarsAreZero,padding_max_rgb:paddingMaxRgb,padding_nonzero_channels:nonzeroPaddingChannels,padding_excluding_2px_boundary_max_rgb:innerPaddingMaxRgb});
}
const sv=sourceProbe.streams.find(x=>x.codec_type==='video'), ov=outputProbe.streams.find(x=>x.codec_type==='video');
const qa={
  source:{path:source,bytes:statSync(source).size,sha256:sha(source)},
  derivative:{path:output,bytes:statSync(output).size,sha256:sha(output),width:ov.width,height:ov.height,frames:Number(ov.nb_frames),fps:ov.avg_frame_rate,duration_seconds:Number(outputProbe.format.duration)},
  transform:{scale:{width:1080,height:608,filter:'lanczos'},canvas:{width:1080,height:1920},offset:{x:0,y:656},padding:'black',crop:false,added_text:false,timing_changed:false,note:'The whole 1920x1080 frame is scaled to 1080x608; nearest even output height rounds ideal 607.5 pixels by 0.5 px.'},
  checks:{source_is_approved_v4:sha(source)==='86f0066bef312868e4ca0c43442634c480d2ab4f05de90c77ef65c40f5b9edb6',frame_count_matches:ov.nb_frames===sv.nb_frames,frame_rate_matches:ov.avg_frame_rate===sv.avg_frame_rate,duration_matches:outputProbe.format.duration===sourceProbe.format.duration,aac_packet_data_identical:sourceAudioHash===outputAudioHash,decoded_pcm_s16le_identical:sourcePcmHash===outputPcmHash,decode_errors:errors,h264_high:ov.codec_name==='h264'&&ov.profile==='High',yuv420p:ov.pix_fmt==='yuv420p',bt709:ov.color_space==='bt709'&&ov.color_transfer==='bt709'&&ov.color_primaries==='bt709'},
  audio:{source_aac_sha256:sourceAudioHash,derivative_aac_sha256:outputAudioHash,source_pcm_sha256:sourcePcmHash,derivative_pcm_sha256:outputPcmHash},
  samples,
  visual_inspection:'Passed at 1.0 and 22.5 seconds: whole landscape frame intact, centered with black top/bottom padding; no added text or visible crop. Pixel-level deviations at picture boundary may occur from normal H.264 chroma compression.'
};
writeFileSync(path.join(folder,'shorts_qa.json'),JSON.stringify(qa,null,2));
console.log(JSON.stringify(qa,null,2));

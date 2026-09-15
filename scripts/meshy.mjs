import {readFile,writeFile,mkdir} from 'node:fs/promises';
const root=new URL('../art/meshy/',import.meta.url);await mkdir(root,{recursive:true});
const env=await readFile(new URL('../.env.meshy.local',import.meta.url),'utf8');const key=env.match(/^MESHY_API_KEY=(.+)$/m)?.[1].trim();if(!key)throw Error('Meshy local key missing');
async function api(endpoint,data){const r=await fetch('https://api.meshy.ai/openapi/'+endpoint,{method:data?'POST':'GET',headers:{Authorization:'Bearer '+key,'Content-Type':'application/json'},...(data?{body:JSON.stringify(data)}:{}),signal:AbortSignal.timeout(60000)});const body=await r.json();if(!r.ok)throw Error('Meshy '+r.status+': '+JSON.stringify(body));return body;}
const mode=process.argv[2];
if(mode==='correct-wetsuit'){
 const file=new URL('diver-wetsuit.json',root);
 try{const previous=JSON.parse(await readFile(file));console.log({existingTask:previous.result});}
 catch(e){if(e.code!=='ENOENT')throw e;
 const {result:id}=JSON.parse(await readFile(new URL('diver-texture.json',root)));
 const request={input_task_id:id,ai_model:'meshy-7',enable_original_uv:true,enable_pbr:true,texture_resolution:'2k',target_formats:['glb','fbx'],text_style_prompt:'Full coverage professional diving wetsuit, CLOSED FRONT, high round collar. The entire chest, abdomen, belly, back, shoulders, upper arms, legs and feet are covered in opaque dark navy blue neoprene fabric. A continuous navy fabric panel covers the torso from neck collar to waist, with a small closed vertical zipper and subtle stitched seams. No bare chest, no bare belly, no open jacket, no cutouts, no skin anywhere on torso. Bright cobalt blue side stripes and shoulder accents. Natural tan human skin ONLY on face, neck above the collar, and hands. Realistic goggles, short brown hair. Athletic human diver, clean matte neoprene, natural human face, no logos or text.'};
 await writeFile(new URL('diver-wetsuit-request.json',root),JSON.stringify(request,null,2));await writeFile(file,JSON.stringify({pending:true}),{flag:'wx'});const result=await api('v1/retexture',request);await writeFile(file,JSON.stringify(result));console.log({taskCreated:true,...result});
 }
}
if(mode==='wetsuit-status'){
 const {result:id}=JSON.parse(await readFile(new URL('diver-wetsuit.json',root)));if(!id)throw Error('Submission outcome needs checking');const result=await api('v1/retexture/'+id);await writeFile(new URL('diver-wetsuit-status.json',root),JSON.stringify(result,null,2));console.log({status:result.status,progress:result.progress,thumbnail:result.thumbnail_url,error:result.task_error});
}
if(mode==='rig'){
 const file=new URL('diver-rig.json',root);
 try{const previous=JSON.parse(await readFile(file));console.log({existingTask:previous.result});}
 catch(e){if(e.code!=='ENOENT')throw e;const {result:id}=JSON.parse(await readFile(new URL('diver-texture.json',root)));await writeFile(file,JSON.stringify({pending:true}),{flag:'wx'});const result=await api('v1/rigging',{input_task_id:id,height_meters:1.8});await writeFile(file,JSON.stringify(result));console.log({taskCreated:true,...result});}
}
if(mode==='rig-status'){
 const {result:id}=JSON.parse(await readFile(new URL('diver-rig.json',root)));if(!id)throw Error('Rig submission outcome needs checking');const result=await api('v1/rigging/'+id);await writeFile(new URL('diver-rig-status.json',root),JSON.stringify(result,null,2));console.log({status:result.status,progress:result.progress,error:result.task_error});
}
if(mode==='download-rig'){
 const task=JSON.parse(await readFile(new URL('diver-rig-status.json',root)));if(task.status!=='SUCCEEDED')throw Error('Rig is not ready');
 for(const format of ['glb','fbx']){const url=task.result['rigged_character_'+format+'_url'];if(!url)continue;const r=await fetch(url);if(!r.ok)throw Error('Rig download failed');await writeFile(new URL('diver-rigged.'+format,root),new Uint8Array(await r.arrayBuffer()));console.log('Saved rigged '+format);}
}
if(mode==='texture'){
 const file=new URL('diver-texture.json',root);
 try{const previous=JSON.parse(await readFile(file));console.log({existingTask:previous.result});}
 catch(e){if(e.code!=='ENOENT')throw e;
  const {result:id}=JSON.parse(await readFile(new URL('diver-preview.json',root)));
  const preview=await api('v2/text-to-3d/'+id);if(preview.status!=='SUCCEEDED')throw Error('Preview is not ready');
  const request={mode:'refine',preview_task_id:id,enable_pbr:true,texture_resolution:'2k',target_formats:['fbx','glb'],texture_prompt:'Realistic adult human underwater hockey athlete. Natural medium tan skin on face, neck, forearms and hands, believable skin detail and subtle variations, dark brown eyebrows and short dark hair. A fitted matte dark navy neoprene short-sleeved wetsuit covers torso and legs with clean cobalt-blue shoulder and side panels, subtle fabric seams. Dark rubber diving mask frame and blue-gray glass lenses, restrained silver hardware. Physically realistic materials, no baked shadows, no logos, no text, no metallic skin, no robotic details. Clean premium aquatic sports styling.'};
  await writeFile(new URL('diver-texture-request.json',root),JSON.stringify(request,null,2));
  // Reserve the request before transmission so an ambiguous failure cannot silently spend twice.
  await writeFile(file,JSON.stringify({pending:true}),{flag:'wx'});
  const result=await api('v2/text-to-3d',request);await writeFile(file,JSON.stringify(result));console.log({taskCreated:true,...result});
 }
}
if(mode==='texture-status'){
 const {result:id}=JSON.parse(await readFile(new URL('diver-texture.json',root)));if(!id)throw Error('Texture submission outcome needs checking before retry');
 const result=await api('v2/text-to-3d/'+id);await writeFile(new URL('diver-texture-status.json',root),JSON.stringify(result,null,2));console.log({status:result.status,progress:result.progress,thumbnail:result.thumbnail_url});
}
if(mode==='download-textured'||mode==='download-wetsuit'){
 const name=mode==='download-wetsuit'?'wetsuit':'texture';
 const result=JSON.parse(await readFile(new URL('diver-'+name+'-status.json',root)));if(result.status!=='SUCCEEDED')throw Error('Textures are not ready');
 for(const format of ['glb','fbx']){const url=result.model_urls?.[format];if(!url)continue;const r=await fetch(url);if(!r.ok)throw Error('Model download failed');await writeFile(new URL('diver-'+name+'.'+format,root),new Uint8Array(await r.arrayBuffer()));console.log('Saved '+name+' '+format);}
 for(const [i,maps] of (result.texture_urls||[]).entries())for(const [type,url]of Object.entries(maps)){if(typeof url!=='string'||!url.startsWith('https://'))continue;const r=await fetch(url);if(!r.ok)throw Error('Texture download failed');await writeFile(new URL('diver-'+name+'-'+i+'-'+type+'.png',root),new Uint8Array(await r.arrayBuffer()));console.log('Saved '+type);}
}
if(mode==='balance')console.log(await api('v1/balance'));
if(mode==='diver'){
 const file=new URL('diver-preview.json',root);try{const previous=JSON.parse(await readFile(file));console.log({existingTask:previous.result});}catch(e){if(e.code!=='ENOENT')throw e;
 const request={mode:'preview',ai_model:'meshy-7',model_type:'standard',should_remesh:true,target_polycount:18000,topology:'quad',pose_mode:'a-pose',target_formats:['fbx','glb'],prompt:'A realistic adult male underwater hockey athlete, full body, anatomically natural human proportions, athletic lean build, detailed recognizable human face with short dark hair. Wearing a close-fitting dark navy neoprene short sleeve wetsuit with blue shoulder panels, bare forearms and hands with five separated fingers, close fitting diving goggles over the eyes and a simple snorkel beside the head, plain bare feet for separate swim fins. Neutral symmetrical A pose, arms separated from torso, legs slightly apart. Realistic human, not robot, not armor, no oxygen tank, no weapon, no platform, single character only.'};
 await writeFile(new URL('diver-request.json',root),JSON.stringify(request,null,2));const result=await api('v2/text-to-3d',request);await writeFile(file,JSON.stringify(result));console.log({taskCreated:true,...result});}}
if(mode==='poll'){const {result:id}=JSON.parse(await readFile(new URL('diver-preview.json',root)));const result=await api('v2/text-to-3d/'+id);await writeFile(new URL('diver-preview-status.json',root),JSON.stringify(result,null,2));console.log({status:result.status,progress:result.progress,thumbnail:result.thumbnail_url,models:result.model_urls});}

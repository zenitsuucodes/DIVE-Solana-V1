import {readFileSync,writeFileSync,mkdirSync,existsSync} from 'node:fs';
import {fileURLToPath} from 'node:url';
import {spawnSync} from 'node:child_process';
const root=fileURLToPath(new URL('../',import.meta.url)), raw=root+'art/audio/elevenlabs/';
const key=readFileSync(root+'.env.elevenlabs.local','utf8').match(/^\s*ELEVENLABS_API_KEY\s*=\s*(.*?)\s*$/m)?.[1]?.replace(/^['"]|['"]$/g,'');
if(!key)throw Error('Local API key missing.');
const jobs=[
 ['BlueStart','Blue team starts with the puck. Get ready!'],
 ['CoralStart','Coral team starts with the puck. Get ready!'],
 ['BlueGoal','Goal! Blue team scores!'],
 ['CoralGoal','Goal! Coral team scores!'],
 ['BlueWin','Blue team wins! What a finish!'],
 ['CoralWin','Coral team takes the win! Good game!']
];
mkdirSync(raw,{recursive:true});
const records=existsSync(raw+'announcer.json')?JSON.parse(readFileSync(raw+'announcer.json','utf8')):[];
async function generate(name,path,body){
 const dest=raw+name+'.mp3';if(existsSync(dest))return dest;
 console.log('Generating '+name);
 const r=await fetch('https://api.elevenlabs.io'+path,{method:'POST',headers:{'xi-api-key':key,'Content-Type':'application/json'},body:JSON.stringify(body),signal:AbortSignal.timeout(180000)});
 if(!r.ok){let code='request_failed';try{code=(await r.json()).detail?.status||code;}catch{}console.log(name+': HTTP '+r.status+' ('+code+')');return null;}
 const b=Buffer.from(await r.arrayBuffer());if(b.length<500)throw Error('Empty audio');writeFileSync(dest,b);
 records.push({name,...body,provider:'ElevenLabs',generatedAt:new Date().toISOString(),reportedCreditCost:r.headers.get('character-cost')});writeFileSync(raw+'announcer.json',JSON.stringify(records,null,2)+'\n');console.log(name+' saved; credits '+r.headers.get('character-cost'));return dest;
}
function convert(name,file,music=false){
 if(!file)return;
 const filter=music?'loudnorm=I=-22:TP=-3:LRA=7':'highpass=f=80,loudnorm=I=-18:TP=-2:LRA=7';
 const r=spawnSync('ffmpeg',['-hide_banner','-loglevel','error','-y','-i',file,'-af',filter,'-ar','24000','-ac',music?'2':'1','-c:a','pcm_s16le',root+'unity/Assets/Dive/Resources/Audio/'+name+'.wav'],{encoding:'utf8'});if(r.status!==0)throw Error('Conversion failed: '+name);
}
for(const [name,text] of jobs){const file=await generate(name,'/v1/text-to-speech/JBFqnCBsd6RMkjVDRZzb?output_format=mp3_44100_128',{text,model_id:'eleven_multilingual_v2',voice_settings:{stability:.45,similarity_boost:.75,style:.25}});if(!file)break;convert(name,file);}
const music=await generate('Music','/v1/music?output_format=mp3_44100_128',{prompt:'Instrumental soundtrack for a friendly futuristic underwater hockey arena. Calm energetic downtempo electronic groove at 96 BPM, warm rounded bass, soft mallet arpeggios, airy aquatic synth textures, restrained percussion. Consistent repeating groove suitable for background gameplay. No vocals, no spoken words, no dramatic intro or ending.',music_length_ms:20000,force_instrumental:true});convert('Music',music,true);

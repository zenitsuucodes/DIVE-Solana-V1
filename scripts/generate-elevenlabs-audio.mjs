import {readFileSync,writeFileSync,mkdirSync,existsSync} from 'node:fs';
import {fileURLToPath} from 'node:url';
import {spawnSync} from 'node:child_process';
const root=fileURLToPath(new URL('../',import.meta.url));
const raw=root+'art/audio/elevenlabs/';
const out=root+'unity/Assets/Dive/Resources/Audio/';
const jobs=[
 {name:'Ambience',seconds:8,loop:true,text:'Seamless quiet underwater swimming pool ambience, submerged listening perspective, gentle low watery rumble and delicate distant bubbles. Constant calm texture. No voices, no music, no dramatic impacts, no breathing.'},
 {name:'Swim',seconds:3,loop:true,text:'Seamless loop of gentle swimming fins and gloved hands moving through water, heard from underwater. Soft rhythmic watery swishes and small bubbles, close intimate foley, no music, no voices, no breathing.'},
 {name:'Shot',seconds:1,loop:false,text:'One short immediate underwater hockey puck hit by a small plastic stick: a firm muffled tok impact followed by a tiny rush of bubbles. Single isolated game sound, starts immediately, fast decay, no voices, no music.'},
 {name:'Impact',seconds:1,loop:false,text:'One short muted rubber hockey puck knock against a swimming pool tile, heard underwater. Soft low clack, very brief watery resonance, immediate onset and quick decay, no music, no voices.'}
];
if(!process.argv.includes('--generate')){console.log('Plan: four sound effects, 13 seconds total. Run with --generate to use ElevenLabs credits. Existing downloads are reused.');process.exit(0);}
const config=readFileSync(root+'.env.elevenlabs.local','utf8');
const key=config.match(/^\s*ELEVENLABS_API_KEY\s*=\s*(.*?)\s*$/m)?.[1]?.replace(/^['"]|['"]$/g,'');
if(!key)throw Error('Save ELEVENLABS_API_KEY in .env.elevenlabs.local first.');
mkdirSync(raw,{recursive:true});mkdirSync(out,{recursive:true});
const manifestPath=raw+'manifest.json';
const manifest=existsSync(manifestPath)?JSON.parse(readFileSync(manifestPath,'utf8')):[];
for(const job of jobs){
 const file=raw+job.name+'.mp3';
 if(!existsSync(file)){
  console.log('Generating '+job.name+' ('+job.seconds+' seconds)');
  const response=await fetch('https://api.elevenlabs.io/v1/sound-generation?output_format=mp3_44100_128',{
   method:'POST',headers:{'xi-api-key':key,'Content-Type':'application/json'},
   body:JSON.stringify({text:job.text,duration_seconds:job.seconds,loop:job.loop,model_id:'eleven_text_to_sound_v2',prompt_influence:.4}),signal:AbortSignal.timeout(120000)
  });
  if(!response.ok){let code='request_failed';try{const body=await response.json();code=body.detail?.status||code;}catch{}throw Error('ElevenLabs returned HTTP '+response.status+' ('+code+'). No automatic retry.');}
  const audio=Buffer.from(await response.arrayBuffer());if(audio.length<500)throw Error('Empty audio response; stopping.');
  writeFileSync(file,audio);
  manifest.push({...job,provider:'ElevenLabs',model:'eleven_text_to_sound_v2',generatedAt:new Date().toISOString(),reportedCreditCost:response.headers.get('character-cost'),file:job.name+'.mp3'});
  writeFileSync(manifestPath,JSON.stringify(manifest,null,2)+'\n');
  console.log('Saved '+job.name+'; reported credits: '+response.headers.get('character-cost'));
 }
 const filters=['highpass=f=45','lowpass=f=3500','loudnorm=I=-22:TP=-3:LRA=7'];
 if(!job.loop)filters.push('afade=t=in:d=0.005','afade=t=out:st=0.8:d=0.2');
 const result=spawnSync('ffmpeg',['-hide_banner','-loglevel','error','-y','-i',file,'-af',filters.join(','),'-ar','24000','-ac','1','-c:a','pcm_s16le',out+job.name+'.wav'],{encoding:'utf8'});
 if(result.status!==0)throw Error('Audio conversion failed for '+job.name);
}
console.log('Four generated clips imported; remaining short game cues use original synthesis.');

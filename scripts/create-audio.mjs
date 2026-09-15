import {mkdirSync,writeFileSync} from 'node:fs';
import {fileURLToPath} from 'node:url';
const out=fileURLToPath(new URL('../unity/Assets/Dive/Resources/Audio/',import.meta.url));
mkdirSync(out,{recursive:true});
const rate=24000, tau=2*Math.PI;
let seed=839; const noise=()=>{seed=(Math.imul(seed,1664525)+1013904223)>>>0;return seed/2147483648-1;};
function save(name,duration,fn,loop=false){
 const n=Math.round(duration*rate), samples=new Float64Array(n);let low=0;
 for(let i=0;i<n;i++){low+=.065*(noise()-low);samples[i]=fn(i/rate,low);}
 if(loop){const k=1200;for(let i=0;i<k;i++){const u=i/k;samples[i]=samples[n-k+i]*(1-u)+samples[i]*u;}samples[0]=samples[n-1];}
 let peak=0;for(const x of samples)peak=Math.max(peak,Math.abs(x));
 const b=Buffer.alloc(44+n*2);b.write('RIFF');b.writeUInt32LE(b.length-8,4);b.write('WAVEfmt ',8);b.writeUInt32LE(16,16);b.writeUInt16LE(1,20);b.writeUInt16LE(1,22);b.writeUInt32LE(rate,24);b.writeUInt32LE(rate*2,28);b.writeUInt16LE(2,32);b.writeUInt16LE(16,34);b.write('data',36);b.writeUInt32LE(n*2,40);
 for(let i=0;i<n;i++){const fade=loop?1:Math.min(1,i/120,(n-1-i)/600);b.writeInt16LE(Math.round(samples[i]/Math.max(peak,.001)*.7*fade*32767),44+i*2);}
 writeFileSync(out+name+'.wav',b);console.log(name, duration+'s',b.length+' bytes');
}
save('Ambience',8,(t,n)=>n*.6+Math.sin(tau*53*t)*.025+Math.sin(tau*79*t)*.02,true);
save('Swim',2,(t,n)=>n*(.12+.45*Math.pow(Math.sin(Math.PI*t),4)),true);
save('Shot',.45,(t,n)=>Math.exp(-t*20)*(Math.sin(tau*(160*t-85*t*t))*.7+n*2));
save('Pass',.35,(t,n)=>Math.exp(-t*18)*(Math.sin(tau*230*t)*.4+n*1.2));
save('Impact',.25,(t,n)=>Math.exp(-t*30)*(Math.sin(tau*95*t)*.65+n));
save('Pickup',.28,t=>Math.sin(tau*(430*t+330*t*t))*Math.exp(-t*14));
save('Start',.65,t=>Math.sin(tau*(t<.2?440:660)*t)*Math.exp(-4*(t%.2))*.4);
for(const [name,notes] of [['Goal',[392,494,587]],['Concede',[330,294,220]],['Win',[392,494,587,784]],['Lose',[330,294,247,196]]])save(name,1.6,t=>{let x=0;notes.forEach((f,i)=>{const s=t-i*.18;if(s>=0)x+=(Math.sin(tau*f*s)+.2*Math.sin(tau*f*2*s))*Math.exp(-s*5);});return x;});

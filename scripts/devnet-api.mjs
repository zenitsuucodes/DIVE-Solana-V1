import {randomUUID,randomBytes} from 'node:crypto';
import {appendFile} from 'node:fs/promises';
import {status,submitEvent,checkSignatures,recordReceipt} from './devnet.mjs';
const audit=entry=>appendFile(new URL('../artifacts/devnet-errors.jsonl',import.meta.url),JSON.stringify({time:new Date().toISOString(),...entry})+'\n').catch(()=>{});
export function createApi({getStatus=status,sendEvent=submitEvent,lookup=checkSignatures,saveReceipt=recordReceipt,logError=audit}={}){
const sessions=new Map();let queue=Promise.resolve();let pending=0;let cached=null,healthPromise=null;
async function networkStatus(){
 if(cached&&Date.now()-cached.time<15000)return cached;
 if(!healthPromise)healthPromise=getStatus().then(s=>cached={...s,time:Date.now()}).finally(()=>{healthPromise=null;});
 return healthPromise;
}
function recheck(session){
 if(session.checking||Date.now()<(session.nextCheck||0))return;
 const rows=session.rows.filter(r=>r.signature&&['unconfirmed','failed'].includes(r.status));if(!rows.length)return;
 session.nextCheck=Date.now()+15000;
 session.checking=(async()=>{
  try{const results=await lookup(rows.map(r=>r.signature));
   for(let i=0;i<rows.length;i++){const value=results[i],row=rows[i];
    if(value?.err){row.status='failed';row.error='Transaction rejected on Devnet: '+JSON.stringify(value.err);}
    else if(value&&['confirmed','finalized'].includes(value.confirmationStatus)){
     row.status='confirmed';row.slot=value.slot;delete row.error;delete row.message;
     await saveReceipt({match:row.match,seq:row.seq,type:row.type,actor:row.actor,blue:row.blue,coral:row.coral},{signature:row.signature,slot:value.slot});
    }
   }
  }catch(error){await logError({phase:'reconcile',error:error.message});}
 })().finally(()=>{session.checking=null;});
}
const types=new Set(['match_start','kickoff','shot','pass','steal','goal','match_end']);
function json(res,code,data){res.writeHead(code,{'content-type':'application/json','cache-control':'no-store'});res.end(JSON.stringify(data));}
async function body(req){let data='';for await(const chunk of req){data+=chunk;if(data.length>2048)throw Error('Request too large');}return JSON.parse(data);}
return async function api(req,res){
 if(!req.url.startsWith('/api/'))return false;
 try{
  if(req.headers.host!=='127.0.0.1:5186'||(req.headers.origin&&req.headers.origin!=='http://127.0.0.1:5186')){json(res,403,{error:'Origin rejected'});return true;}
  if(req.url==='/api/status'&&req.method==='GET'){
   json(res,200,await networkStatus());return true;
  }
  if(req.url==='/api/session'&&req.method==='POST'){
   for(const [key,s]of sessions)if(Date.now()-s.created>7200000)sessions.delete(key);
   if(sessions.size>=100)throw Error('Session capacity reached');
   const token=randomBytes(24).toString('hex');sessions.set(token,{created:Date.now(),matches:new Map(),rows:[]});json(res,200,{token});return true;
  }
  const session=sessions.get(req.headers.authorization?.replace('Bearer ',''));if(!session){json(res,401,{error:'Start a new session'});return true;}
  if(req.url==='/api/events'&&req.method==='GET'){recheck(session);json(res,200,session.rows.slice(-30));return true;}
  if(req.url==='/api/events'&&req.method==='POST'){
   const e=await body(req);if(!types.has(e.type)||!Number.isInteger(e.seq)||e.seq<1||e.seq>200||!Number.isInteger(e.actor)||e.actor<0||e.actor>5||![e.blue,e.coral].every(n=>Number.isInteger(n)&&n>=0&&n<=3)||typeof e.match!=='string'||!/^\w{1,40}$/.test(e.match))throw Error('Invalid match event');
   let match=session.matches.get(e.match);
   if(!match){if(e.type!=='match_start'||e.seq!==1||session.matches.size>=20)throw Error('Invalid match start');match={id:randomUUID(),last:0,ended:false};session.matches.set(e.match,match);}
   const duplicate=session.rows.find(r=>r.clientMatch===e.match&&r.seq===e.seq);if(duplicate){json(res,200,duplicate);return true;}
   if(match.ended||e.seq!==match.last+1)throw Error('Event order rejected');if(pending>=80)throw Object.assign(Error('Transaction queue busy; retrying shortly'),{temporary:true});
   const network=await networkStatus();if(!network.ready)throw Error(network.active?'Sponsor needs free Devnet SOL':'V1 unavailable on Devnet');
   // Recheck after the network await to prevent duplicate concurrent admissions.
   const admitted=session.rows.find(r=>r.clientMatch===e.match&&r.seq===e.seq);
   if(admitted){json(res,200,admitted);return true;}
   if(match.ended||e.seq!==match.last+1||pending>=80)throw Error('Event order or capacity changed');
   match.last=e.seq;if(e.type==='match_end')match.ended=true;
   const row={id:randomUUID(),clientMatch:e.match,match:match.id,seq:e.seq,type:e.type,actor:e.actor,blue:e.blue,coral:e.coral,status:'queued',signature:null};session.rows.push(row);pending++;
   queue=queue.then(async()=>{row.status='sending';try{const result=await sendEvent({match:match.id,seq:e.seq,type:e.type,actor:e.actor,blue:e.blue,coral:e.coral},sig=>{row.signature=sig;row.status='submitted';},message=>{row.message=message;});Object.assign(row,result,{status:'confirmed'});delete row.message;delete row.error;}catch(error){row.status=error.definitive?'failed':row.signature?'unconfirmed':'failed';row.error=error.message;await logError({phase:'submit',event:{match:row.match,seq:row.seq,type:row.type},signature:row.signature,status:row.status,error:row.error});}finally{pending--;}});
   json(res,202,row);return true;
  }
  json(res,404,{error:'Unknown endpoint'});
 }catch(error){json(res,error.temporary?503:400,{error:error.message,temporary:!!error.temporary});}return true;
}

}
export const api=createApi();

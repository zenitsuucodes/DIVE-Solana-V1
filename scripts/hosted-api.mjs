import {createHmac,randomUUID,timingSafeEqual} from 'node:crypto';
import {status,prepareEvent} from './devnet.mjs';
import {createRpc} from './devnet-transport.mjs';

const types=new Set(['match_start','kickoff','pass','shot','steal','goal','match_end']);
const fail=(message,code=400)=>Object.assign(Error(message),{code});
// No receipt database or background jobs: every request completes its own work.
export function createHostedApi({secret=()=>process.env.DIVE_SESSION_SECRET,getStatus=status,prepare=prepareEvent,rpc=createRpc({attempts:1}),now=Date.now,origin=()=>process.env.DIVE_ORIGIN||'https://dive-unity.vercel.app'}={}){
 let cached,checking;
 const mac=text=>{
  const key=secret();if(!key||key.length<32)throw fail('Devnet backend is not configured',503);
  return createHmac('sha256',key).update(text).digest('base64url');
 };
 const seal=data=>{const text=Buffer.from(JSON.stringify(data)).toString('base64url');return text+'.'+mac(text);};
 const unseal=(token,kind)=>{
  if(typeof token!=='string'||token.length>12000)throw fail('Invalid session',401);
  const [text,sig,...extra]=token.split('.'),expected=mac(text||'');
  if(extra.length||!sig||sig.length!==expected.length||!timingSafeEqual(Buffer.from(sig),Buffer.from(expected)))throw fail('Invalid session',401);
  let data;try{data=JSON.parse(Buffer.from(text,'base64url'));}catch{throw fail('Invalid session',401);}
  if(data.kind!==kind||!Number.isFinite(data.exp)||data.exp<now())throw fail('Session expired; reload to reconnect',401);
  return data;
 };
 async function health(){
  if(cached&&cached.until>now())return cached.value;
  if(!checking)checking=getStatus().then(value=>{cached={value,until:now()+15000};return value;}).finally(()=>checking=null);
  return checking;
 }
 const json=(res,code,data)=>{res.writeHead(code,{'content-type':'application/json','cache-control':'no-store'});res.end(JSON.stringify(data));};
 return async function handler(req,res){
  try{
   const path=new URL(req.url,'http://localhost').pathname.replace(/\/$/,'');
   if(!['GET','POST'].includes(req.method))throw fail('Method not allowed',405);
   if(req.headers.origin&&req.headers.origin!==origin())throw fail('Origin rejected',403);
   if(req.method==='POST'&&req.headers.origin!==origin())throw fail('Origin required',403);
   if(req.method==='POST'&&!req.headers['content-type']?.startsWith('application/json'))throw fail('JSON required',415);
   if(path==='/api/status'&&req.method==='GET'){mac('configuration-check');return json(res,200,await health());}
   let body={};
   if(req.method==='POST'){
    if(Number(req.headers['content-length'])>16000)throw fail('Request too large',413);
    if(req.body!==undefined){body=typeof req.body==='string'?JSON.parse(req.body):req.body;}
    else {let raw='';for await(const chunk of req){raw+=chunk;if(raw.length>16000)throw fail('Request too large',413);}body=JSON.parse(raw||'{}');}
    if(!body||Array.isArray(body)||JSON.stringify(body).length>16000)throw fail('Invalid request');
   }
   if(path==='/api/session'&&req.method==='POST')return json(res,200,{token:seal({kind:'session',id:randomUUID(),exp:now()+7200000})});
   const session=unseal(req.headers.authorization?.replace(/^Bearer /,''),'session');
   if(path==='/api/prepare'&&req.method==='POST'){
    const e=body;
    if(!types.has(e.type)||!Number.isInteger(e.seq)||e.seq<1||e.seq>200||!Number.isInteger(e.actor)||e.actor<0||e.actor>5||![e.blue,e.coral].every(n=>Number.isInteger(n)&&n>=0&&n<=3)||typeof e.match!=='string'||!/^\w{1,40}$/.test(e.match))throw fail('Invalid match event');
    const h=await health();if(!h.ready)throw fail(h.active?'Sponsor needs Devnet SOL':'V1 unavailable on Devnet',503);
    const event={match:session.id+':'+e.match,seq:e.seq,type:e.type,actor:e.actor,blue:e.blue,coral:e.coral};
    // Preparing never broadcasts. Only the response the browser actually receives can be submitted.
    const tx=await prepare(event);
    return json(res,200,{signature:tx.signature,ticket:seal({kind:'transaction',session:session.id,...tx,exp:now()+180000})});
   }
   if(path==='/api/send'&&req.method==='POST'){
    const tx=unseal(body.ticket,'transaction');if(tx.session!==session.id)throw fail('Session mismatch',403);
    try{
     const signature=await rpc('sendTransaction',[tx.wire,{encoding:'base64',preflightCommitment:'confirmed',maxRetries:3}]);
     if(signature!==tx.signature)throw Error('Signature mismatch');
     return json(res,200,{signature:tx.signature,status:'submitted'});
    }catch{
     // A preflight failure on a replay does not prove that the first send failed.
     // Only an on-chain error can mark this known signature failed.
     return json(res,202,{signature:tx.signature,status:'unconfirmed',message:'Checking the existing transaction on Devnet'});
    }
   }
   if(path==='/api/confirm'&&req.method==='POST'){
    if(!Array.isArray(body.signatures)||body.signatures.length>5||!body.signatures.every(s=>typeof s==='string'&&/^[1-9A-HJ-NP-Za-km-z]{64,88}$/.test(s)))throw fail('Invalid signatures');
    if(!body.signatures.length)return json(res,200,[]);
    const values=(await rpc('getSignatureStatuses',[body.signatures,{searchTransactionHistory:true}])).value;
    return json(res,200,values.map((v,i)=>({signature:body.signatures[i],status:v?.err?'failed':v&&['confirmed','finalized'].includes(v.confirmationStatus)?'confirmed':'unconfirmed',...(v?.err?{error:'Transaction rejected on Devnet'}:{}),...(v?.slot?{slot:v.slot}:{})})));
   }
   throw fail('Unknown endpoint',404);
  }catch(error){json(res,error.code||503,{error:error.code?error.message:'Devnet is busy; retry shortly',temporary:!error.code||error.code===503});}
 };
}
export const hostedApi=createHostedApi();

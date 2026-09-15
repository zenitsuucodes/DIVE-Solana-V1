import test from 'node:test';
import assert from 'node:assert/strict';
import {Readable} from 'node:stream';
import {createHostedApi} from './hosted-api.mjs';
const signature='5'.repeat(88),event={match:'test',seq:1,type:'match_start',actor:0,blue:0,coral:0};
function setup(overrides={}){
 const sent=[];
 const handler=createHostedApi({secret:()=> 'test-secret-only-'.repeat(4),getStatus:async()=>({ready:true,active:true}),prepare:async()=>({signature,wire:'signed-bytes',lastValidBlockHeight:100}),rpc:async(method,args)=>{if(method==='sendTransaction'){sent.push(args[0]);return signature;}return {value:[{confirmationStatus:'confirmed',slot:20,err:null}]};},...overrides});
 async function call(path,body={},token,method='POST',origin='https://dive-unity.vercel.app'){
  const req=Readable.from([JSON.stringify(body)]);Object.assign(req,{url:'/api/'+path,method,headers:{origin,'content-type':'application/json',authorization:token?'Bearer '+token:undefined}});
  let code,data;await handler(req,{writeHead(c){code=c;},end(s){data=JSON.parse(s);}});return {code,data};
 }
 return {call,sent};
}
test('stateless sessions and signed tickets survive cold starts; retries broadcast identical bytes',async()=>{
 const a=setup(),b=setup();const token=(await a.call('session')).data.token;
 const prepared=await a.call('prepare',event,token);assert.equal(prepared.code,200);assert.equal(a.sent.length,0);
 await b.call('send',{ticket:prepared.data.ticket},token);await b.call('send',{ticket:prepared.data.ticket},token);
 assert.deepEqual(b.sent,['signed-bytes','signed-bytes']);
 assert.equal((await b.call('confirm',{signatures:[signature]},token)).data[0].status,'confirmed');
});
test('rejects cross-origin requests, tampered tickets, foreign sessions and invalid events',async()=>{
 const a=setup();assert.equal((await a.call('session',{},null,'POST','https://evil.example')).code,403);
 const token=(await a.call('session')).data.token,other=(await a.call('session')).data.token;
 assert.equal((await a.call('prepare',{...event,type:'transfer'},token)).code,400);
 const {ticket}=(await a.call('prepare',event,token)).data;
 assert.equal((await a.call('send',{ticket:ticket+'x'},token)).code,401);
 assert.equal((await a.call('send',{ticket},other)).code,403);assert.equal(a.sent.length,0);
});
test('ambiguous send is reconciled from the chain, not incorrectly marked failed',async()=>{
 const a=setup({rpc:async(method)=>{if(method==='sendTransaction')throw Error('lost response');return {value:[{confirmationStatus:'finalized',slot:22,err:null}]};}});
 const token=(await a.call('session')).data.token,{ticket}=(await a.call('prepare',event,token)).data;
 assert.equal((await a.call('send',{ticket},token)).data.status,'unconfirmed');
 assert.equal((await a.call('confirm',{signatures:[signature]},token)).data[0].status,'confirmed');
});
test('missing secrets, expired sessions, unfunded sponsor and oversized confirmation batches fail closed',async()=>{
 assert.equal((await setup({secret:()=>undefined}).call('session')).code,503);
 let time=100;const a=setup({now:()=>time});const token=(await a.call('session')).data.token;
 assert.equal((await a.call('confirm',{signatures:Array(6).fill(signature)},token)).code,400);
 time+=7200001;assert.equal((await a.call('prepare',event,token)).code,401);
 const b=setup({getStatus:async()=>({active:true,ready:false})}),t=(await b.call('session')).data.token;
 assert.equal((await b.call('prepare',event,t)).code,503);assert.equal(b.sent.length,0);
});

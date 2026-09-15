import test from 'node:test';
import assert from 'node:assert/strict';
import {Readable} from 'node:stream';
import {createApi} from './devnet-api.mjs';
function setup(overrides={}){
 const sent=[];const api=createApi({getStatus:async()=>({ready:true,active:true}),lookup:async signatures=>signatures.map(()=>null),saveReceipt:async()=>{},logError:async()=>{},sendEvent:async(e,submitted)=>{sent.push(e);submitted('test-signature');return {slot:1};},...overrides});
 async function call(url,method='GET',payload={},token,headers={}){
  const req=Readable.from([JSON.stringify(payload)]);Object.assign(req,{url:'/api/'+url,method,headers:{host:'127.0.0.1:5186',...(token?{authorization:'Bearer '+token}:{}),...headers}});
  let code,data;await api(req,{writeHead(c){code=c;},end(s){data=JSON.parse(s);}});return {code,data};
 }
 return {sent,call};
}
const event={match:'testmatch',seq:1,type:'match_start',actor:0,blue:0,coral:0};
test('rejects untrusted origins, missing sessions and malformed events',async()=>{
 const {call,sent}=setup();assert.equal((await call('session','POST',{},null,{origin:'https://example.com'})).code,403);
 assert.equal((await call('events','POST',event)).code,401);
 const {data:{token}}=await call('session','POST');
 assert.equal((await call('events','POST',{...event,actor:99},token)).code,400);
 assert.equal((await call('events','POST',{...event,type:'transfer'},token)).code,400);assert.equal(sent.length,0);
});
test('a late confirmed receipt repairs the feed without submitting again',async()=>{
 let sends=0,saved=0,health=0;
 const {call}=setup({getStatus:async()=>{health++;return {ready:true};},sendEvent:async(e,submitted)=>{sends++;submitted('late');throw Error('Polling interrupted');},lookup:async()=>[{err:null,confirmationStatus:'finalized',slot:42}],saveReceipt:async()=>{saved++;}});
 const token=(await call('session','POST')).data.token;
 await call('events','POST',event,token);await new Promise(r=>setImmediate(r));
 await call('events','GET',{},token);await new Promise(r=>setImmediate(r));
 const rows=(await call('events','GET',{},token)).data;
 assert.equal(rows[0].status,'confirmed');assert.equal(rows[0].slot,42);assert.equal(rows[0].error,undefined);assert.equal(sends,1);assert.equal(saved,1);
 await call('status');await call('status');assert.equal(health,1);
});
test('concurrent duplicate events submit only once and preserve order',async()=>{
 const {call,sent}=setup({getStatus:async()=>{await new Promise(r=>setTimeout(r,5));return {ready:true};}});
 const {data:{token}}=await call('session','POST');
 const results=await Promise.all([call('events','POST',event,token),call('events','POST',event,token)]);
 assert.deepEqual(results.map(r=>r.code).sort(),[200,202]);
 assert.equal((await call('events','POST',{...event,seq:3,type:'shot'},token)).code,400);
 await call('events','POST',{...event,seq:2,type:'match_end'},token);
 assert.equal((await call('events','POST',{...event,seq:3,type:'shot'},token)).code,400);
 await new Promise(r=>setImmediate(r));assert.equal(sent.length,2);assert.notEqual(sent[0].match,event.match);
});
test('unfunded sponsor never sends; ambiguous sends stay unconfirmed',async()=>{
 const a=setup({getStatus:async()=>({ready:false,active:true})});let token=(await a.call('session','POST')).data.token;
 assert.equal((await a.call('events','POST',event,token)).code,400);assert.equal(a.sent.length,0);
 const b=setup({sendEvent:async(e,submitted)=>{submitted('known-signature');throw Error('Connection interrupted');}});token=(await b.call('session','POST')).data.token;
 await b.call('events','POST',event,token);await new Promise(r=>setImmediate(r));
 const rows=(await b.call('events','GET',{},token)).data;assert.equal(rows[0].status,'unconfirmed');assert.equal(rows[0].signature,'known-signature');
});

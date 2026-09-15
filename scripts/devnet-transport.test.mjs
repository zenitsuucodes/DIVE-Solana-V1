import test from 'node:test';
import assert from 'node:assert/strict';
import {createRpc,confirmSignedTransaction} from './devnet-transport.mjs';
const sleep=async()=>{};
test('rate limiting and non-JSON temporary errors recover with backoff',async()=>{
 const waits=[];let calls=0;
 const rpc=createRpc({sleep:async ms=>{waits.push(ms);},fetchFn:async()=>{
  calls++;if(calls===1)return new Response('Rate limited',{status:429,headers:{'retry-after':'2'}});
  if(calls===2)return new Response('Gateway unavailable',{status:502});
  return Response.json({result:{value:12}});
 }});
 assert.deepEqual(await rpc('getBalance',[]),{value:12});assert.equal(calls,3);assert.ok(waits.includes(2000));
});
test('definitive RPC rejection is not retried',async()=>{
 let calls=0;const rpc=createRpc({sleep,fetchFn:async()=>{calls++;return Response.json({error:{code:-32602,message:'Invalid params'}});}});
 await assert.rejects(rpc('getBalance'),{message:'Invalid params',definitive:true});assert.equal(calls,1);
});
test('lost send response and interrupted polling still find confirmation',async()=>{
 let polls=0,sends=0;const rpc=async method=>{
  if(method==='sendTransaction'){sends++;throw Object.assign(Error('timeout'),{temporary:true});}
  polls++;if(polls===1)throw Error('lookup interrupted');
  return {value:[{err:null,confirmationStatus:'confirmed',slot:12}]};
 };
 assert.deepEqual(await confirmSignedTransaction({rpc,wire:'same-bytes',signature:'sig',sleep,polls:3}),{signature:'sig',slot:12});assert.equal(sends,1);
});
test('resends identical bytes and never invents confirmation',async()=>{
 const wires=[];const rpc=async(method,params)=>{if(method==='sendTransaction'){wires.push(params[0]);return 'sig';}return {value:[null]};};
 await assert.rejects(confirmSignedTransaction({rpc,wire:'signed-once',signature:'sig',sleep,polls:8}),/Confirmation delayed/);
 assert.deepEqual(wires,['signed-once','signed-once']);
});
test('actual on-chain errors remain failures',async()=>{
 const rpc=async method=>method==='sendTransaction'?'sig':{value:[{err:{InstructionError:[0,'InvalidArgument']},confirmationStatus:'confirmed'}]};
 await assert.rejects(confirmSignedTransaction({rpc,wire:'bytes',signature:'sig',sleep}),{definitive:true});
});

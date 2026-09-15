// Explicit live Devnet smoke test; never runs as part of the unit suite.
const base=process.argv[2];if(!base)throw Error('Supply the game origin');
let token;
async function post(path,body){
 const r=await fetch(base+'/api/'+path,{method:'POST',headers:{origin:base,'content-type':'application/json',...(token?{authorization:'Bearer '+token}:{})},body:JSON.stringify(body)});
 const data=await r.json();if(!r.ok)throw Error(path+': '+JSON.stringify(data));return data;
}
token=(await post('session',{})).token;
const signatures=[];let seq=0;const match='smoke'+Date.now();
for(const type of ['match_start','pass','steal']){
 const tx=await post('prepare',{match,seq:++seq,type,actor:0,blue:0,coral:0});
 await post('send',{ticket:tx.ticket});
 // Deliberately replay a send: it must keep the same signature.
 const replay=await post('send',{ticket:tx.ticket});if(replay.signature!==tx.signature)throw Error('Duplicate signature changed');
 signatures.push(tx.signature);console.log(type,tx.signature);
}
for(let i=0;i<20;i++){
 const rows=await post('confirm',{signatures});
 if(rows.some(r=>r.status==='failed'))throw Error('An event failed on chain');
 if(rows.every(r=>r.status==='confirmed')){console.log('All three events confirmed; replay used identical transactions.');process.exit(0);}
 await new Promise(r=>setTimeout(r,3000));
}
throw Error('Confirmation still pending');

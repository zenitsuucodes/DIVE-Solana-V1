const panel=document.createElement('aside');panel.id='chain';panel.innerHTML='<b>SOLANA DEVNET · V1</b><p id="chain-status">Checking connection…</p><div id="chain-rows"></div><small>Game event receipts · Esc to inspect</small>';document.body.append(panel);
const style=document.createElement('style');style.textContent='#chain{position:fixed;right:18px;top:16px;width:260px;max-height:285px;padding:14px;background:#052b38e8;border:1px solid #75eed066;border-radius:12px;font-size:12px;z-index:3;box-shadow:0 6px 24px #0004}#chain b{color:#a2fac9;letter-spacing:1px}#chain p{margin:7px 0}#chain-rows{max-height:190px;overflow:auto}#chain-rows a,#chain-rows span{display:block;padding:6px 0;border-bottom:1px solid #ffffff20;color:#e8fffa;text-decoration:none}#chain-rows a:hover{text-decoration:underline}#chain small{display:block;margin-top:9px;color:#a9c5ca}#chain-rows .receipt-detail{font-size:10px;color:#f3cb93;margin-top:3px;line-height:1.4;overflow-wrap:anywhere}';document.head.append(style);
let token,rows=[],localError='',serial=Promise.resolve();
const labels={match_start:'Match started',kickoff:'Kickoff',shot:'Shot',pass:'Pass',steal:'Puck stolen',goal:'Goal',match_end:'Match finished'};
async function request(url,options={}){
 for(let attempt=0;attempt<5;attempt++){
  try{
   const r=await fetch('/api/'+url,{...options,signal:AbortSignal.timeout(15000),headers:{'content-type':'application/json',...(token?{authorization:'Bearer '+token}:{}),...options.headers}});
   const data=await r.json();if(!r.ok)throw Object.assign(Error(data.error||'Connection unavailable'),{retryable:r.status>=500||r.status===429||data.temporary});return data;
  }catch(error){
   const retryable=error.retryable??(error instanceof TypeError||error.name==='TimeoutError'||error.name==='AbortError');
   if(!retryable||attempt===4)throw error;
   document.querySelector('#chain-status').textContent='Connection delayed · Retrying the same event';
   await new Promise(resolve=>setTimeout(resolve,Math.min(8000,1000*2**attempt)));
  }
 }
}
const staticPreview=window.DIVE_STATIC_PREVIEW===true;
const hosted=window.DIVE_HOSTED_API===true;
const seen=new Map();
const session=staticPreview?Promise.resolve():request('session',{method:'POST',body:'{}'}).then(s=>{token=s.token;});session.catch(e=>{localError=e.message;});
window.diveEvent=payload=>{
 if(staticPreview)return;
 const event=JSON.parse(payload);
 if(hosted){
  const key=event.match+':'+event.seq;if(seen.has(key))return;
  seen.set(key,true);if(seen.size>4000)seen.delete(seen.keys().next().value);
  const row={...event,status:'queued'};rows.push(row);rows=rows.slice(-5);render();
  serial=serial.then(async()=>{
   try{
    await session;
    const prepared=await request('prepare',{method:'POST',body:JSON.stringify(event)});
    row.signature=prepared.signature;row.ticket=prepared.ticket;row.preparedAt=Date.now();row.status='sending';render();
    await transmit(row);localError='';
   }catch(e){row.status=row.signature?'unconfirmed':'failed';row.error=e.message;localError=labels[event.type]+': '+e.message;}
   render();
  });return;
 }
 serial=serial.then(async()=>{
  try{await session;await request('events',{method:'POST',body:JSON.stringify(event)});localError='';await refresh();}
  catch(e){localError=labels[event.type]+': '+e.message;document.querySelector('#chain-status').textContent=localError;}
 });
};
async function refresh(){
 try{
  if(token&&hosted){
   const pending=rows.filter(row=>row.signature&&!['confirmed','failed'].includes(row.status));
   if(pending.length){
    const updates=await request('confirm',{method:'POST',body:JSON.stringify({signatures:pending.map(r=>r.signature)})});
    for(const update of updates){
     const row=pending.find(r=>r.signature===update.signature);Object.assign(row,update);
     if(row.status==='confirmed'){delete row.error;delete row.message;delete row.ticket;}
     else if(row.ticket&&Date.now()-row.preparedAt<150000&&Date.now()-(row.sentAt||0)>12000)await transmit(row);
     else if(row.status==='unconfirmed'&&Date.now()-row.preparedAt>=150000){delete row.ticket;row.message='Confirmation delayed · Check Solscan';}
    }
   }
  }else if(token)rows=await request('events');
  render();
 }catch(e){localError=e.message;}
}
async function transmit(row){
 if(row.sending)return;row.sending=true;row.sentAt=Date.now();
 try{const sent=await request('send',{method:'POST',body:JSON.stringify({ticket:row.ticket})});if(row.status!=='confirmed')Object.assign(row,sent);}
 finally{row.sending=false;}
}
function render(){
  const list=document.querySelector('#chain-rows');const scroll=list.scrollTop;list.replaceChildren();
  for(const row of rows.slice(-5).reverse()){
   const el=document.createElement(row.signature?'a':'span');
   const status=row.status==='unconfirmed'?'awaiting confirmation':row.status==='failed'&&!row.signature?'not sent':row.status;
   el.textContent=(row.actor<3?'Blue · ':'Coral · ')+labels[row.type]+' — '+status;
   const detail=row.error||row.message;
   if(detail&&row.status!=='confirmed'){el.title=detail;const note=document.createElement('div');note.className='receipt-detail';note.textContent=detail;el.append(note);}
   if(row.signature){el.href='https://solscan.io/tx/'+encodeURIComponent(row.signature)+'?cluster=devnet';el.target='_blank';el.rel='noopener noreferrer';}
   list.append(el);
  }
  list.scrollTop=scroll;
}
async function health(){try{const s=await request('status');document.querySelector('#chain-status').textContent=localError||(s.ready?'Connected · Sponsored transactions':s.active?'Awaiting sponsor Devnet SOL':'V1 unavailable');}catch{document.querySelector('#chain-status').textContent='Devnet connection unavailable';}}
if(staticPreview){document.querySelector('#chain-status').textContent='Game ready · Devnet server not configured';}else{
let refreshing=false;setInterval(async()=>{if(refreshing)return;refreshing=true;try{await refresh();}finally{refreshing=false;}},3000);setInterval(health,15000);health();

}

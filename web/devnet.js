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
const session=staticPreview?Promise.resolve():request('session',{method:'POST',body:'{}'}).then(s=>{token=s.token;});session.catch(e=>{localError=e.message;});
window.diveEvent=payload=>{
 if(staticPreview)return;
 const event=JSON.parse(payload);
 serial=serial.then(async()=>{
  try{await session;await request('events',{method:'POST',body:JSON.stringify(event)});localError='';await refresh();}
  catch(e){localError=labels[event.type]+': '+e.message;document.querySelector('#chain-status').textContent=localError;}
 });
};
async function refresh(){
 try{
  if(token)rows=await request('events');
  const list=document.querySelector('#chain-rows');const scroll=list.scrollTop;list.replaceChildren();
  for(const row of rows.slice(-30).reverse()){
   const el=document.createElement(row.signature?'a':'span');
   const status=row.status==='unconfirmed'?'awaiting confirmation':row.status==='failed'&&!row.signature?'not sent':row.status;
   el.textContent=(row.actor<3?'Blue · ':'Coral · ')+labels[row.type]+' — '+status;
   const detail=row.error||row.message;
   if(detail&&row.status!=='confirmed'){el.title=detail;const note=document.createElement('div');note.className='receipt-detail';note.textContent=detail;el.append(note);}
   if(row.signature){el.href='https://solscan.io/tx/'+encodeURIComponent(row.signature)+'?cluster=devnet';el.target='_blank';el.rel='noopener noreferrer';}
   list.append(el);
  }
  list.scrollTop=scroll;
 }catch(e){localError=e.message;}
}
async function health(){try{const s=await request('status');document.querySelector('#chain-status').textContent=localError||(s.ready?'Connected · Sponsored transactions':s.active?'Awaiting sponsor Devnet SOL':'V1 unavailable');}catch{document.querySelector('#chain-status').textContent='Devnet connection unavailable';}}
if(staticPreview){document.querySelector('#chain-status').textContent='Game ready · Devnet server not configured';}else{
let refreshing=false;setInterval(async()=>{if(refreshing)return;refreshing=true;try{await refresh();}finally{refreshing=false;}},3000);setInterval(health,15000);health();

}

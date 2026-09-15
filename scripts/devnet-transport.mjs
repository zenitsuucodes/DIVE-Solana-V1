const pause=ms=>new Promise(resolve=>setTimeout(resolve,ms));
export function createRpc({fetchFn=fetch,sleep=pause,spacing=350,attempts=4}={}){
 let serial=Promise.resolve();
 return function rpc(method,params=[]){
  const result=serial.then(async()=>{
   for(let attempt=0;attempt<attempts;attempt++){
    await sleep(spacing);
    let error,retryAfter=0;
    try{
     const response=await fetchFn('https://api.devnet.solana.com',{method:'POST',headers:{'content-type':'application/json'},body:JSON.stringify({jsonrpc:'2.0',id:1,method,params}),signal:AbortSignal.timeout(15000)});
     const raw=await response.text();let data;try{data=JSON.parse(raw);}catch{}
     if(response.ok&&!data?.error&&data&&'result' in data)return data.result;
     const temporary=[408,429,500,502,503,504].includes(response.status)||[-32004,-32005,-32007,-32009].includes(data?.error?.code);
     error=Object.assign(new Error(data?.error?.message||`Devnet RPC HTTP ${response.status}`),{temporary,definitive:!temporary,rpcCode:data?.error?.code,httpStatus:response.status});
     const retry=response.headers.get('retry-after');retryAfter=retry?Math.max(0,Number(retry)*1000||Date.parse(retry)-Date.now()):0;
    }catch(cause){error=Object.assign(new Error('Devnet network request interrupted'),{temporary:true,cause});}
    if(!error.temporary||attempt===attempts-1)throw error;
    await sleep(Math.min(30000,Math.max(retryAfter,1000*2**attempt)));
   }
  });
  serial=result.catch(()=>{});return result;
 };
}

// Retransmit exactly the same signed bytes. Never create a second payment/receipt on an ambiguous send.
export async function confirmSignedTransaction({rpc,wire,signature,sleep=pause,polls=45,onProgress=()=>{}}){
 let lastError,observed=false,attempted=false;
 const send=async()=>{
  const first=!attempted;attempted=true;
  try{await rpc('sendTransaction',[wire,{encoding:'base64',preflightCommitment:'confirmed',maxRetries:3}]);}
  catch(error){lastError=error;if(error.definitive&&first)throw error;onProgress('Network interrupted; checking the existing signature');}
 };
 await send();
 for(let i=0;i<polls;i++){
  await sleep(2000);
  let value;
  try{value=(await rpc('getSignatureStatuses',[[signature],{searchTransactionHistory:true}])).value[0];}
  catch(error){lastError=error;onProgress('Devnet confirmation check delayed; retrying');continue;}
  if(value?.err)throw Object.assign(new Error('Transaction rejected on Devnet: '+JSON.stringify(value.err)),{definitive:true});
  if(value){observed=true;if(['confirmed','finalized'].includes(value.confirmationStatus))return {signature,slot:value.slot};}
  if(!observed&&i>0&&i%5===0)await send();
 }
 throw Object.assign(new Error(lastError?.message||'Confirmation delayed; follow-up checks will continue'),{temporary:true});
}

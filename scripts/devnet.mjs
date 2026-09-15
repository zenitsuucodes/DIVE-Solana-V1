import {randomBytes} from 'node:crypto';
import {readFile,writeFile,appendFile,mkdir} from 'node:fs/promises';
import * as k from '@solana/kit';
import {createRpc,confirmSignedTransaction} from './devnet-transport.mjs';
const seedFile=new URL('../.env.devnet-seed',import.meta.url);
export const rpc=createRpc();
export async function sponsor(){
 if(process.env.DIVE_SPONSOR_SEED){
  const seed=Buffer.from(process.env.DIVE_SPONSOR_SEED,'base64');
  if(seed.length!==32)throw Error('Invalid sponsor configuration');
  return k.createKeyPairSignerFromPrivateKeyBytes(seed);
 }
 if(process.env.VERCEL)throw Error('Devnet sponsor is not configured');
 let seed;try{seed=await readFile(seedFile);}catch(e){if(e.code!=='ENOENT')throw e;seed=randomBytes(32);await writeFile(seedFile,seed,{flag:'wx',mode:0o600});}
 return k.createKeyPairSignerFromPrivateKeyBytes(seed);
}
export async function status(){
 const payer=await sponsor();const [feature,balance]=await Promise.all([rpc('getAccountInfo',['txv1aq4pp281K9um3tnPgkfX8UqtFT6wcVW3hNezGLL',{encoding:'base64'}]),rpc('getBalance',[payer.address])]);
 const active=!!feature.value&&Buffer.from(feature.value.data[0],'base64')[0]===1;
 return {cluster:'devnet',version:1,active,address:payer.address,balance:balance.value,ready:active&&balance.value>=100000};
}
export async function submitEvent(event,onSubmitted,onProgress){
 const {wire,signature}=await prepareEvent(event);
 // Preserve the signature before any network submission.
 onSubmitted(signature);
 const result=await confirmSignedTransaction({rpc,wire,signature,onProgress});
 await recordReceipt(event,result);return result;
}
export async function prepareEvent(event){
 const payer=await sponsor();const latest=(await rpc('getLatestBlockhash',[{commitment:'confirmed'}])).value;
 const memo=JSON.stringify({game:'DIVE',schema:1,...event});
 const message=k.pipe(k.createTransactionMessage({version:1}),m=>k.setTransactionMessageFeePayerSigner(payer,m),m=>k.setTransactionMessageLifetimeUsingBlockhash({...latest,lastValidBlockHeight:BigInt(latest.lastValidBlockHeight)},m),m=>k.appendTransactionMessageInstruction({programAddress:k.address('MemoSq4gqABAXKb96qnH8TysNcWxMyWCqXgDLGmfcHr'),data:new TextEncoder().encode(memo)},m),m=>k.setTransactionMessageConfig({computeUnitLimit:200000,loadedAccountsDataSizeLimit:1048576,priorityFeeLamports:0n},m));
 const tx=await k.signTransactionMessageWithSigners(message);k.assertIsTransactionWithinSizeLimit(tx);
 const wire=k.getBase64EncodedWireTransaction(tx);const signature=k.getSignatureFromTransaction(tx);
 return {wire,signature,lastValidBlockHeight:latest.lastValidBlockHeight};
}
export async function recordReceipt(event,result){await mkdir(new URL('../artifacts/',import.meta.url),{recursive:true});await appendFile(new URL('../artifacts/devnet-receipts.jsonl',import.meta.url),JSON.stringify({event,...result,version:1})+'\n');}
export async function checkSignatures(signatures){return (await rpc('getSignatureStatuses',[signatures,{searchTransactionHistory:true}])).value;}
if(process.argv.includes('--fund')){const s=await status();console.log(s);if(s.balance<100000){console.log('Devnet airdrop:',await rpc('requestAirdrop',[s.address,1000000000]));}}
if(process.argv.includes('--status'))console.log(await status());

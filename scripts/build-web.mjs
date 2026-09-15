import {cp,mkdir,readFile,writeFile} from 'node:fs/promises';
const root=new URL('../',import.meta.url);
for(const name of ['Web.data','Web.wasm','Web.framework.js','Web.loader.js']){
 const bytes=await readFile(new URL('web/player/Build/'+name,root));
 if(!bytes.length)throw Error('Missing Unity player asset: '+name);
 if(name.endsWith('.wasm')&&bytes.subarray(0,4).toString('hex')!=='0061736d')throw Error('Invalid Unity WebAssembly build');
}
await mkdir(new URL('dist/',root),{recursive:true});
await cp(new URL('web/player/Build/',root),new URL('dist/Build/',root),{recursive:true});
await cp(new URL('web/unity.html',root),new URL('dist/index.html',root));
await cp(new URL('web/devnet.js',root),new URL('dist/devnet.js',root));
console.log('Published current Unity first-person player to dist.');

await writeFile(new URL('dist/player-config.js',root),'window.DIVE_STATIC_PREVIEW='+JSON.stringify(process.env.VERCEL==='1')+';\n');

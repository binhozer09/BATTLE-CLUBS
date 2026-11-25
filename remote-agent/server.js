const http = require('node:http');
const fs = require('node:fs');
const path = require('node:path');
const crypto = require('node:crypto');

const PORT = Number(process.env.PORT || 8080);
const ADMIN_KEY = process.env.BATTLE_AGENT_ADMIN_KEY || '';
const DATA_DIR = process.env.DATA_DIR || '/data';
const DB_FILE = path.join(DATA_DIR, 'state.json');
const UPLOAD_DIR = path.join(DATA_DIR, 'uploads');
fs.mkdirSync(UPLOAD_DIR, { recursive: true });

function readState() {
  try { return JSON.parse(fs.readFileSync(DB_FILE, 'utf8')); }
  catch { return { devices: {}, commands: [], diagnostics: [] }; }
}
function writeState(state) {
  const tmp = DB_FILE + '.tmp';
  fs.writeFileSync(tmp, JSON.stringify(state, null, 2));
  fs.renameSync(tmp, DB_FILE);
}
function send(res, status, data, type='application/json') {
  res.writeHead(status, { 'Content-Type': type, 'Cache-Control': 'no-store', 'X-Content-Type-Options': 'nosniff' });
  res.end(type === 'application/json' ? JSON.stringify(data) : data);
}
function body(req, limit=12*1024*1024) {
  return new Promise((resolve, reject) => {
    const chunks=[]; let size=0;
    req.on('data', c => { size += c.length; if (size > limit) { reject(new Error('payload too large')); req.destroy(); } else chunks.push(c); });
    req.on('end', () => resolve(Buffer.concat(chunks)));
    req.on('error', reject);
  });
}
function safeEqual(a,b) {
  const aa=Buffer.from(String(a||'')), bb=Buffer.from(String(b||''));
  return aa.length===bb.length && aa.length>0 && crypto.timingSafeEqual(aa,bb);
}
function bearer(req) { return (req.headers.authorization || '').replace(/^Bearer\s+/i,''); }
function deviceFor(req, state) {
  const token=bearer(req);
  return Object.entries(state.devices).find(([,d])=>safeEqual(d.token,token));
}
const allowed = new Set(['cameraFov','targetFrameRate','moveSpeed','sprintSpeed','cameraSensitivity','shadowDistance','masterVolume']);
function validConfig(input) {
  if (!input || typeof input !== 'object' || Array.isArray(input)) return null;
  const out={};
  for (const [k,v] of Object.entries(input)) {
    if (!allowed.has(k) || typeof v !== 'number' || !Number.isFinite(v)) continue;
    const ranges={cameraFov:[40,90],targetFrameRate:[30,90],moveSpeed:[1,12],sprintSpeed:[1,16],cameraSensitivity:[0.2,5],shadowDistance:[0,150],masterVolume:[0,1]};
    const [min,max]=ranges[k]; out[k]=Math.min(max,Math.max(min,v));
  }
  return Object.keys(out).length ? out : null;
}
const server=http.createServer(async(req,res)=>{
  const url=new URL(req.url,'http://localhost');
  if (req.method==='GET' && url.pathname==='/health') return send(res,200,{ok:true,service:'battle-clubs-remote-agent',time:new Date().toISOString()});
  if (req.method==='GET' && url.pathname==='/') {
    const html=fs.readFileSync(path.join(__dirname,'public','index.html'));
    return send(res,200,html.toString(),'text/html; charset=utf-8');
  }
  if (req.method==='POST' && url.pathname==='/v1/device/pair') {
    if (!ADMIN_KEY || !safeEqual(req.headers['x-pairing-code'],ADMIN_KEY)) return send(res,401,{error:'Código de pareamento inválido'});
    let data; try { data=JSON.parse((await body(req,4096)).toString()); } catch { return send(res,400,{error:'JSON inválido'}); }
    const id=crypto.randomUUID(), token=crypto.randomBytes(32).toString('hex');
    const state=readState(); state.devices[id]={token,name:String(data.deviceName||'Android').slice(0,80),createdAt:new Date().toISOString(),lastSeen:null}; writeState(state);
    return send(res,200,{deviceId:id,deviceToken:token,pollSeconds:8});
  }
  if (req.method==='POST' && url.pathname==='/v1/diagnostics') {
    const state=readState(), found=deviceFor(req,state);
    if (!found) return send(res,401,{error:'Dispositivo não pareado'});
    const [id,device]=found, raw=await body(req);
    const file=path.join(UPLOAD_DIR,Date.now()+'_'+id+'.multipart');
    fs.writeFileSync(file,raw);
    state.diagnostics.unshift({deviceId:id,deviceName:device.name,receivedAt:new Date().toISOString(),bytes:raw.length,file:path.basename(file),contentType:req.headers['content-type']||''});
    state.diagnostics=state.diagnostics.slice(0,200); device.lastSeen=new Date().toISOString(); writeState(state);
    return send(res,200,{ok:true,receivedBytes:raw.length});
  }
  if (req.method==='GET' && url.pathname==='/v1/device/poll') {
    const state=readState(), found=deviceFor(req,state);
    if (!found) return send(res,401,{error:'Dispositivo não pareado'});
    const [id,device]=found; device.lastSeen=new Date().toISOString();
    const pending=state.commands.filter(c=>c.deviceId===id && !c.acked);
    writeState(state);
    return send(res,200,{serverTime:new Date().toISOString(),commands:pending.map(c=>({id:c.id,type:c.type,payload:c.payload}))});
  }
  if (req.method==='POST' && url.pathname==='/v1/device/ack') {
    const state=readState(), found=deviceFor(req,state);
    if (!found) return send(res,401,{error:'Dispositivo não pareado'});
    let data; try { data=JSON.parse((await body(req,4096)).toString()); } catch { return send(res,400,{error:'JSON inválido'}); }
    const cmd=state.commands.find(c=>c.id===data.commandId && c.deviceId===found[0]);
    if (cmd) { cmd.acked=true; cmd.result=String(data.result||'applied').slice(0,300); cmd.ackedAt=new Date().toISOString(); writeState(state); }
    return send(res,200,{ok:true});
  }
  if (req.method==='GET' && url.pathname==='/v1/admin/state') {
    if (!safeEqual(bearer(req),ADMIN_KEY)) return send(res,401,{error:'Não autorizado'});
    const s=readState();
    return send(res,200,{devices:Object.entries(s.devices).map(([id,d])=>({id,name:d.name,createdAt:d.createdAt,lastSeen:d.lastSeen})),commands:s.commands.slice(-50).reverse(),diagnostics:s.diagnostics.slice(0,50)});
  }
  if (req.method==='POST' && url.pathname==='/v1/admin/config') {
    if (!safeEqual(bearer(req),ADMIN_KEY)) return send(res,401,{error:'Não autorizado'});
    let data; try { data=JSON.parse((await body(req,8192)).toString()); } catch { return send(res,400,{error:'JSON inválido'}); }
    const config=validConfig(data.config);
    if (!config) return send(res,400,{error:'Nenhuma configuração permitida válida'});
    const state=readState(), deviceIds=Object.keys(state.devices);
    const targets=data.deviceId ? [data.deviceId] : deviceIds;
    let queued=0;
    for (const deviceId of targets) {
      if (!state.devices[deviceId]) continue;
      state.commands.push({id:crypto.randomUUID(),deviceId,type:'apply_config',payload:config,createdAt:new Date().toISOString(),acked:false}); queued++;
    }
    writeState(state);
    return send(res,200,{ok:true,queued,config});
  }
  if (req.method==='POST' && url.pathname==='/v1/admin/pair-code-check') {
    if (!safeEqual(bearer(req),ADMIN_KEY)) return send(res,401,{error:'Código inválido'});
    return send(res,200,{ok:true});
  }
  send(res,404,{error:'Not found'});
});
server.listen(PORT,'0.0.0.0',()=>console.log('BATTLE CLUBS remote agent listening on '+PORT));

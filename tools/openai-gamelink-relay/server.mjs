import http from "node:http";
import crypto from "node:crypto";

const PORT = Number(process.env.PORT || 8787);
const OPENAI_API_KEY = process.env.OPENAI_API_KEY || "";
const OPENAI_MODEL = process.env.OPENAI_MODEL || "gpt-4.1-mini";
const RELAY_TOKEN = process.env.GAMELINK_TOKEN || "";
const MAX_JSON_BYTES = 2 * 1024 * 1024;
const MAX_IMAGE_BYTES = 12 * 1024 * 1024;
const WINDOW_MS = 60_000;
const MAX_REQUESTS_PER_WINDOW = 30;

let latestAnalysis = null;
const rate = new Map();

const json = (res, status, data) => {
  const body = JSON.stringify(data);
  res.writeHead(status, {
    "Content-Type": "application/json; charset=utf-8",
    "Content-Length": Buffer.byteLength(body),
    "Cache-Control": "no-store"
  });
  res.end(body);
};

function authorized(req) {
  if (!RELAY_TOKEN) return false;
  const value = req.headers.authorization || "";
  const supplied = value.startsWith("Bearer ") ? value.slice(7) : "";
  const a = Buffer.from(supplied);
  const b = Buffer.from(RELAY_TOKEN);
  return a.length === b.length && crypto.timingSafeEqual(a, b);
}

function rateAllowed(req) {
  const key = req.socket.remoteAddress || "unknown";
  const now = Date.now();
  const entry = rate.get(key) || { start: now, count: 0 };
  if (now - entry.start >= WINDOW_MS) {
    entry.start = now;
    entry.count = 0;
  }
  entry.count += 1;
  rate.set(key, entry);
  // Evita crescimento ilimitado da tabela em serviços expostos à internet.
  if (rate.size > 10_000) {
    for (const [ip, value] of rate) {
      if (now - value.start >= WINDOW_MS) rate.delete(ip);
    }
  }
  return entry.count <= MAX_REQUESTS_PER_WINDOW;
}

async function readBody(req, limit) {
  const chunks = [];
  let size = 0;
  for await (const chunk of req) {
    size += chunk.length;
    if (size > limit) throw Object.assign(new Error("Payload muito grande."), { status: 413 });
    chunks.push(chunk);
  }
  return Buffer.concat(chunks);
}

function extractOutput(data) {
  if (typeof data.output_text === "string" && data.output_text.trim()) return data.output_text.trim();
  const parts = [];
  for (const item of data.output || []) {
    for (const content of item.content || []) {
      if (content.type === "output_text" && content.text) parts.push(content.text);
    }
  }
  return parts.join("\n").trim();
}

async function askOpenAI(prompt, imageDataUrl) {
  if (!OPENAI_API_KEY) throw Object.assign(new Error("OPENAI_API_KEY ainda não foi configurada no servidor."), { status: 503 });
  const content = [{ type: "input_text", text: prompt }];
  if (imageDataUrl) content.push({ type: "input_image", image_url: imageDataUrl, detail: "high" });

  const response = await fetch("https://api.openai.com/v1/responses", {
    method: "POST",
    headers: {
      "Authorization": "Bearer " + OPENAI_API_KEY,
      "Content-Type": "application/json"
    },
    signal: AbortSignal.timeout(45_000),
    body: JSON.stringify({
      model: OPENAI_MODEL,
      instructions: [
        "Você é uma revisora técnica de cenas Unity para um jogo de exploração medieval.",
        "Responda em português brasileiro, com diagnóstico objetivo e priorizado.",
        "Avalie coerência visual, encaixe de paredes/telhados, escala, materiais, ruas, colisões aparentes e organização da cena.",
        "Diferencie claramente o que é visível do que não pode ser confirmado pela imagem ou relatório.",
        "Não invente objetos, medidas ou resultados. Não diga que corrigiu nada.",
        "NÃO gere comandos, código executável ou alterações automáticas. Apenas diagnóstico e passos recomendados."
      ].join(" "),
      input: [{ role: "user", content }]
    })
  });

  const data = await response.json().catch(() => ({}));
  if (!response.ok) {
    const message = data.error?.message || "A API de IA recusou a solicitação.";
    throw Object.assign(new Error(message), { status: response.status === 429 ? 429 : 502 });
  }
  const text = extractOutput(data);
  if (!text) throw Object.assign(new Error("A IA não retornou um diagnóstico legível."), { status: 502 });
  return text;
}

async function handle(req, res) {
  if (req.method === "GET" && req.url === "/health") {
    return json(res, 200, { ok: true, service: "openai-gamelink-relay", configured: Boolean(RELAY_TOKEN && OPENAI_API_KEY) });
  }
  if (!authorized(req)) return json(res, 401, { error: "Token inválido ou não configurado." });
  if (!rateAllowed(req)) return json(res, 429, { error: "Limite temporário de solicitações atingido." });

  if (req.method === "POST" && req.url === "/v1/unity/ping") {
    const raw = await readBody(req, 64 * 1024);
    let info = {};
    try { info = raw.length ? JSON.parse(raw.toString("utf8")) : {}; } catch { return json(res, 400, { error: "JSON inválido." }); }
    return json(res, 200, { ok: true, message: "Relay conectado.", unityVersion: info.unityVersion || null });
  }

  if (req.method === "POST" && req.url === "/v1/unity/scene-report") {
    const raw = await readBody(req, MAX_JSON_BYTES);
    let report;
    try { report = JSON.parse(raw.toString("utf8")); } catch { return json(res, 400, { error: "Relatório JSON inválido." }); }
    if (!report || !Array.isArray(report.objects)) return json(res, 400, { error: "Relatório sem lista de objetos." });
    const safeReport = {
      project: String(report.project || "").slice(0, 120),
      scene: String(report.scene || "").slice(0, 300),
      unityVersion: String(report.unityVersion || "").slice(0, 80),
      objectCount: report.objects.length,
      objects: report.objects.slice(0, 350)
    };
    const diagnosis = await askOpenAI("Analise este relatório estrutural da cena Unity. Ele não contém necessariamente informação visual. Aponte problemas que os dados realmente sustentam e o que precisa de captura visual para confirmar.\n\n" + JSON.stringify(safeReport));
    latestAnalysis = { createdAt: new Date().toISOString(), source: "scene-report", diagnosis };
    return json(res, 200, { ok: true, analysis: diagnosis, createdAt: latestAnalysis.createdAt });
  }

  if (req.method === "POST" && req.url === "/v1/unity/scene-image") {
    const raw = await readBody(req, MAX_IMAGE_BYTES + 256 * 1024);
    const request = new Request("http://relay.local/upload", {
      method: "POST",
      headers: { "content-type": req.headers["content-type"] || "application/octet-stream" },
      body: raw
    });
    let form;
    try { form = await request.formData(); } catch { return json(res, 400, { error: "Envio multipart inválido." }); }
    const file = form.get("image");
    if (!file || typeof file.arrayBuffer !== "function") return json(res, 400, { error: "Campo image não encontrado." });
    if (file.size > MAX_IMAGE_BYTES) return json(res, 413, { error: "Imagem excede 12 MB." });
    if (!["image/png", "image/jpeg", "image/webp"].includes(file.type)) return json(res, 415, { error: "Formato de imagem não permitido." });
    const bytes = Buffer.from(await file.arrayBuffer());
    const imageDataUrl = "data:" + file.type + ";base64," + bytes.toString("base64");
    const diagnosis = await askOpenAI("Analise esta captura da Scene View do Unity para o vilarejo medieval. Descreva defeitos visíveis de construção, encaixe, escala, telhados, ruas, materiais e composição. Não presuma colisões ou partes fora da câmera.", imageDataUrl);
    latestAnalysis = { createdAt: new Date().toISOString(), source: "scene-image", diagnosis };
    return json(res, 200, { ok: true, analysis: diagnosis, createdAt: latestAnalysis.createdAt });
  }

  if (req.method === "GET" && req.url === "/v1/unity/commands") {
    return json(res, 200, { commands: [] });
  }

  if (req.method === "GET" && req.url === "/v1/unity/latest-analysis") {
    return json(res, 200, latestAnalysis || { analysis: null });
  }

  return json(res, 404, { error: "Rota não encontrada." });
}

const server = http.createServer((req, res) => {
  handle(req, res).catch(error => {
    console.error("[GameLink]", error.message);
    json(res, error.status || 500, { error: error.message || "Erro interno." });
  });
});

server.listen(PORT, "0.0.0.0", () => {
  console.log("OpenAI GameLink relay listening on port " + PORT);
  if (!RELAY_TOKEN) console.warn("GAMELINK_TOKEN não configurado; endpoints protegidos ficarão indisponíveis.");
  if (!OPENAI_API_KEY) console.warn("OPENAI_API_KEY não configurada; análise de IA indisponível.");
});

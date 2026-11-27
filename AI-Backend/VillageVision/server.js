const http = require("node:http");

const PORT = Number(process.env.PORT || 8080);
const API_KEY = process.env.OPENAI_API_KEY;
const MODEL = process.env.OPENAI_MODEL || "gpt-4.1-mini";
const WINDOW_MS = 60_000;
const MAX_REQUESTS_PER_WINDOW = 8;
const MAX_BODY_BYTES = 7 * 1024 * 1024;
const clients = new Map();

function send(res, status, data) {
  res.writeHead(status, {
    "content-type": "application/json; charset=utf-8",
    "cache-control": "no-store",
    "access-control-allow-origin": "*",
    "access-control-allow-methods": "POST, OPTIONS",
    "access-control-allow-headers": "Content-Type"
  });
  res.end(JSON.stringify(data));
}

function rateLimited(ip) {
  const now = Date.now();
  const entry = clients.get(ip);
  if (!entry || now - entry.start > WINDOW_MS) {
    clients.set(ip, { start: now, count: 1 });
    return false;
  }
  entry.count += 1;
  return entry.count > MAX_REQUESTS_PER_WINDOW;
}

function readJson(req) {
  return new Promise((resolve, reject) => {
    let size = 0;
    const chunks = [];
    req.on("data", chunk => {
      size += chunk.length;
      if (size > MAX_BODY_BYTES) {
        reject(new Error("Imagem muito grande; limite de 7 MB."));
        req.destroy();
        return;
      }
      chunks.push(chunk);
    });
    req.on("end", () => {
      try { resolve(JSON.parse(Buffer.concat(chunks).toString("utf8"))); }
      catch { reject(new Error("JSON inválido.")); }
    });
    req.on("error", reject);
  });
}

function extractText(response) {
  if (typeof response.output_text === "string") return response.output_text;
  const parts = [];
  for (const item of response.output || []) {
    for (const content of item.content || []) {
      if (content.type === "output_text" && content.text) parts.push(content.text);
    }
  }
  return parts.join("\n").trim();
}

const server = http.createServer(async (req, res) => {
  if (req.method === "OPTIONS") return send(res, 204, {});
  if (req.method !== "POST" || req.url !== "/v1/village/inspect")
    return send(res, 404, { error: "Rota não encontrada." });
  if (!API_KEY) return send(res, 503, { error: "OPENAI_API_KEY não configurada no servidor." });

  const ip = (req.headers["x-forwarded-for"] || req.socket.remoteAddress || "unknown").split(",")[0].trim();
  if (rateLimited(ip)) return send(res, 429, { error: "Limite temporário de análises atingido. Aguarde um minuto." });

  try {
    const body = await readJson(req);
    if (!body || typeof body.imageBase64 !== "string" || body.imageBase64.length < 100)
      return send(res, 400, { error: "Imagem ausente ou inválida." });
    if (body.imageBase64.length > 6_500_000)
      return send(res, 413, { error: "Imagem excede o limite permitido." });

    const imageUrl = "data:image/jpeg;base64," + body.imageBase64;
    const prompt = [
      "Você é o inspetor visual de um jogo de exploração medieval feito no Unity.",
      "Examine somente o que é visível na captura. Não afirme que detectou colisões internas ou peças ocultas.",
      "Dê um relatório curto em português brasileiro com: (1) o que está bom, (2) defeitos visíveis por prioridade, (3) correções concretas no Unity e (4) o que precisa de outra captura para confirmar.",
      "Não execute alterações. Não invente coordenadas nem nomes de assets.",
      "Contexto do jogo: " + String(body.sceneName || "cena medieval").slice(0, 100),
      "Modelo do dispositivo: " + String(body.deviceModel || "Android").slice(0, 80),
      "Pedido: " + String(body.instruction || "Inspecione a cena").slice(0, 1200)
    ].join("\n");

    const upstream = await fetch("https://api.openai.com/v1/responses", {
      method: "POST",
      headers: {
        "Authorization": "Bearer " + API_KEY,
        "Content-Type": "application/json"
      },
      body: JSON.stringify({
        model: MODEL,
        store: false,
        max_output_tokens: 700,
        input: [{
          role: "user",
          content: [
            { type: "input_text", text: prompt },
            { type: "input_image", image_url: imageUrl, detail: "high" }
          ]
        }]
      }),
      signal: AbortSignal.timeout(65_000)
    });

    const result = await upstream.json();
    if (!upstream.ok) {
      const message = result && result.error && result.error.message
        ? result.error.message : "Falha na API da OpenAI.";
      return send(res, upstream.status, { error: message });
    }
    const analysis = extractText(result);
    if (!analysis) return send(res, 502, { error: "A OpenAI não retornou texto de análise." });
    return send(res, 200, { analysis });
  } catch (error) {
    const status = error && error.message && error.message.includes("7 MB") ? 413 : 500;
    return send(res, status, { error: error.message || "Erro interno." });
  }
});

server.listen(PORT, "0.0.0.0", () => {
  console.log("Village AI proxy listening on " + PORT);
});

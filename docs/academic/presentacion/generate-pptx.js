const PptxGenJS = require("pptxgenjs");
const path = require("path");

const pptx = new PptxGenJS();
pptx.defineLayout({ name: "WIDE", width: 13.333, height: 7.5 });
pptx.layout = "WIDE";
pptx.author = "Katlheen Valeska Rodriguez Garcia";
pptx.title = "Presentación Final — ContactCenterAI";
pptx.subject = "Proyecto Final Ingeniería en Desarrollo de Software";

const C = {
  navy: "1B3A5F",
  blue: "2E5A88",
  lightBlue: "D6E4F0",
  accent: "3A7CA5",
  white: "FFFFFF",
  nearWhite: "F7F9FC",
  gray: "5A6A7A",
  softGray: "E8EEF4",
  dark: "1A2332",
  line: "C5D0DC",
  success: "2E7D4F",
  muted: "8A97A6",
};

const ROOT = path.resolve(__dirname, "../../..");
const IMG_ANTES = path.join(ROOT, "docs/security/antes-correccion-nuget.png");
const IMG_DESPUES = path.join(ROOT, "docs/security/despues-correccion-nuget.png");

function addChrome(slide, title) {
  slide.addShape(pptx.shapes.RECTANGLE, {
    x: 0, y: 0, w: 13.333, h: 7.5,
    fill: { color: C.nearWhite }, line: { color: C.nearWhite },
  });
  slide.addShape(pptx.shapes.RECTANGLE, {
    x: 0, y: 0, w: 13.333, h: 0.08,
    fill: { color: C.navy }, line: { color: C.navy },
  });
  slide.addShape(pptx.shapes.RECTANGLE, {
    x: 0, y: 7.35, w: 13.333, h: 0.15,
    fill: { color: C.navy }, line: { color: C.navy },
  });
  if (title) {
    slide.addText(title, {
      x: 0.55, y: 0.28, w: 12.2, h: 0.55,
      fontSize: 26, fontFace: "Calibri", bold: true, color: C.navy, margin: 0,
    });
    slide.addShape(pptx.shapes.RECTANGLE, {
      x: 0.55, y: 0.88, w: 1.4, h: 0.06,
      fill: { color: C.accent }, line: { color: C.accent },
    });
  }
}

function bulletSlide(slide, items, opts = {}) {
  const y0 = opts.y0 ?? 1.2;
  const x = opts.x ?? 0.7;
  const w = opts.w ?? 11.8;
  slide.addText(
    items.map((t) => ({
      text: t,
      options: { breakLine: true },
    })),
    {
      x, y: y0, w, h: opts.h ?? 5.2,
      fontSize: opts.fontSize ?? 20,
      fontFace: "Calibri",
      color: C.dark,
      paraSpaceAfter: 14,
      bullet: { code: "25CF" },
      valign: "top",
    }
  );
}

function card(slide, x, y, w, h, title, lines) {
  slide.addShape(pptx.shapes.ROUNDED_RECTANGLE, {
    x, y, w, h,
    fill: { color: C.white },
    line: { color: C.line, width: 1 },
    rectRadius: 0.08,
  });
  slide.addText(title, {
    x: x + 0.15, y: y + 0.12, w: w - 0.3, h: 0.35,
    fontSize: 14, fontFace: "Calibri", bold: true, color: C.navy, margin: 0,
  });
  if (lines && lines.length) {
    slide.addText(
      lines.map((t) => ({ text: t, options: { breakLine: true } })),
      {
        x: x + 0.15, y: y + 0.48, w: w - 0.3, h: h - 0.6,
        fontSize: 13, fontFace: "Calibri", color: C.gray,
        paraSpaceAfter: 6, valign: "top", margin: 0,
      }
    );
  }
}

function flowBox(slide, x, y, w, h, label, fill) {
  slide.addShape(pptx.shapes.ROUNDED_RECTANGLE, {
    x, y, w, h,
    fill: { color: fill || C.navy },
    line: { color: fill || C.navy },
    rectRadius: 0.06,
  });
  slide.addText(label, {
    x, y, w, h,
    fontSize: 12, fontFace: "Calibri", bold: true, color: C.white,
    align: "center", valign: "middle", margin: 0,
  });
}

function arrowDown(slide, x, y) {
  slide.addText("↓", {
    x, y, w: 0.4, h: 0.28,
    fontSize: 16, fontFace: "Calibri", color: C.accent,
    align: "center", margin: 0,
  });
}

function arrowRight(slide, x, y) {
  slide.addText("→", {
    x, y, w: 0.35, h: 0.35,
    fontSize: 18, fontFace: "Calibri", color: C.accent,
    align: "center", valign: "middle", margin: 0,
  });
}

// ─── 1. Portada ───────────────────────────────────────────────
{
  const s = pptx.addSlide();
  s.addShape(pptx.shapes.RECTANGLE, {
    x: 0, y: 0, w: 13.333, h: 7.5,
    fill: { color: C.navy }, line: { color: C.navy },
  });
  s.addShape(pptx.shapes.RECTANGLE, {
    x: 0, y: 0, w: 0.22, h: 7.5,
    fill: { color: C.accent }, line: { color: C.accent },
  });
  s.addShape(pptx.shapes.RECTANGLE, {
    x: 0.9, y: 2.05, w: 1.8, h: 0.07,
    fill: { color: C.accent }, line: { color: C.accent },
  });
  s.addText("ContactCenterAI", {
    x: 0.9, y: 2.25, w: 11, h: 0.85,
    fontSize: 44, fontFace: "Calibri", bold: true, color: C.white, margin: 0,
  });
  s.addText("Proyecto Final · Ingeniería en Desarrollo de Software", {
    x: 0.9, y: 3.15, w: 11, h: 0.4,
    fontSize: 18, fontFace: "Calibri", color: C.lightBlue, margin: 0,
  });
  s.addText("Katlheen Valeska Rodriguez Garcia", {
    x: 0.9, y: 4.2, w: 11, h: 0.35,
    fontSize: 16, fontFace: "Calibri", color: C.white, margin: 0,
  });
  s.addText("Julio 2026", {
    x: 0.9, y: 4.6, w: 11, h: 0.3,
    fontSize: 14, fontFace: "Calibri", color: C.muted, margin: 0,
  });
  s.addNotes(
    "Buenos días/tardes. Presento ContactCenterAI, mi proyecto final de Ingeniería en Desarrollo de Software. " +
      "Es una plataforma SaaS multiempresa que ayuda a agentes de contact center a responder con base en documentos PDF mediante RAG. " +
      "En esta presentación revisaré el problema, la arquitectura, seguridad, calidad y resultados."
  );
}

// ─── 2. Problema ──────────────────────────────────────────────
{
  const s = pptx.addSlide();
  addChrome(s, "Problema");
  bulletSlide(s, [
    "Agentes sin respuestas consistentes sobre políticas y procedimientos",
    "Documentación dispersa en PDFs difíciles de consultar en vivo",
    "Riesgo de respuestas incorrectas o desactualizadas al cliente",
    "Falta de aislamiento entre empresas en soluciones genéricas",
    "Poca trazabilidad: sin historial, fuentes ni escalamiento a tickets",
  ]);
  s.addNotes(
    "El problema central es la fricción operativa del agente: debe responder rápido, pero la información vive en PDFs. " +
      "Sin un sistema con búsqueda semántica, tenancy y trazabilidad, aumentan errores, tiempos de atención y riesgo de filtrar datos entre empresas."
  );
}

// ─── 3. Objetivos ─────────────────────────────────────────────
{
  const s = pptx.addSlide();
  addChrome(s, "Objetivos");
  card(s, 0.55, 1.25, 12.2, 1.35, "Objetivo general", [
    "Desarrollar una plataforma SaaS multiempresa con RAG sobre PDF para asistir a agentes de contact center con respuestas fundamentadas, seguras y auditables.",
  ]);
  card(s, 0.55, 2.85, 5.9, 3.6, "Objetivos específicos", [
    "1. Autenticación Auth0 y roles (SuperAdmin, CompanyAdmin, Agent)",
    "2. Gestión multiempresa, usuarios y documentos PDF",
    "3. Pipeline asíncrono: Worker + embeddings + pgvector",
    "4. Chat RAG con Gemini y fuentes citables",
    "5. Tickets, GraphQL BFF y despliegue en AWS EC2",
  ]);
  card(s, 6.65, 2.85, 6.1, 3.6, "Alcance del MVP", [
    "• Frontend React + Vite",
    "• Core API, Chat API, BFF, Worker",
    "• RabbitMQ, PostgreSQL, Docker Compose",
    "• CI/CD con GitHub Actions",
    "• Evidencia de calidad: 131 pruebas",
  ]);
  s.addNotes(
    "El objetivo general es entregar un MVP usable: SaaS multiempresa con RAG. " +
      "Los específicos cubren identidad, documentos, procesamiento asíncrono, chat con fuentes, tickets, BFF GraphQL y despliegue cloud. " +
      "No prometo E2E de navegador ni métricas productivas de AWS en esta entrega."
  );
}

// ─── 4. Arquitectura ──────────────────────────────────────────
{
  const s = pptx.addSlide();
  addChrome(s, "Arquitectura");

  // Side: Auth0
  flowBox(s, 0.5, 2.9, 2.0, 0.7, "Auth0", C.accent);
  s.addText("JWT RS256", {
    x: 0.5, y: 3.7, w: 2.0, h: 0.3,
    fontSize: 12, fontFace: "Calibri", color: C.gray, align: "center", margin: 0,
  });
  arrowRight(s, 2.55, 3.0);

  const main = [
    "React",
    "GraphQL BFF",
    "Core API",
    "RabbitMQ",
    "Worker",
    "PostgreSQL",
    "pgvector",
    "Gemini",
  ];
  main.forEach((label, i) => {
    const y = 1.15 + i * 0.7;
    flowBox(s, 3.0, y, 3.2, 0.48, label, i % 2 === 0 ? C.navy : C.blue);
    if (i < main.length - 1) {
      s.addText("↓", {
        x: 4.3, y: y + 0.45, w: 0.5, h: 0.28,
        fontSize: 14, fontFace: "Calibri", color: C.accent, align: "center", margin: 0,
      });
    }
  });

  // Side: Chat API
  arrowRight(s, 6.35, 2.45);
  flowBox(s, 6.85, 2.3, 2.5, 0.7, "Chat API", C.accent);
  s.addText("Conversaciones RAG", {
    x: 6.85, y: 3.1, w: 2.5, h: 0.3,
    fontSize: 12, fontFace: "Calibri", color: C.gray, align: "center", margin: 0,
  });

  card(s, 9.55, 1.4, 3.3, 5.2, "Integración", [
    "Auth0 protege Frontend,",
    "Core API y Chat API.",
    "",
    "Chat API consulta Core",
    "(perfil + search) y",
    "Gemini; persiste en",
    "chat-db.",
    "",
    "Worker indexa PDF →",
    "embeddings → pgvector.",
  ]);

  s.addNotes(
    "La arquitectura es por capas y servicios. El frontend React habla con el GraphQL BFF y también con Core y Chat según el caso. " +
      "Core API publica eventos a RabbitMQ; el Worker procesa PDF, genera embeddings y guarda en PostgreSQL con pgvector. " +
      "Gemini aporta embeddings y generación. Auth0 emite JWT. Chat API es el bounded context de conversaciones RAG."
  );
}

// ─── 5. Tecnologías ───────────────────────────────────────────
{
  const s = pptx.addSlide();
  addChrome(s, "Tecnologías");
  const techs = [
    ["ASP.NET Core", "React", "Vite"],
    ["PostgreSQL", "pgvector", "RabbitMQ"],
    ["GraphQL", "Docker Compose", "AWS EC2"],
    ["Auth0", "Gemini", "GitHub Actions"],
    ["Caddy", "", ""],
  ];
  techs.forEach((row, ri) => {
    row.forEach((label, ci) => {
      if (!label) return;
      const x = 0.7 + ci * 4.1;
      const y = 1.25 + ri * 1.05;
      s.addShape(pptx.shapes.ROUNDED_RECTANGLE, {
        x, y, w: 3.8, h: 0.85,
        fill: { color: C.white },
        line: { color: C.line, width: 1 },
        rectRadius: 0.08,
      });
      s.addShape(pptx.shapes.RECTANGLE, {
        x, y, w: 0.12, h: 0.85,
        fill: { color: ri < 2 ? C.navy : C.accent },
        line: { color: ri < 2 ? C.navy : C.accent },
      });
      s.addText(label, {
        x: x + 0.3, y, w: 3.3, h: 0.85,
        fontSize: 16, fontFace: "Calibri", bold: true, color: C.navy,
        valign: "middle", margin: 0,
      });
    });
  });
  s.addNotes(
    "Stack backend en ASP.NET Core 9; frontend React con Vite. Persistencia PostgreSQL con pgvector. " +
      "Mensajería RabbitMQ, GraphQL HotChocolate, orquestación Docker Compose, despliegue AWS EC2, Auth0, Gemini, CI/CD GitHub Actions y HTTPS con Caddy."
  );
}

// ─── 6. Arquitectura RAG ──────────────────────────────────────
{
  const s = pptx.addSlide();
  addChrome(s, "Arquitectura RAG");

  const steps = [
    { t: "1. PDF", d: "Carga en Core API" },
    { t: "2. Cola", d: "Evento RabbitMQ" },
    { t: "3. Worker", d: "Texto + chunks" },
    { t: "4. Embed", d: "Gemini embeddings" },
    { t: "5. Índice", d: "pgvector" },
    { t: "6. Pregunta", d: "Chat API" },
    { t: "7. Retrieve", d: "Búsqueda semántica" },
    { t: "8. Respuesta", d: "Gemini + fuentes" },
  ];
  steps.forEach((st, i) => {
    const col = i % 4;
    const row = Math.floor(i / 4);
    const x = 0.55 + col * 3.2;
    const y = 1.3 + row * 2.5;
    s.addShape(pptx.shapes.ROUNDED_RECTANGLE, {
      x, y, w: 2.95, h: 1.7,
      fill: { color: C.white },
      line: { color: C.line, width: 1 },
      rectRadius: 0.08,
    });
    s.addShape(pptx.shapes.OVAL, {
      x: x + 1.1, y: y + 0.2, w: 0.7, h: 0.7,
      fill: { color: C.navy }, line: { color: C.navy },
    });
    s.addText(String(i + 1), {
      x: x + 1.1, y: y + 0.2, w: 0.7, h: 0.7,
      fontSize: 16, fontFace: "Calibri", bold: true, color: C.white,
      align: "center", valign: "middle", margin: 0,
    });
    s.addText(st.t.replace(/^\d+\.\s*/, ""), {
      x: x + 0.15, y: y + 1.0, w: 2.65, h: 0.3,
      fontSize: 14, fontFace: "Calibri", bold: true, color: C.navy,
      align: "center", margin: 0,
    });
    s.addText(st.d, {
      x: x + 0.15, y: y + 1.3, w: 2.65, h: 0.28,
      fontSize: 12, fontFace: "Calibri", color: C.gray,
      align: "center", margin: 0,
    });
  });
  s.addNotes(
    "Flujo RAG completo: el agente sube un PDF; Core encola el trabajo; el Worker extrae texto, fragmenta y pide embeddings a Gemini; " +
      "se indexan en pgvector. En el chat, la pregunta se convierte en vector, se recuperan fragmentos relevantes y Gemini genera la respuesta con fuentes. " +
      "Así la respuesta queda anclada al documento de la empresa."
  );
}

// ─── 7. Microservicios ────────────────────────────────────────
{
  const s = pptx.addSlide();
  addChrome(s, "Microservicios");
  const services = [
    { n: "Core API", d: "Empresas, usuarios, documentos, tickets, búsqueda" },
    { n: "Chat API", d: "Conversaciones RAG, historial, integración Gemini" },
    { n: "GraphQL BFF", d: "Agregación de consultas para el frontend" },
    { n: "Worker", d: "PDF, chunks, embeddings, consumers RabbitMQ" },
    { n: "RabbitMQ", d: "Mensajería asíncrona entre Core y Worker" },
  ];
  services.forEach((svc, i) => {
    const y = 1.2 + i * 1.05;
    s.addShape(pptx.shapes.ROUNDED_RECTANGLE, {
      x: 0.55, y, w: 12.2, h: 0.9,
      fill: { color: C.white },
      line: { color: C.line, width: 1 },
      rectRadius: 0.06,
    });
    s.addShape(pptx.shapes.ROUNDED_RECTANGLE, {
      x: 0.7, y: y + 0.18, w: 2.6, h: 0.55,
      fill: { color: i === 4 ? C.accent : C.navy },
      line: { color: i === 4 ? C.accent : C.navy },
      rectRadius: 0.05,
    });
    s.addText(svc.n, {
      x: 0.7, y: y + 0.18, w: 2.6, h: 0.55,
      fontSize: 13, fontFace: "Calibri", bold: true, color: C.white,
      align: "center", valign: "middle", margin: 0,
    });
    s.addText(svc.d, {
      x: 3.55, y, w: 8.9, h: 0.9,
      fontSize: 16, fontFace: "Calibri", color: C.dark,
      valign: "middle", margin: 0,
    });
  });
  s.addNotes(
    "Hay separación por bounded context: Core posee empresas, usuarios, documentos y tickets; Chat posee conversaciones. " +
      "El BFF GraphQL reduce acoplamiento del frontend. El Worker aísla el trabajo pesado de PDF. RabbitMQ desacopla productores y consumidores."
  );
}

// ─── 8. Seguridad ─────────────────────────────────────────────
{
  const s = pptx.addSlide();
  addChrome(s, "Seguridad");
  const items = [
    { t: "Auth0", d: "Identidad externa · JWT RS256" },
    { t: "Roles", d: "SuperAdmin · CompanyAdmin · Agent" },
    { t: "JWT", d: "Validación issuer, audience y lifetime" },
    { t: "HTTPS", d: "Caddy en borde de despliegue" },
    { t: "Multiempresa", d: "Aislamiento por CompanyId" },
    { t: "Secretos", d: "Fuera de git · .env / host EC2" },
  ];
  items.forEach((it, i) => {
    const col = i % 3;
    const row = Math.floor(i / 3);
    const x = 0.55 + col * 4.2;
    const y = 1.35 + row * 2.55;
    s.addShape(pptx.shapes.ROUNDED_RECTANGLE, {
      x, y, w: 3.95, h: 2.2,
      fill: { color: C.white },
      line: { color: C.line, width: 1 },
      rectRadius: 0.08,
    });
    s.addShape(pptx.shapes.RECTANGLE, {
      x, y, w: 3.95, h: 0.12,
      fill: { color: C.navy }, line: { color: C.navy },
    });
    s.addText(it.t, {
      x: x + 0.25, y: y + 0.55, w: 3.45, h: 0.45,
      fontSize: 20, fontFace: "Calibri", bold: true, color: C.navy, margin: 0,
    });
    s.addText(it.d, {
      x: x + 0.25, y: y + 1.15, w: 3.45, h: 0.7,
      fontSize: 14, fontFace: "Calibri", color: C.gray, margin: 0,
    });
  });
  s.addNotes(
    "Seguridad en capas: Auth0 autentica; la base local autoriza con rol, empresa y estado activo. " +
      "Los JWT se validan con parámetros estrictos. En producción hay HTTPS con Caddy. " +
      "El aislamiento multiempresa filtra por CompanyId. Los secretos no viven en el repositorio."
  );
}

// ─── 9. CI/CD ─────────────────────────────────────────────────
{
  const s = pptx.addSlide();
  addChrome(s, "CI/CD");
  const stages = [
    { n: "01", t: "Push / PR", d: "GitHub Actions CI" },
    { n: "02", t: "Build & Test", d: "dotnet + frontend" },
    { n: "03", t: "Imágenes", d: "Docker Compose" },
    { n: "04", t: "Deploy", d: "AWS EC2 vía SSH" },
  ];
  stages.forEach((st, i) => {
    const x = 0.55 + i * 3.2;
    s.addShape(pptx.shapes.ROUNDED_RECTANGLE, {
      x, y: 2.0, w: 2.95, h: 3.2,
      fill: { color: C.white },
      line: { color: C.line, width: 1 },
      rectRadius: 0.08,
    });
    s.addText(st.n, {
      x, y: 2.35, w: 2.95, h: 0.55,
      fontSize: 28, fontFace: "Calibri", bold: true, color: C.accent,
      align: "center", margin: 0,
    });
    s.addText(st.t, {
      x: x + 0.15, y: 3.2, w: 2.65, h: 0.5,
      fontSize: 18, fontFace: "Calibri", bold: true, color: C.navy,
      align: "center", margin: 0,
    });
    s.addText(st.d, {
      x: x + 0.15, y: 3.85, w: 2.65, h: 0.7,
      fontSize: 14, fontFace: "Calibri", color: C.gray,
      align: "center", margin: 0,
    });
    if (i < 3) {
      s.addText("→", {
        x: x + 2.85, y: 3.3, w: 0.4, h: 0.4,
        fontSize: 22, fontFace: "Calibri", color: C.accent, margin: 0,
      });
    }
  });
  s.addNotes(
    "El pipeline empieza con push o pull request. GitHub Actions construye y ejecuta pruebas. " +
      "Las imágenes se orquestan con Docker Compose y el despliegue llega a AWS EC2. " +
      "También hay workflow de CodeQL para análisis estático."
  );
}

// ─── 10. Pruebas ──────────────────────────────────────────────
{
  const s = pptx.addSlide();
  addChrome(s, "Pruebas");

  const kpis = [
    { v: "131", l: "Pruebas ejecutadas", c: C.navy },
    { v: "0", l: "Fallidas", c: C.success },
    { v: "0", l: "Omitidas", c: C.success },
  ];
  kpis.forEach((k, i) => {
    const x = 0.55 + i * 4.2;
    s.addShape(pptx.shapes.ROUNDED_RECTANGLE, {
      x, y: 1.15, w: 3.95, h: 1.35,
      fill: { color: C.white },
      line: { color: C.line, width: 1 },
      rectRadius: 0.08,
    });
    s.addText(k.v, {
      x, y: 1.25, w: 3.95, h: 0.7,
      fontSize: 40, fontFace: "Calibri", bold: true, color: k.c,
      align: "center", margin: 0,
    });
    s.addText(k.l, {
      x, y: 1.95, w: 3.95, h: 0.35,
      fontSize: 14, fontFace: "Calibri", color: C.gray,
      align: "center", margin: 0,
    });
  });

  s.addText("Corrección de vulnerabilidades NuGet (capturas del proyecto)", {
    x: 0.55, y: 2.7, w: 12, h: 0.35,
    fontSize: 14, fontFace: "Calibri", bold: true, color: C.navy, margin: 0,
  });

  s.addShape(pptx.shapes.ROUNDED_RECTANGLE, {
    x: 0.55, y: 3.15, w: 6.0, h: 3.85,
    fill: { color: C.white },
    line: { color: C.line, width: 1 },
    rectRadius: 0.06,
  });
  s.addShape(pptx.shapes.ROUNDED_RECTANGLE, {
    x: 6.75, y: 3.15, w: 6.0, h: 3.85,
    fill: { color: C.white },
    line: { color: C.line, width: 1 },
    rectRadius: 0.06,
  });
  s.addText("Antes", {
    x: 0.7, y: 3.25, w: 2, h: 0.28,
    fontSize: 12, fontFace: "Calibri", bold: true, color: C.gray, margin: 0,
  });
  s.addText("Después", {
    x: 6.9, y: 3.25, w: 2, h: 0.28,
    fontSize: 12, fontFace: "Calibri", bold: true, color: C.success, margin: 0,
  });

  try {
    s.addImage({ path: IMG_ANTES, x: 0.7, y: 3.55, w: 5.7, h: 3.25 });
    s.addImage({ path: IMG_DESPUES, x: 6.9, y: 3.55, w: 5.7, h: 3.25 });
  } catch {
    bulletSlide(s, [
      "Suite: Domain, Application, Infrastructure, Api, Chat, Bff",
      "Build Release: 0 errores, 0 advertencias",
      "AutoMapper y SemanticKernel actualizados",
      "Auditoría NuGet sin hallazgos posteriores",
    ], { y0: 3.5 });
  }

  s.addNotes(
    "Ejecuté la suite completa: 131 pruebas, cero fallidas y cero omitidas. " +
      "Además corregí vulnerabilidades NuGet: AutoMapper High y SemanticKernel Critical. " +
      "Las capturas muestran el antes y el después de la auditoría. El frontend también construye correctamente."
  );
}

// ─── 11. Demostración ─────────────────────────────────────────
{
  const s = pptx.addSlide();
  addChrome(s, "Demostración — pantallas clave");
  const screens = [
    { t: "Dashboard", lines: ["Resumen operativo", "Acceso por rol"] },
    { t: "Documentos", lines: ["Carga PDF", "Estado de indexación"] },
    { t: "Chat", lines: ["Preguntas RAG", "Fuentes citadas"] },
    { t: "Tickets", lines: ["Escalamiento", "Seguimiento"] },
    { t: "GraphQL", lines: ["BFF HotChocolate", "Consultas agregadas"] },
  ];
  screens.forEach((sc, i) => {
    const x = 0.4 + i * 2.55;
    // fake browser chrome
    s.addShape(pptx.shapes.ROUNDED_RECTANGLE, {
      x, y: 1.3, w: 2.4, h: 5.2,
      fill: { color: C.white },
      line: { color: C.line, width: 1 },
      rectRadius: 0.08,
    });
    s.addShape(pptx.shapes.RECTANGLE, {
      x, y: 1.3, w: 2.4, h: 0.45,
      fill: { color: C.navy }, line: { color: C.navy },
    });
    s.addText(sc.t, {
      x, y: 1.3, w: 2.4, h: 0.45,
      fontSize: 13, fontFace: "Calibri", bold: true, color: C.white,
      align: "center", valign: "middle", margin: 0,
    });
    // mock content bars
    for (let b = 0; b < 4; b++) {
      s.addShape(pptx.shapes.ROUNDED_RECTANGLE, {
        x: x + 0.2, y: 2.05 + b * 0.7, w: 2.0, h: 0.45,
        fill: { color: b === 0 ? C.lightBlue : C.softGray },
        line: { color: C.softGray },
        rectRadius: 0.04,
      });
    }
    s.addText(sc.lines.join("\n"), {
      x: x + 0.15, y: 5.2, w: 2.1, h: 0.9,
      fontSize: 12, fontFace: "Calibri", color: C.dark,
      align: "center", valign: "middle", margin: 0,
    });
  });
  s.addNotes(
    "En la demo en vivo muestro: Dashboard, Documentos con carga PDF, Chat RAG con fuentes, Tickets de escalamiento y el playground o consultas GraphQL del BFF. " +
      "Estas siluetas representan las pantallas del sistema; la evidencia funcional está en la aplicación en ejecución."
  );
}

// ─── 12. Resultados ───────────────────────────────────────────
{
  const s = pptx.addSlide();
  addChrome(s, "Resultados");
  bulletSlide(s, [
    "Auth0 + roles + aislamiento multiempresa",
    "Documentos PDF indexados con Worker y pgvector",
    "Chat RAG con Gemini e historial de conversaciones",
    "Tickets, GraphQL BFF y mensajería RabbitMQ",
    "Despliegue Docker Compose en AWS EC2 + Caddy",
    "131/131 pruebas en verde y vulnerabilidades NuGet corregidas",
  ]);
  s.addNotes(
    "Los resultados del MVP son concretos: identidad y tenancy, pipeline de documentos, chat con fuentes, tickets, BFF, despliegue cloud y calidad verificable. " +
      "El sistema cumple el propósito funcional del proyecto académico con evidencia en código y pruebas."
  );
}

// ─── 13. Lecciones aprendidas ─────────────────────────────────
{
  const s = pptx.addSlide();
  addChrome(s, "Lecciones aprendidas");
  card(s, 0.55, 1.25, 6.0, 5.3, "Problemas encontrados", [
    "• Errores API sin formato uniforme",
    "• CORS / proxy en GraphQL BFF",
    "• Imagen Docker del Worker inestable",
    "• Vulnerabilidades NuGet High/Critical",
    "• Ajuste de stack (Azure → AWS/Gemini)",
  ]);
  card(s, 6.75, 1.25, 6.0, 5.3, "Cómo se resolvieron", [
    "• Middleware global de excepciones",
    "• CORS + nginx/Caddy + cliente GraphQL",
    "• Runtime aspnet:9.0 en Worker",
    "• Upgrade AutoMapper y SemanticKernel",
    "• Decisiones documentadas en actualización técnica",
  ]);
  s.addNotes(
    "Aprendí que la arquitectura evoluciona: cambiamos Azure OpenAI por Gemini y Azure por EC2 por disponibilidad académica. " +
      "Los defectos reales se resolvieron con commits concretos: excepciones, CORS del BFF, Dockerfile del Worker y parches de dependencias. " +
      "Documentar el cambio de alcance evitó confusión entre plan e implementación."
  );
}

// ─── 14. Conclusiones ─────────────────────────────────────────
{
  const s = pptx.addSlide();
  addChrome(s, "Conclusiones");
  bulletSlide(s, [
    "MVP funcional: SaaS multiempresa con RAG sobre PDF",
    "Arquitectura por servicios desacoplados y desplegable",
    "Seguridad basada en Auth0, roles y tenancy",
    "Calidad demostrable con suite automatizada en verde",
    "Base sólida para evolucionar hacia producción completa",
  ], { fontSize: 20, y0: 1.5 });
  s.addNotes(
    "Concluyo que ContactCenterAI cumple el objetivo del proyecto final: un sistema coherente, seguro y verificable. " +
      "Quedan caminos naturales de mejora —E2E, métricas en AWS, más pruebas de dominio— pero el MVP está listo para defensa académica."
  );
}

// ─── 15. Preguntas ────────────────────────────────────────────
{
  const s = pptx.addSlide();
  s.addShape(pptx.shapes.RECTANGLE, {
    x: 0, y: 0, w: 13.333, h: 7.5,
    fill: { color: C.navy }, line: { color: C.navy },
  });
  s.addShape(pptx.shapes.RECTANGLE, {
    x: 0, y: 0, w: 0.22, h: 7.5,
    fill: { color: C.accent }, line: { color: C.accent },
  });
  s.addText("¿Preguntas?", {
    x: 0.9, y: 2.6, w: 11.5, h: 0.9,
    fontSize: 48, fontFace: "Calibri", bold: true, color: C.white, margin: 0,
  });
  s.addText("ContactCenterAI  ·  Gracias", {
    x: 0.9, y: 3.7, w: 11.5, h: 0.4,
    fontSize: 18, fontFace: "Calibri", color: C.lightBlue, margin: 0,
  });
  s.addText("Katlheen Valeska Rodriguez Garcia", {
    x: 0.9, y: 5.5, w: 11.5, h: 0.35,
    fontSize: 14, fontFace: "Calibri", color: C.muted, margin: 0,
  });
  s.addNotes(
    "Quedo atenta a sus preguntas sobre arquitectura, RAG, seguridad, CI/CD o la demo. Gracias por su atención."
  );
}

const outRoot = path.join(ROOT, "Presentacion_Final_ContactCenterAI.pptx");
const outDocs = path.join(__dirname, "Presentacion_Final_ContactCenterAI.pptx");

pptx
  .writeFile({ fileName: outRoot })
  .then(() => pptx.writeFile({ fileName: outDocs }))
  .then(() => {
    console.log("OK:", outRoot);
    console.log("OK:", outDocs);
  })
  .catch((err) => {
    console.error(err);
    process.exit(1);
  });

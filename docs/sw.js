// Bump CACHE_VERSION whenever any cached file changes so clients refresh.
const CACHE_VERSION = "v3";
const CACHE_NAME = `insulinipcalc-${CACHE_VERSION}`;

const APP_SHELL = [
  "./",
  "./index.html",
  "./app.js",
  "./app.py",
  "./styles.css",
  "./manifest.webmanifest",
  "./icons/favicon.ico",
  "./icons/favicon-16.png",
  "./icons/favicon-32.png",
  "./icons/apple-touch-icon.png",
  "./icons/icon-192.png",
  "./icons/icon-512.png",
  "./icons/maskable-512.png",
];

// Pyodide is loaded from the CDN; its files are cached here for offline use.
const PYODIDE_BASE = "https://cdn.jsdelivr.net/pyodide/v0.26.2/full/";
const PYODIDE_FILES = ["pyodide.mjs", "pyodide.asm.js", "pyodide.asm.wasm", "python_stdlib.zip", "pyodide-lock.json"]
  .map((file) => PYODIDE_BASE + file);

self.addEventListener("install", (event) => {
  event.waitUntil(
    caches.open(CACHE_NAME).then(async (cache) => {
      await cache.addAll(APP_SHELL);
      // Best effort: anything missed is cached on first use by the fetch handler.
      await Promise.allSettled(PYODIDE_FILES.map((url) => cache.add(new Request(url, { mode: "cors" }))));
    }).then(() => self.skipWaiting())
  );
});

self.addEventListener("activate", (event) => {
  event.waitUntil(
    caches.keys()
      .then((keys) => Promise.all(keys.filter((key) => key !== CACHE_NAME).map((key) => caches.delete(key))))
      .then(() => self.clients.claim())
  );
});

self.addEventListener("fetch", (event) => {
  const { request } = event;
  if (request.method !== "GET") return;

  if (request.url.startsWith(PYODIDE_BASE)) {
    event.respondWith(
      caches.open(CACHE_NAME).then(async (cache) => {
        const cached = await cache.match(request);
        if (cached) return cached;
        const response = await fetch(request);
        if (response.ok) cache.put(request, response.clone());
        return response;
      })
    );
    return;
  }

  if (new URL(request.url).origin !== self.location.origin) return;

  event.respondWith(
    caches.match(request, { ignoreSearch: true }).then((cached) => {
      if (cached) return cached;
      return fetch(request).catch(() =>
        request.mode === "navigate" ? caches.match("./index.html") : Response.error()
      );
    })
  );
});

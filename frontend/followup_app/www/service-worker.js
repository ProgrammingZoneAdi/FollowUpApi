const CACHE_NAME = "followup-shell-v1";
const APP_SHELL = [
  "./",
  "./index.html",
  "./manifest.webmanifest",
  "./assets/icons/app-icon.svg",
  "./assets/css/tokens.css",
  "./assets/css/app.css",
  "./vendor/jquery/jquery.min.js",
  "./assets/js/config.js",
  "./assets/js/core/html.js",
  "./assets/js/core/storage.js",
  "./assets/js/core/api-client.js",
  "./assets/js/core/router.js",
  "./assets/js/components/toast.js",
  "./assets/js/components/app-shell.js",
  "./assets/js/features/auth/auth.service.js",
  "./assets/js/features/auth/auth.view.js",
  "./assets/js/features/dashboard/dashboard.data.js",
  "./assets/js/features/dashboard/dashboard.view.js",
  "./assets/js/features/leads/leads.data.js",
  "./assets/js/features/leads/leads.view.js",
  "./assets/js/features/shared/feature-pages.js",
  "./assets/js/app.js"
];

self.addEventListener("install", (event) => {
  event.waitUntil(caches.open(CACHE_NAME).then((cache) => cache.addAll(APP_SHELL)));
  self.skipWaiting();
});

self.addEventListener("activate", (event) => {
  event.waitUntil(
    caches
      .keys()
      .then((keys) =>
        Promise.all(keys.filter((key) => key !== CACHE_NAME).map((key) => caches.delete(key)))
      )
  );
  self.clients.claim();
});

self.addEventListener("fetch", (event) => {
  if (event.request.method !== "GET") return;

  const requestUrl = new URL(event.request.url);
  if (requestUrl.pathname.startsWith("/api/")) return;

  event.respondWith(
    fetch(event.request)
      .then((response) => {
        const copy = response.clone();
        caches.open(CACHE_NAME).then((cache) => cache.put(event.request, copy));
        return response;
      })
      .catch(() => caches.match(event.request).then((response) => response || caches.match("./index.html")))
  );
});

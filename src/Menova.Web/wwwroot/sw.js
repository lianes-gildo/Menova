const CACHE_NAME = 'menova-pwa-v2';
const STATIC_ASSETS = [
  '/offline.html',
  '/manifest.json',
  '/css/site.css',
  '/js/site.js',
  '/icons/icon-180.png',
  '/icons/icon-192.png',
  '/icons/icon-512.png',
  '/icons/icon-maskable-512.png',
  '/menova_logo_0f.png',
  '/default.jpg',
  '/lib/bootstrap/dist/css/bootstrap.min.css',
  '/lib/bootstrap/dist/js/bootstrap.bundle.min.js',
  '/lib/bootstrap-icons/font/bootstrap-icons.min.css',
  '/lib/signalr/signalr.min.js'
];

self.addEventListener('install', (event) => {
  event.waitUntil(
    caches.open(CACHE_NAME).then((cache) => {
      // Usar Promise.allSettled ou tratar individualmente para nunca abortar a instalação
      return Promise.all(
        STATIC_ASSETS.map((url) => {
          return fetch(url).then((response) => {
            if (response.ok) {
              return cache.put(url, response);
            }
          }).catch((err) => {
            console.warn('[SW] Aviso ao pré-carregar recurso:', url, err);
          });
        })
      );
    })
  );
  self.skipWaiting();
});

self.addEventListener('activate', (event) => {
  event.waitUntil(
    caches.keys().then((keys) => {
      return Promise.all(
        keys.filter((key) => key !== CACHE_NAME).map((key) => caches.delete(key))
      );
    })
  );
  self.clients.claim();
});

self.addEventListener('fetch', (event) => {
  const req = event.request;

  // Ignorar pedidos que não sejam GET
  if (req.method !== 'GET') return;

  const url = new URL(req.url);

  // Ignorar WebSockets e SignalR (/hub/)
  if (url.pathname.startsWith('/hub/') || url.pathname.startsWith('/ws')) {
    return;
  }

  // Ignorar downloads binários (APK, MobileConfig, CRT) para o navegador transferir nativamente
  if (url.pathname.includes('/download') || 
      url.pathname.endsWith('.apk') || 
      url.pathname.endsWith('.mobileconfig') || 
      url.pathname.endsWith('.crt') ||
      url.searchParams.has('download')) {
    return;
  }

  // 1. Pedidos de Navegação HTML (ex.: abrir /garcom) -> Rede primeiro, fallback para cache, fallback para offline.html
  if (req.mode === 'navigate' || req.headers.get('accept')?.includes('text/html')) {
    event.respondWith(
      fetch(req)
        .then((networkResponse) => {
          if (networkResponse.status === 200) {
            const copy = networkResponse.clone();
            caches.open(CACHE_NAME).then((cache) => cache.put(req, copy));
          }
          return networkResponse;
        })
        .catch(async () => {
          const cached = await caches.match(req);
          if (cached) return cached;
          const offlinePage = await caches.match('/offline.html');
          return offlinePage || new Response('Sem ligação à rede local.', { status: 503, headers: { 'Content-Type': 'text/plain; charset=utf-8' } });
        })
    );
    return;
  }

  // 2. Ficheiros estáticos e assets locais -> Cache primeiro, depois rede
  event.respondWith(
    caches.match(req).then((cachedResponse) => {
      if (cachedResponse) {
        return cachedResponse;
      }
      return fetch(req).then((networkResponse) => {
        if (networkResponse.status === 200 && (url.pathname.startsWith('/css/') || url.pathname.startsWith('/js/') || url.pathname.startsWith('/lib/') || url.pathname.startsWith('/icons/'))) {
          const copy = networkResponse.clone();
          caches.open(CACHE_NAME).then((cache) => cache.put(req, copy));
        }
        return networkResponse;
      });
    })
  );
});

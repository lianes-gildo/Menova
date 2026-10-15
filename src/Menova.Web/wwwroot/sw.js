const CACHE_NAME = 'menova-v1';
const ASSETS_TO_CACHE = [
  '/',
  '/garcom',
  '/css/site.css',
  '/js/site.js',
  '/lib/bootstrap/dist/css/bootstrap.min.css',
  '/lib/bootstrap/dist/js/bootstrap.bundle.min.js',
  '/lib/bootstrap-icons/font/bootstrap-icons.min.css',
  '/lib/signalr/signalr.min.js',
  '/menova_logo_0f.png',
  '/default.jpg',
  '/manifest.json'
];

self.addEventListener('install', (event) => {
  event.waitUntil(
    caches.open(CACHE_NAME).then((cache) => {
      return cache.addAll(ASSETS_TO_CACHE);
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
  // Apenas intercepta pedidos GET locais
  if (event.request.method !== 'GET') return;

  event.respondWith(
    caches.match(event.request).then((cachedResponse) => {
      if (cachedResponse) {
        return cachedResponse;
      }
      return fetch(event.request).then((response) => {
        // Guarda cópia de assets estáticos na cache
        if (response.status === 200 && event.request.url.includes('/lib/')) {
          const responseToCache = response.clone();
          caches.open(CACHE_NAME).then((cache) => {
            cache.put(event.request, responseToCache);
          });
        }
        return response;
      }).catch(() => {
        // Fallback offline caso perca a rede
        if (event.request.headers.get('accept').includes('text/html')) {
          return caches.match('/garcom');
        }
      });
    })
  );
});

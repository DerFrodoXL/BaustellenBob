// Structra Service Worker — offline caching for PWA
const CACHE_NAME = 'structra-v1';
const STATIC_ASSETS = [
    '/',
    '/manifest.webmanifest',
    '/structra-logo.png',
    '/pwa-icon-192.png',
    '/pwa-icon-512.png',
    '/_content/MudBlazor/MudBlazor.min.css',
    '/_content/MudBlazor/MudBlazor.min.js',
    '/app.css',
    '/_framework/blazor.web.js'
];

self.addEventListener('install', event => {
    event.waitUntil(
        caches.open(CACHE_NAME).then(cache => cache.addAll(STATIC_ASSETS))
    );
    self.skipWaiting();
});

self.addEventListener('activate', event => {
    event.waitUntil(
        caches.keys().then(keys =>
            Promise.all(keys.filter(k => k !== CACHE_NAME).map(k => caches.delete(k)))
        )
    );
    self.clients.claim();
});

self.addEventListener('fetch', event => {
    const request = event.request;

    // Skip non-GET and API requests
    if (request.method !== 'GET' || request.url.includes('/api/')) {
        return;
    }

    event.respondWith(
        fetch(request)
            .then(response => {
                // Only cache responses that are valid app responses:
                // – HTTP 200 (not a Railway "service starting" or error page)
                // – from our own origin (not a third-party redirect)
                // – correct content-type so we never cache Railway's HTML splash page
                const isSameOrigin = response.url.startsWith(self.location.origin);
                const contentType = response.headers.get('content-type') || '';
                const isHtml = contentType.includes('text/html');

                const pathname = new URL(request.url).pathname;
                const isStaticAsset =
                    pathname.startsWith('/_content/') ||
                    pathname === '/app.css';

                // Cache static assets that are valid 200 responses from our origin
                if (response.status === 200 && isSameOrigin && isStaticAsset && !isHtml) {
                    const clone = response.clone();
                    caches.open(CACHE_NAME).then(cache => cache.put(request, clone));
                }

                return response;
            })
            .catch(() => caches.match(request))
    );
});

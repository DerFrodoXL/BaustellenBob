// Structra Service Worker — offline caching for PWA
const CACHE_NAME = 'structra-v1';
const STATIC_ASSETS = [
    '/',
    '/manifest.webmanifest',
    '/structra-logo.png',
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
                // Cache successful responses for static assets
                if (response.ok && (request.url.includes('/_content/') || request.url.includes('/app.css'))) {
                    const clone = response.clone();
                    caches.open(CACHE_NAME).then(cache => cache.put(request, clone));
                }
                return response;
            })
            .catch(() => caches.match(request))
    );
});

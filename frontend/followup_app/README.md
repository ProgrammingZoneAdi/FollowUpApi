# FollowUp cross-platform frontend

This client is intentionally separate from the ASP.NET Core API. It uses:

- HTML and CSS for the UI
- jQuery 4 for DOM events and API requests
- a small feature-based SPA router
- a PWA manifest and service worker for installable web delivery
- Capacitor 8 native shells for Android and iOS

## Structure

```text
followup_app/
├── android/                    # Generated Capacitor Android project
├── ios/                        # Generated Capacitor iOS project
├── scripts/
│   └── copy-vendor.mjs
├── www/
│   ├── assets/
│   │   ├── css/                # Design tokens and application styles
│   │   ├── icons/
│   │   └── js/
│   │       ├── components/     # Shared application shell and toast
│   │       ├── core/           # API, router, storage and HTML helpers
│   │       └── features/       # Feature-owned UI and data
│   ├── index.html
│   ├── manifest.webmanifest
│   └── service-worker.js
├── capacitor.config.json
└── package.json
```

The feature-first layout follows the useful part of VektorPlatform's convention:
shared infrastructure stays in `core`/`components`, while each business feature
owns its screen and data adapter.

## Run on the web

```powershell
npm install
npm start
```

Open `http://localhost:5173`.

## Connect the API

Edit `www/assets/js/config.js`:

1. Set `apiBaseUrl` to the ASP.NET Core API URL.
2. Change `useMockData` to `false` after login and feature endpoints exist.
3. Keep endpoint paths in the same config file rather than hard-coding URLs in screens.

For a physical phone, `localhost` points to the phone itself. Use a reachable HTTPS
development URL or LAN address. Android emulator host access normally uses
`10.0.2.2`; production must use HTTPS.

The ASP.NET Core API must allow only the frontend origins you actually use (for
example the local web development URL and Capacitor's local origin). Do not copy
VektorPlatform's unrestricted `AllowAnyOrigin` policy into production.

## Android

Install Android Studio and its SDK, then:

```powershell
npm run sync
npm run open:android
```

Build and sign the application from Android Studio.

## iOS

The iOS project can live in this repository, but compiling/signing it requires
macOS, Xcode, CocoaPods and an Apple developer configuration:

```bash
npm run sync
npm run open:ios
```

## Important boundaries

Capacitor wraps this web UI in native applications. It is suitable for a
form/dashboard-oriented CRM such as FollowUp. Device-heavy experiences, complex
animations, or extensive offline processing may be better implemented in Flutter
or native code.

Never keep database credentials, API keys or production tokens in this frontend.
Only public runtime configuration belongs here.

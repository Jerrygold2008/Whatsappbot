# SellFlow frontend

Public website, demo and merchant dashboard for SellFlow (WhatsApp commerce).
Built with TanStack Start (server-rendered), React, Tailwind and shadcn/ui.

## Run
```bash
npm install        # or: bun install
npm run dev        # http://localhost:3000
npm run build
npm start          # runs the production build: node .output/server/index.mjs
```

## Configure
Copy `.env.example` to `.env`. The API address is set here only, never in the app:

| Variable | Purpose |
| --- | --- |
| `VITE_API_URL` | Your SellFlow API address (required) |
| `VITE_SITE_URL` | Public website address for SEO |
| `VITE_SUPPORT_WHATSAPP` / `VITE_SUPPORT_EMAIL` | Contact buttons on the "Connect WhatsApp" step |
| `VITE_GSC_VERIFICATION` | Optional Search Console token |

Your API must allow this site's address in its CORS settings (including `http://localhost:3000` while developing).

## How it works
- Public pages: `/` (home), `/demo` (sample dashboard, no sign-up). Indexed by Google.
- `/login`: sign in or create an account.
- `/setup`: first-time setup, in order: business name and description, subscription plan, WhatsApp connection.
- Dashboard (`/dashboard`, `/orders`, `/products`, `/customers`, `/payments`, `/settings`): opens once setup is complete.
  These pages are `noindex`.

Setup behaviour lives in `src/lib/setup.ts`. Set `ALLOW_SKIP_WHATSAPP` to `false` there to make WhatsApp mandatory.

## Deploy on Render
Create a **Web Service** (not a Static Site) from your repository:

| Setting | Value |
| --- | --- |
| Build command | `npm install && npm run build` |
| Start command | `npm start` |
| Environment | `VITE_API_URL`, `VITE_SITE_URL` (plus `NODE_VERSION=22`) |

`VITE_*` values are baked in at build time, so after changing one, trigger a new deploy.
If a deploy fails, Render keeps the previous version live. Check the **Events** and **Logs** tabs.
A `render.yaml` with these settings is included.

## Deploy elsewhere
The production build is a Node server (`.output/server/index.mjs`). To target another host set `NITRO_PRESET`
(for example `vercel`, `netlify` or `cloudflare-module`) before `npm run build`.
Before going live, replace `www.example.com` in `public/robots.txt` and `public/sitemap.xml`.

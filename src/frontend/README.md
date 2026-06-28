# Frontend

React Router frontend for the Say-To-Any website. The production build is configured for SSG/static hosting.

## Development

```bash
pnpm dev
```

## Build

```bash
pnpm build
```

The deployable static site is emitted to:

```text
build/client
```

Deploy `build/client` to your static host. The `build/server` folder is generated for React Router prerendering and is not needed by a static website host.

## Preview Static Build

```bash
pnpm preview:ssg
```

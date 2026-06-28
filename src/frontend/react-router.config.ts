import type { Config } from "@react-router/dev/config";

export default {
  ssr: true,
  routeDiscovery: {
    mode: "initial",
  },
  prerender: {
    paths: ["/", "/download", "/pricing", "/about"],
    concurrency: 2,
  },
} satisfies Config;

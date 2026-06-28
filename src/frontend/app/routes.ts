import { type RouteConfig, index, route } from "@react-router/dev/routes";

export default [
  index("routes/home.tsx"),
  route("download", "routes/download.tsx"),
  route("pricing", "routes/pricing.tsx"),
  route("about", "routes/about.tsx"),
] satisfies RouteConfig;

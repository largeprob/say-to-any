import { type RouteConfig, index, layout, route } from '@react-router/dev/routes';

export default [
  index('routes/index.tsx'),
  route('login', 'routes/login.tsx'),
  route('recording-dock', 'routes/recording-dock.tsx'),
  layout('routes/app-layout.tsx', [
    route('home', 'routes/home.tsx'),
    route('history', 'routes/history.tsx'),
  ]),
] satisfies RouteConfig;

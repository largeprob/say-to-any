import type { ReactNode } from 'react';
import {
  isRouteErrorResponse,
  Links,
  Meta,
  Outlet,
  Scripts,
  ScrollRestoration,
} from 'react-router';
import '../src/index.css';

export function Layout({ children }: { children: ReactNode }) {
  return (
    <html lang="zh-CN">
      <head>
        <meta charSet="utf-8" />
        <meta name="viewport" content="width=device-width, initial-scale=1.0" />
        <Meta />
        <Links />
        <title>Say To Any</title>
      </head>
      <body>
        {children}
        <ScrollRestoration />
        <Scripts />
      </body>
    </html>
  );
}

export default function Root() {
  return <Outlet />;
}

export function ErrorBoundary({ error }: { error: unknown }) {
  let title = '发生错误';
  let detail = '应用遇到了一个未处理的问题。';

  if (isRouteErrorResponse(error)) {
    title = error.status === 404 ? '404' : '路由错误';
    detail = error.statusText || detail;
  } else if (error instanceof Error) {
    detail = error.message;
  }

  return (
    <main className="panel">
      <div className="panel-header">
        <div>
          <h1>{title}</h1>
          <p>{detail}</p>
        </div>
      </div>
    </main>
  );
}

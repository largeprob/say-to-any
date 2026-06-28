import { useOutletContext } from 'react-router';
import { HomeRoute } from '../../src/routes/HomeRoute';
import type { AppRouteContext } from '../../src/routes/route-context';

export default function HomePage() {
  const context = useOutletContext<AppRouteContext>();
  return <HomeRoute {...context} />;
}

import { useOutletContext } from 'react-router';
import { HistoryRoute } from '../../src/routes/HistoryRoute';
import type { AppRouteContext } from '../../src/routes/route-context';

export default function HistoryPage() {
  const context = useOutletContext<AppRouteContext>();
  return <HistoryRoute {...context} />;
}

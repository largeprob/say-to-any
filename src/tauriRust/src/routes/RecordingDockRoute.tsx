import { useEffect, useLayoutEffect, useState } from 'react';
import { emitTo } from '@tauri-apps/api/event';
import { RecordingDockControl } from '../components/RecordingDockControl';

const isTauri = () => '__TAURI_INTERNALS__' in window;

function useDockWaveBars() {
  const [bars, setBars] = useState([10, 10, 10, 10, 10]);

  useEffect(() => {
    let frame = 0;
    const timer = window.setInterval(() => {
      frame += 1;
      setBars(
        Array.from({ length: 5 }, (_, index) => {
          const motion = (Math.sin(frame * 0.7 + index * 1.2) + 1) / 2;
          return Math.round(10 + motion * 28);
        }),
      );
    }, 90);

    return () => window.clearInterval(timer);
  }, []);

  return bars;
}

export default function RecordingDockRoute() {
  const bars = useDockWaveBars();
  const [isBusy, setIsBusy] = useState(false);

  useLayoutEffect(() => {
    document.documentElement.classList.add('recording-window-document');
    document.body.classList.add('recording-window-body');

    return () => {
      document.documentElement.classList.remove('recording-window-document');
      document.body.classList.remove('recording-window-body');
    };
  }, []);

  useEffect(() => {
    if (!isTauri()) {
      return;
    }

    const timer = window.setTimeout(() => {
      void emitTo('main', 'recording-dock-ready');
    }, 80);

    return () => window.clearTimeout(timer);
  }, []);

  async function requestCancel() {
    if (isBusy) {
      return;
    }

    setIsBusy(true);
    try {
      if (isTauri()) {
        await emitTo('main', 'recording-dock-cancel');
      }
    } catch {
      setIsBusy(false);
    }
  }

  async function requestFinish() {
    if (isBusy) {
      return;
    }

    setIsBusy(true);
    try {
      if (isTauri()) {
        await emitTo('main', 'recording-dock-finish');
      }
    } catch {
      setIsBusy(false);
    }
  }

  return (
    <main className="recording-window-page">
      <RecordingDockControl
        bars={bars}
        isBusy={isBusy}
        onCancel={() => void requestCancel()}
        onFinish={() => void requestFinish()}
      />
    </main>
  );
}

import { listen, once } from '@tauri-apps/api/event';
import { WebviewWindow } from '@tauri-apps/api/webviewWindow';
import {
  currentMonitor,
  LogicalPosition,
  LogicalSize,
  primaryMonitor,
  type Monitor,
} from '@tauri-apps/api/window';

const RECORDING_DOCK_LABEL = 'recording-dock';
const RECORDING_DOCK_WIDTH = 430;
const RECORDING_DOCK_HEIGHT = 94;
const RECORDING_DOCK_VISIBLE_BOTTOM_GAP = 34;
const RECORDING_DOCK_WINDOW_BOTTOM_PADDING = 14;
const RECORDING_DOCK_OFFSCREEN_POSITION = -10_000;
const TRANSPARENT_BACKGROUND: [number, number, number, number] = [0, 0, 0, 0];

const isTauri = () => '__TAURI_INTERNALS__' in window;

let dockReadyPromise: Promise<boolean> | null = null;
let isDockReady = false;

type RecordingDockHandlers = {
  onCancel: () => void;
  onFinish: () => void;
};

function getDockPosition(monitor: Monitor) {
  const scaleFactor = monitor.scaleFactor || 1;
  const workAreaPosition = monitor.workArea.position.toLogical(scaleFactor);
  const workAreaSize = monitor.workArea.size.toLogical(scaleFactor);
  const bottomGap = RECORDING_DOCK_VISIBLE_BOTTOM_GAP - RECORDING_DOCK_WINDOW_BOTTOM_PADDING;

  return new LogicalPosition(
    Math.round(workAreaPosition.x + (workAreaSize.width - RECORDING_DOCK_WIDTH) / 2),
    Math.round(workAreaPosition.y + workAreaSize.height - RECORDING_DOCK_HEIGHT - bottomGap),
  );
}

async function resolveDockPosition() {
  const monitor = (await currentMonitor()) ?? (await primaryMonitor());
  return monitor ? getDockPosition(monitor) : undefined;
}

async function getRecordingDockWindow() {
  return WebviewWindow.getByLabel(RECORDING_DOCK_LABEL);
}

async function createHiddenRecordingDockWindow() {
  let createdDockWindow: WebviewWindow;

  return new Promise<boolean>((resolve) => {
    let settled = false;
    let unlistenReady = () => {};
    const timeout = window.setTimeout(() => settle(false), 1800);

    function settle(value: boolean) {
      if (!settled) {
        settled = true;
        isDockReady = value;
        window.clearTimeout(timeout);
        unlistenReady();
        if (!value) {
          void createdDockWindow.destroy();
        }
        resolve(value);
      }
    }

    void once('recording-dock-ready', () => settle(true))
      .then((unlisten) => {
        if (settled) {
          unlisten();
          return;
        }

        unlistenReady = unlisten;
      })
      .catch(() => settle(false));

    createdDockWindow = new WebviewWindow(RECORDING_DOCK_LABEL, {
      alwaysOnTop: true,
      backgroundColor: TRANSPARENT_BACKGROUND,
      closable: false,
      decorations: false,
      focus: false,
      height: RECORDING_DOCK_HEIGHT,
      maximizable: false,
      minimizable: false,
      resizable: false,
      shadow: false,
      skipTaskbar: true,
      title: 'Recording',
      transparent: true,
      url: '/recording-dock',
      visible: false,
      width: RECORDING_DOCK_WIDTH,
      x: RECORDING_DOCK_OFFSCREEN_POSITION,
      y: RECORDING_DOCK_OFFSCREEN_POSITION,
    });

    void createdDockWindow.once('tauri://error', () => settle(false));
  });
}

export async function prepareRecordingDockWindow() {
  if (!isTauri()) {
    return false;
  }

  if (dockReadyPromise) {
    return dockReadyPromise;
  }

  dockReadyPromise = (async () => {
    try {
      const position = await resolveDockPosition();
      const dockWindow = await getRecordingDockWindow();

      if (dockWindow) {
        await Promise.all([
          dockWindow.setSize(new LogicalSize(RECORDING_DOCK_WIDTH, RECORDING_DOCK_HEIGHT)),
          position ? dockWindow.setPosition(position) : Promise.resolve(),
          dockWindow.setAlwaysOnTop(true),
          dockWindow.hide(),
        ]);
        isDockReady = true;
        return true;
      }

      return createHiddenRecordingDockWindow();
    } catch {
      isDockReady = false;
      return false;
    }
  })().finally(() => {
    dockReadyPromise = null;
  });

  return dockReadyPromise;
}

export async function showRecordingDockWindow() {
  if (!isTauri()) {
    return false;
  }

  try {
    const position = await resolveDockPosition();
    const dockWindow = await getRecordingDockWindow();

    if (!dockWindow || !isDockReady) {
      void prepareRecordingDockWindow();
      return false;
    }

    await Promise.all([
      dockWindow.setSize(new LogicalSize(RECORDING_DOCK_WIDTH, RECORDING_DOCK_HEIGHT)),
      position ? dockWindow.setPosition(position) : Promise.resolve(),
      dockWindow.setAlwaysOnTop(true),
    ]);
    await dockWindow.show();
    return true;
  } catch {
    return false;
  }
}

export async function hideRecordingDockWindow() {
  if (!isTauri()) {
    return;
  }

  try {
    const dockWindow = await getRecordingDockWindow();
    await dockWindow?.hide();
  } catch {
    // The recording dock is best-effort UI; recording state is owned by App.tsx.
  }
}

export async function listenRecordingDockRequests({ onCancel, onFinish }: RecordingDockHandlers) {
  if (!isTauri()) {
    return () => {};
  }

  const unlistenCancel = await listen('recording-dock-cancel', onCancel);
  const unlistenFinish = await listen('recording-dock-finish', onFinish);

  return () => {
    unlistenCancel();
    unlistenFinish();
  };
}

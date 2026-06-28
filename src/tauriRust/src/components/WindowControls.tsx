import { Maximize2, Minus, X } from 'lucide-react';
import { getCurrentWindow } from '@tauri-apps/api/window';
import { Icon } from './Icon';

export async function minimizeWindow() {
  await getCurrentWindow().minimize();
}

export async function toggleWindowMaximize() {
  await getCurrentWindow().toggleMaximize();
}

export async function closeWindow() {
  await getCurrentWindow().close();
}

export function WindowControls() {
  return (
    <div className="window-controls">
      <button
        aria-label="最小化"
        className="window-control-button"
        onClick={() => void minimizeWindow()}
        type="button"
      >
        <Icon icon={Minus} size={15} />
      </button>
      <button
        aria-label="最大化"
        className="window-control-button"
        onClick={() => void toggleWindowMaximize()}
        type="button"
      >
        <Icon icon={Maximize2} size={13} />
      </button>
      <button
        aria-label="关闭"
        className="window-control-button close"
        onClick={() => void closeWindow()}
        type="button"
      >
        <Icon icon={X} size={15} />
      </button>
    </div>
  );
}

import { X } from 'lucide-react';
import { Icon } from './Icon';
import { WaveBars } from './WaveBars';

type RecordingDockControlProps = {
  bars: number[];
  isBusy?: boolean;
  onCancel: () => void;
  onFinish: () => void;
};

export function RecordingDockControl({
  bars,
  isBusy = false,
  onCancel,
  onFinish,
}: RecordingDockControlProps) {
  const dockBars = [...bars, ...bars.slice(0, -1).reverse()];

  return (
    <div className="recording-dock">
      <button
        aria-label="取消录音"
        className="recording-dock-button recording-dock-cancel"
        disabled={isBusy}
        onClick={onCancel}
        type="button"
      >
        <Icon icon={X} size={18} />
      </button>
      <div className="recording-dock-wave" aria-hidden="true">
        <WaveBars bars={dockBars} />
      </div>
      <button
        className="recording-dock-button recording-dock-finish"
        disabled={isBusy}
        onClick={onFinish}
        type="button"
      >
        完成
      </button>
    </div>
  );
}

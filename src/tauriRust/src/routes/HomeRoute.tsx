import { Activity, ClipboardPaste, Copy, Mic, Square } from 'lucide-react';
import { Button, Card } from '@heroui/react';
import { Icon } from '../components/Icon';
import { WaveBars } from '../components/WaveBars';

export type HomeRouteProps = {
  cancelDictation: () => Promise<void>;
  copyFinalText: () => Promise<void>;
  finalText: string;
  finishDictation: () => Promise<void>;
  isBusy: boolean;
  isRecording: boolean;
  pasteFinalText: () => Promise<void>;
  processingProgress: number;
  rawTranscript: string;
  setFinalText: (value: string) => void;
  startDictation: () => Promise<void>;
  statusMessage: string;
  waveBars: number[];
};

export function HomeRoute({
  cancelDictation,
  copyFinalText,
  finalText,
  finishDictation,
  isBusy,
  isRecording,
  pasteFinalText,
  processingProgress,
  rawTranscript,
  setFinalText,
  startDictation,
  statusMessage,
  waveBars,
}: HomeRouteProps) {
  return (
    <section className="panel">
      <div className="panel-header">
        <div>
          <h1>听写</h1>
          <p>OpenAI 兼容语音识别、文本整理和自动粘贴。</p>
        </div>
        <div className="header-actions">
          {isRecording ? (
            <>
              <Button onPress={() => void cancelDictation()} variant="tertiary">
                取消
              </Button>
              <Button className="danger-button" onPress={() => void finishDictation()} variant="danger">
                <Icon icon={Square} size={15} />
                停止并识别
              </Button>
            </>
          ) : (
            <Button
              className="primary-button"
              isDisabled={isBusy}
              onPress={() => void startDictation()}
            >
              <Icon icon={Mic} size={17} />
              开始听写
            </Button>
          )}
        </div>
      </div>

      <Card className="transcript-card" variant="default">
        <Card.Header className="transcript-header">
          <div>
            <Card.Title className="text-[15px] text-[#102a56]">识别结果</Card.Title>
            <Card.Description className="text-[#6e7d99]">{statusMessage}</Card.Description>
          </div>
          <div className="inline-actions">
            <Button isIconOnly onPress={() => void copyFinalText()} variant="ghost" aria-label="复制">
              <Icon icon={Copy} size={17} />
            </Button>
            <Button isIconOnly onPress={() => void pasteFinalText()} variant="ghost" aria-label="粘贴">
              <Icon icon={ClipboardPaste} size={17} />
            </Button>
          </div>
        </Card.Header>
        <Card.Content>
          <textarea
            className="transcript-input"
            onChange={(event) => setFinalText(event.target.value)}
            placeholder="完成听写后，整理后的文本会显示在这里。"
            value={finalText}
          />
        </Card.Content>
      </Card>

      {(isRecording || isBusy) && (
        <Card className="control-overlay-card" variant="secondary">
          <Card.Content className="control-content">
            {isRecording ? (
              <>
                <div className="recording-indicator">
                  <span className="stop-dot">
                    <span />
                  </span>
                  <WaveBars bars={waveBars} />
                </div>
                <div>
                  <div className="control-title">录音中...</div>
                  <div className="control-caption">再次点击停止并开始识别</div>
                </div>
              </>
            ) : (
              <>
                <div className="processing-icon">
                  <Icon icon={Activity} size={20} />
                </div>
                <div className="processing-copy">
                  <div className="control-title">语音识别中...</div>
                  <div className="processing-track">
                    <span style={{ transform: `scaleX(${Math.min(processingProgress, 100) / 100})` }} />
                  </div>
                </div>
              </>
            )}
          </Card.Content>
        </Card>
      )}

      {rawTranscript ? (
        <Card className="raw-card" variant="transparent">
          <Card.Header>
            <Card.Title className="text-[14px] text-[#526681]">原始识别文本</Card.Title>
          </Card.Header>
          <Card.Content>
            <p>{rawTranscript}</p>
          </Card.Content>
        </Card>
      ) : null}
    </section>
  );
}

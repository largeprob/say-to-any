import { Copy, Download, History, Trash2 } from 'lucide-react';
import { Button } from '@heroui/react';
import { Icon } from '../components/Icon';
import type { HistoryItem } from '../lib/types';

export type HistoryGroup = {
  title: string;
  items: HistoryItem[];
};

export type HistoryRouteProps = {
  copyHistoryText: (item: HistoryItem) => Promise<void>;
  groups: HistoryGroup[];
  removeHistoryItem: (item: HistoryItem) => Promise<void>;
};

export function HistoryRoute({ copyHistoryText, groups, removeHistoryItem }: HistoryRouteProps) {
  const total = groups.reduce((count, group) => count + group.items.length, 0);

  return (
    <section className="panel">
      <div className="panel-header">
        <div>
          <h1>历史记录</h1>
          <p>{total === 0 ? '暂无听写记录。' : `${total} 条听写结果，按日期分组。`}</p>
        </div>
      </div>

      {total === 0 ? (
        <div className="empty-state">
          <Icon icon={History} size={28} />
          <span>暂无听写记录</span>
        </div>
      ) : (
        <div className="history-list">
          {groups.map((group) => (
            <section key={group.title}>
              <div className="history-group-heading">
                <span>{group.title}</span>
                <i />
              </div>
              {group.items.map((item) => (
                <article className="history-item" key={item.id}>
                  <div>
                    <div className="history-meta">
                      <span>{formatHistoryTime(item.createdAt)}</span>
                      <span>{item.audioFilePath ? '音频已保存' : '音频不可用'}</span>
                    </div>
                    <p>{item.finalText}</p>
                  </div>
                  <div className="history-actions">
                    <Button
                      aria-label="复制"
                      className="history-icon-button"
                      isIconOnly
                      onPress={() => void copyHistoryText(item)}
                      variant="ghost"
                    >
                      <Icon icon={Copy} size={15} />
                    </Button>
                    <Button
                      aria-label="下载音频"
                      className="history-icon-button"
                      isDisabled={!item.audioFilePath}
                      isIconOnly
                      variant="ghost"
                    >
                      <Icon icon={Download} size={15} />
                    </Button>
                    <Button
                      aria-label="删除"
                      className="history-icon-button danger-icon-button"
                      isIconOnly
                      onPress={() => void removeHistoryItem(item)}
                      variant="ghost"
                    >
                      <Icon icon={Trash2} size={15} />
                    </Button>
                  </div>
                </article>
              ))}
            </section>
          ))}
        </div>
      )}
    </section>
  );
}

function formatHistoryTime(value: string) {
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return value;
  }

  return `${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(
    2,
    '0',
  )} ${String(date.getHours()).padStart(2, '0')}:${String(date.getMinutes()).padStart(2, '0')}`;
}

import type { LucideIcon } from 'lucide-react';

type IconProps = {
  icon: LucideIcon;
  size?: number;
  className?: string;
};

export function Icon({ icon: IconComponent, size = 18, className }: IconProps) {
  return <IconComponent aria-hidden="true" className={className} size={size} strokeWidth={2} />;
}

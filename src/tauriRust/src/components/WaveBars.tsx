export function WaveBars({ bars }: { bars: number[] }) {
  return (
    <span className="wave-bars">
      {bars.map((height, index) => (
        <i key={index} style={{ height }} />
      ))}
    </span>
  );
}

interface RadarChartProps {
  dominance: number;
  influence: number;
  steadiness: number;
  conscientiousness: number;
  size?: number;
}

export const RadarChart = ({
  dominance,
  influence,
  steadiness,
  conscientiousness,
  size,
}: RadarChartProps) => {
  // Use a canonical 300x300 viewBox coordinate space
  const viewBoxSize = 300;
  const center = viewBoxSize / 2; // 150
  const radius = 95; // Radius leaves ample space for outer labels

  // 4 Axes:
  // Top: Dominance (D) -> (0, -1)
  // Right: Influence (I) -> (1, 0)
  // Bottom: Steadiness (S) -> (0, 1)
  // Left: Conscientiousness (C) -> (-1, 0)

  const dNorm = Math.max(0, Math.min(100, dominance)) / 100;
  const iNorm = Math.max(0, Math.min(100, influence)) / 100;
  const sNorm = Math.max(0, Math.min(100, steadiness)) / 100;
  const cNorm = Math.max(0, Math.min(100, conscientiousness)) / 100;

  const points = [
    { x: center, y: center - radius * dNorm, label: 'D', value: dominance, color: '#ef4444' },
    { x: center + radius * iNorm, y: center, label: 'I', value: influence, color: '#f59e0b' },
    { x: center, y: center + radius * sNorm, label: 'S', value: steadiness, color: '#10b981' },
    { x: center - radius * cNorm, y: center, label: 'C', value: conscientiousness, color: '#3b82f6' },
  ];

  const polygonPath = points.map((p) => `${p.x},${p.y}`).join(' ');

  // Grid levels (25%, 50%, 75%, 100%)
  const gridLevels = [0.25, 0.5, 0.75, 1.0];

  return (
    <div
      className="w-full flex items-center justify-center"
      style={{ maxWidth: size ? `${size}px` : '260px' }}
    >
      <svg
        viewBox={`0 0 ${viewBoxSize} ${viewBoxSize}`}
        className="w-full h-auto max-w-full select-none"
      >
        {/* Background Grid */}
        {gridLevels.map((lvl, idx) => {
          const r = radius * lvl;
          const gridPoints = `${center},${center - r} ${center + r},${center} ${center},${center + r} ${center - r},${center}`;
          return (
            <polygon
              key={idx}
              points={gridPoints}
              fill="none"
              stroke="#e2e8f0"
              strokeWidth="1"
              strokeDasharray={lvl === 1.0 ? '' : '3 3'}
            />
          );
        })}

        {/* Axes Lines */}
        <line x1={center} y1={center - radius} x2={center} y2={center + radius} stroke="#cbd5e1" strokeWidth="1" />
        <line x1={center - radius} y1={center} x2={center + radius} y2={center} stroke="#cbd5e1" strokeWidth="1" />

        {/* Data Polygon */}
        <polygon
          points={polygonPath}
          fill="rgba(14, 140, 233, 0.25)"
          stroke="#0e8ce9"
          strokeWidth="2.5"
          className="transition-all duration-300"
        />

        {/* Vertex Points */}
        {points.map((p, idx) => (
          <g key={idx} className="transition-all duration-300">
            <circle cx={p.x} cy={p.y} r="5" fill={p.color} stroke="#ffffff" strokeWidth="2" />
          </g>
        ))}

        {/* Axis Labels outside with safe padding */}
        <text
          x={center}
          y={center - radius - 12}
          textAnchor="middle"
          className="text-[13px] font-bold fill-red-600"
        >
          D ({dominance}%)
        </text>
        <text
          x={center + radius + 10}
          y={center + 4}
          textAnchor="start"
          className="text-[13px] font-bold fill-amber-600"
        >
          I ({influence}%)
        </text>
        <text
          x={center}
          y={center + radius + 20}
          textAnchor="middle"
          className="text-[13px] font-bold fill-emerald-600"
        >
          S ({steadiness}%)
        </text>
        <text
          x={center - radius - 10}
          y={center + 4}
          textAnchor="end"
          className="text-[13px] font-bold fill-blue-600"
        >
          C ({conscientiousness}%)
        </text>
      </svg>
    </div>
  );
};

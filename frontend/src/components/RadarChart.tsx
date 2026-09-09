import React from 'react';

export interface DimensionItem {
  label: string;
  value: number;
  color?: string;
}

interface RadarChartProps {
  dominance?: number;
  influence?: number;
  steadiness?: number;
  conscientiousness?: number;
  dimensions?: Record<string, number> | DimensionItem[];
  size?: number;
}

const DEFAULT_PALETTE = ['#ef4444', '#f59e0b', '#10b981', '#3b82f6', '#8b5cf6', '#ec4899', '#06b6d4', '#14b8a6'];

export const RadarChart: React.FC<RadarChartProps> = ({
  dominance,
  influence,
  steadiness,
  conscientiousness,
  dimensions,
  size,
}) => {
  // Construir lista unificada de dimensiones
  let items: DimensionItem[] = [];

  if (dimensions) {
    if (Array.isArray(dimensions)) {
      items = dimensions;
    } else {
      items = Object.entries(dimensions).map(([key, val], idx) => ({
        label: key,
        value: val,
        color: DEFAULT_PALETTE[idx % DEFAULT_PALETTE.length],
      }));
    }
  } else if (
    dominance !== undefined ||
    influence !== undefined ||
    steadiness !== undefined ||
    conscientiousness !== undefined
  ) {
    items = [
      { label: 'D', value: dominance ?? 50, color: '#ef4444' },
      { label: 'I', value: influence ?? 50, color: '#f59e0b' },
      { label: 'S', value: steadiness ?? 50, color: '#10b981' },
      { label: 'C', value: conscientiousness ?? 50, color: '#3b82f6' },
    ];
  } else {
    items = [
      { label: 'D', value: 50, color: '#ef4444' },
      { label: 'I', value: 50, color: '#f59e0b' },
      { label: 'S', value: 50, color: '#10b981' },
      { label: 'C', value: 50, color: '#3b82f6' },
    ];
  }

  const n = items.length;
  const viewBoxSize = 300;
  const center = viewBoxSize / 2;
  const radius = 95;

  // Calcular puntos poligonales de los datos
  const dataPoints = items.map((item, i) => {
    const angle = -Math.PI / 2 + (2 * Math.PI * i) / n;
    const norm = Math.max(0, Math.min(100, item.value)) / 100;
    const r = radius * norm;
    return {
      x: center + r * Math.cos(angle),
      y: center + r * Math.sin(angle),
      label: item.label,
      value: Math.round(item.value),
      color: item.color || DEFAULT_PALETTE[i % DEFAULT_PALETTE.length],
      angle,
    };
  });

  const polygonPath = dataPoints.map((p) => `${p.x.toFixed(1)},${p.y.toFixed(1)}`).join(' ');

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
          const gridPolygon = Array.from({ length: n })
            .map((_, i) => {
              const angle = -Math.PI / 2 + (2 * Math.PI * i) / n;
              const gx = center + r * Math.cos(angle);
              const gy = center + r * Math.sin(angle);
              return `${gx.toFixed(1)},${gy.toFixed(1)}`;
            })
            .join(' ');

          return (
            <polygon
              key={idx}
              points={gridPolygon}
              fill="none"
              stroke="#e2e8f0"
              strokeWidth="1"
              strokeDasharray={lvl === 1.0 ? '' : '3 3'}
            />
          );
        })}

        {/* Axes Lines */}
        {Array.from({ length: n }).map((_, i) => {
          const angle = -Math.PI / 2 + (2 * Math.PI * i) / n;
          const ax = center + radius * Math.cos(angle);
          const ay = center + radius * Math.sin(angle);
          return (
            <line
              key={i}
              x1={center}
              y1={center}
              x2={ax}
              y2={ay}
              stroke="#cbd5e1"
              strokeWidth="1"
            />
          );
        })}

        {/* Data Polygon */}
        <polygon
          points={polygonPath}
          fill="rgba(14, 140, 233, 0.25)"
          stroke="#0e8ce9"
          strokeWidth="2.5"
          className="transition-all duration-300"
        />

        {/* Vertex Points */}
        {dataPoints.map((p, idx) => (
          <g key={idx} className="transition-all duration-300">
            <circle cx={p.x} cy={p.y} r="5" fill={p.color} stroke="#ffffff" strokeWidth="2" />
          </g>
        ))}

        {/* Axis Labels outside with safe padding */}
        {dataPoints.map((p, idx) => {
          const labelRadius = radius + 18;
          const lx = center + labelRadius * Math.cos(p.angle);
          const ly = center + labelRadius * Math.sin(p.angle);

          let anchor: 'middle' | 'start' | 'end' = 'middle';
          const cosVal = Math.cos(p.angle);
          if (cosVal > 0.3) anchor = 'start';
          else if (cosVal < -0.3) anchor = 'end';

          return (
            <text
              key={idx}
              x={lx}
              y={ly + 4}
              textAnchor={anchor}
              className="text-[12px] font-bold"
              fill={p.color}
            >
              {p.label} ({p.value}%)
            </text>
          );
        })}
      </svg>
    </div>
  );
};

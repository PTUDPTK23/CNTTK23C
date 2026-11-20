import { useId, useMemo, useState } from 'react'
import { formatMoney, timeAgo } from '../lib/format'
import type { Bid } from '../lib/types'

export function PriceChart({
  bids,
  startingPrice,
  height = 190
}: {
  bids: Bid[]
  startingPrice: number
  height?: number
}) {
  const gradientId = useId()
  const [hover, setHover] = useState<number | null>(null)

  const series = useMemo(() => {
    const ordered = [...bids].sort(
      (a, b) => new Date(a.placedAtUtc).getTime() - new Date(b.placedAtUtc).getTime()
    )
    return ordered.map((bid) => ({ amount: bid.amount, at: bid.placedAtUtc, name: bid.bidderName }))
  }, [bids])

  const width = 640
  const paddingX = 18
  const paddingTop = 18
  const paddingBottom = 26
  const innerHeight = height - paddingTop - paddingBottom

  const { path, area, points, min, max } = useMemo(() => {
    const values = [startingPrice, ...series.map((point) => point.amount)]
    const minValue = Math.min(...values)
    const maxValue = Math.max(...values)
    const span = maxValue - minValue || 1
    const usableWidth = width - paddingX * 2

    const coordinates = series.map((point, index) => {
      const x = paddingX + (series.length <= 1 ? usableWidth / 2 : (index / (series.length - 1)) * usableWidth)
      const y = paddingTop + innerHeight - ((point.amount - minValue) / span) * innerHeight
      return { x, y, ...point }
    })

    const line = coordinates
      .map((point, index) => `${index === 0 ? 'M' : 'L'}${point.x.toFixed(1)},${point.y.toFixed(1)}`)
      .join(' ')

    const areaPath = coordinates.length
      ? `${line} L${coordinates[coordinates.length - 1].x.toFixed(1)},${paddingTop + innerHeight} L${coordinates[0].x.toFixed(1)},${paddingTop + innerHeight} Z`
      : ''

    return { path: line, area: areaPath, points: coordinates, min: minValue, max: maxValue }
  }, [series, startingPrice, innerHeight])

  if (series.length === 0) {
    return (
      <div
        className="grid place-items-center rounded-xl border border-dashed border-white/10 bg-ink-900/40 text-center"
        style={{ height }}
      >
        <div>
          <p className="text-xs font-semibold text-slate-400">Chưa có lượt đặt giá</p>
          <p className="mt-1 text-[11px] text-slate-600">Đường giá bắt đầu từ lượt đặt giá đầu tiên.</p>
        </div>
      </div>
    )
  }

  if (series.length === 1) {
    return (
      <div
        className="grid place-items-center rounded-xl border border-white/[0.07] bg-ink-900/40"
        style={{ height }}
      >
        <div className="text-center">
          <p className="text-[10px] uppercase tracking-[0.16em] text-slate-500">Lượt đặt giá đầu tiên</p>
          <p className="font-display text-2xl font-semibold text-gold-300">{formatMoney(series[0].amount)}</p>
          <p className="mt-1 text-[11px] text-slate-500">
            bởi {series[0].name} · {timeAgo(series[0].at)}
          </p>
        </div>
      </div>
    )
  }

  return (
    <div className="rounded-xl border border-white/[0.07] bg-ink-900/40 p-2">
      <svg
        viewBox={`0 0 ${width} ${height}`}
        className="w-full"
        style={{ height }}
        role="img"
        aria-label="Biểu đồ lịch sử giá đấu"
        onMouseLeave={() => setHover(null)}
      >
        <defs>
          <linearGradient id={gradientId} x1="0" x2="0" y1="0" y2="1">
            <stop offset="0%" stopColor="#d4af37" stopOpacity="0.35" />
            <stop offset="100%" stopColor="#d4af37" stopOpacity="0" />
          </linearGradient>
        </defs>

        {[0, 0.5, 1].map((ratio) => {
          const y = paddingTop + innerHeight * ratio
          return (
            <line
              key={ratio}
              x1={paddingX}
              x2={width - paddingX}
              y1={y}
              y2={y}
              stroke="rgba(255,255,255,0.06)"
              strokeDasharray="4 6"
            />
          )
        })}

        <path d={area} fill={`url(#${gradientId})`} />
        <path d={path} fill="none" stroke="#e3c06a" strokeWidth="2.2" strokeLinecap="round" strokeLinejoin="round" />

        {points.map((point, index) => (
          <g key={`${point.at}-${index}`}>
            <circle
              cx={point.x}
              cy={point.y}
              r={hover === index ? 5 : 3}
              fill={hover === index ? '#f6e7b4' : '#0b1322'}
              stroke="#e3c06a"
              strokeWidth="1.8"
            />
            <rect
              x={point.x - 12}
              y={paddingTop}
              width={24}
              height={innerHeight}
              fill="transparent"
              onMouseEnter={() => setHover(index)}
            />
          </g>
        ))}

        <text x={paddingX} y={height - 8} fill="rgba(148,163,184,0.65)" fontSize="10">
          {new Date(points[0].at).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
        </text>
        <text x={width - paddingX} y={height - 8} textAnchor="end" fill="rgba(148,163,184,0.65)" fontSize="10">
          {new Date(points[points.length - 1].at).toLocaleTimeString([], {
            hour: '2-digit',
            minute: '2-digit'
          })}
        </text>
        <text x={paddingX} y={paddingTop - 5} fill="rgba(148,163,184,0.55)" fontSize="10">
          {formatMoney(max)}
        </text>
        <text x={paddingX} y={paddingTop + innerHeight - 4} fill="rgba(148,163,184,0.55)" fontSize="10">
          {formatMoney(min)}
        </text>
      </svg>

      <div className="flex items-center justify-between px-3 pb-1 text-[11px] text-slate-500">
        <span>
          {hover !== null ? (
            <>
              <span className="font-semibold text-gold-300">{formatMoney(points[hover].amount)}</span> ·{' '}
              {points[hover].name}
            </>
          ) : (
            `${series.length} lượt đặt giá đã xác thực`
          )}
        </span>
        <span>{hover !== null ? timeAgo(points[hover].at) : 'Rê chuột lên biểu đồ để xem chi tiết'}</span>
      </div>
    </div>
  )
}

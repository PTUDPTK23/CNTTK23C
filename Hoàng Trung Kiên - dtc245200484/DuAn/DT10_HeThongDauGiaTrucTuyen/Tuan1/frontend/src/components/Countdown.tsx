import { useEffect, useRef, useState } from 'react'
import { formatCountdown } from '../lib/format'

interface CountdownProps {
  endTimeUtc: string
  serverOffsetMs?: number
  variant?: 'compact' | 'panel' | 'inline'
  onEnded?: () => void
  className?: string
}

export function Countdown({
  endTimeUtc,
  serverOffsetMs = 0,
  variant = 'compact',
  onEnded,
  className = ''
}: CountdownProps) {
  const calculate = () => new Date(endTimeUtc).getTime() - (Date.now() + serverOffsetMs)
  const [remaining, setRemaining] = useState(calculate)
  const endedRef = useRef(false)
  const onEndedRef = useRef(onEnded)
  onEndedRef.current = onEnded

  useEffect(() => {
    endedRef.current = false
    setRemaining(calculate())
    const timer = window.setInterval(() => {
      const next = calculate()
      setRemaining(next)
      if (next <= 0 && !endedRef.current) {
        endedRef.current = true
        onEndedRef.current?.()
      }
    }, 1000)
    return () => window.clearInterval(timer)
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [endTimeUtc, serverOffsetMs])

  const parts = formatCountdown(remaining)

  if (parts.ended) {
    return (
      <span className={`inline-flex items-center gap-1.5 text-xs font-semibold text-slate-400 ${className}`}>
        <span className="h-1.5 w-1.5 rounded-full bg-slate-500" />
        Phiên đã kết thúc
      </span>
    )
  }

  if (variant === 'compact') {
    return (
      <span
        className={`inline-flex items-center gap-1.5 rounded-full border px-2.5 py-1 text-[11px] font-semibold tabular-nums ${
          parts.urgent
            ? 'border-rose-400/30 bg-rose-400/10 text-rose-300'
            : 'border-white/10 bg-white/[0.04] text-slate-300'
        } ${className}`}
      >
        <span
          className={`h-1.5 w-1.5 rounded-full ${parts.urgent ? 'animate-pulse bg-rose-400' : 'bg-emerald-400'}`}
        />
        {parts.compact}
      </span>
    )
  }

  if (variant === 'inline') {
    return (
      <span className={`font-display text-sm font-semibold tabular-nums ${parts.urgent ? 'text-rose-300' : 'text-white'} ${className}`}>
        {parts.days !== '0' ? `${parts.days} ngày ` : ''}
        {parts.hours}:{parts.minutes}:{parts.seconds}
      </span>
    )
  }

  const cells = [
    { value: parts.days, label: 'Ngày' },
    { value: parts.hours, label: 'Giờ' },
    { value: parts.minutes, label: 'Phút' },
    { value: parts.seconds, label: 'Giây' }
  ]

  return (
    <div className={`grid grid-cols-4 gap-2 ${className}`}>
      {cells.map((cell) => (
        <div
          key={cell.label}
          className={`rounded-xl border px-2 py-3 text-center ${
            parts.urgent ? 'border-rose-400/20 bg-rose-400/[0.06]' : 'border-white/[0.08] bg-white/[0.03]'
          }`}
        >
          <p
            className={`font-display text-xl font-semibold tabular-nums ${
              parts.urgent ? 'text-rose-300' : 'text-white'
            }`}
          >
            {cell.value}
          </p>
          <p className="mt-0.5 text-[9px] font-semibold uppercase tracking-[0.16em] text-slate-500">
            {cell.label}
          </p>
        </div>
      ))}
    </div>
  )
}

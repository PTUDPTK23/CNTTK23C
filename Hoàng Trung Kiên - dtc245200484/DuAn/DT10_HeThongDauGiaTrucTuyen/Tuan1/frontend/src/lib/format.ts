const money = new Intl.NumberFormat('vi-VN', {
  style: 'currency',
  currency: 'USD',
  maximumFractionDigits: 0
})

const moneyPrecise = new Intl.NumberFormat('vi-VN', {
  style: 'currency',
  currency: 'USD',
  minimumFractionDigits: 2,
  maximumFractionDigits: 2
})

export function formatMoney(value?: number | null, precise = false): string {
  if (value === undefined || value === null || Number.isNaN(value)) return '—'
  return precise ? moneyPrecise.format(value) : money.format(value)
}

export function formatCompact(value: number): string {
  return new Intl.NumberFormat('vi-VN', { notation: 'compact', maximumFractionDigits: 1 }).format(value)
}

export function formatDateTime(value?: string | null): string {
  if (!value) return '—'
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return '—'
  return date.toLocaleString('vi-VN', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit'
  })
}

export function formatDate(value?: string | null): string {
  if (!value) return '—'
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return '—'
  return date.toLocaleDateString('vi-VN', { day: '2-digit', month: '2-digit', year: 'numeric' })
}

export function timeAgo(value?: string | null): string {
  if (!value) return '—'
  const diff = Date.now() - new Date(value).getTime()
  if (diff < 0) return 'vừa xong'
  const seconds = Math.floor(diff / 1000)
  if (seconds < 45) return 'vừa xong'
  const minutes = Math.floor(seconds / 60)
  if (minutes < 60) return `${minutes} phút trước`
  const hours = Math.floor(minutes / 60)
  if (hours < 24) return `${hours} giờ trước`
  const days = Math.floor(hours / 24)
  if (days < 30) return `${days} ngày trước`
  return formatDate(value)
}

export function formatCountdown(msRemaining: number): {
  days: string
  hours: string
  minutes: string
  seconds: string
  compact: string
  ended: boolean
  urgent: boolean
} {
  const clamped = Math.max(0, msRemaining)
  const totalSeconds = Math.floor(clamped / 1000)
  const days = Math.floor(totalSeconds / 86400)
  const hours = Math.floor((totalSeconds % 86400) / 3600)
  const minutes = Math.floor((totalSeconds % 3600) / 60)
  const seconds = totalSeconds % 60
  const pad = (n: number) => n.toString().padStart(2, '0')
  const ended = clamped <= 0
  const urgent = !ended && clamped < 10 * 60 * 1000

  return {
    days: days.toString(),
    hours: pad(hours),
    minutes: pad(minutes),
    seconds: pad(seconds),
    compact:
      days > 0
        ? `${days} ngày ${pad(hours)}:${pad(minutes)}`
        : `${pad(hours)}:${pad(minutes)}:${pad(seconds)}`,
    ended,
    urgent
  }
}

export function statusLabel(status: string): string {
  switch (status) {
    case 'active':
      return 'Đang diễn ra'
    case 'scheduled':
      return 'Sắp diễn ra'
    case 'ended':
      return 'Đã kết thúc'
    case 'cancelled':
      return 'Đã hủy'
    default:
      return status
  }
}

export function statusClass(status: string): string {
  switch (status) {
    case 'active':
      return 'border-emerald-400/30 bg-emerald-400/10 text-emerald-300'
    case 'scheduled':
      return 'border-cyanx-400/30 bg-cyanx-400/10 text-cyanx-300'
    case 'ended':
      return 'border-white/15 bg-white/5 text-slate-300'
    default:
      return 'border-rose-400/30 bg-rose-400/10 text-rose-300'
  }
}

export function toLocalInputValue(date: Date): string {
  const pad = (n: number) => n.toString().padStart(2, '0')
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`
}

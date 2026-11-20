import { Fragment, useCallback, useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import {
  Activity,
  ChevronDown,
  ChevronUp,
  History,
  Loader2,
  RefreshCw,
  ShieldCheck
} from 'lucide-react'
import { adminApi, toApiError } from '../../lib/api'
import { formatDateTime } from '../../lib/format'
import type { AuditLog } from '../../lib/types'
import { useToast } from '../../context/ToastContext'

const eventFilters = [
  { value: 'all', label: 'Tất cả sự kiện' },
  { value: 'BidAccepted', label: 'BidAccepted · Nhận đặt giá' },
  { value: 'AuctionEnded', label: 'AuctionEnded · Kết thúc phiên' }
]

function prettyPayload(payload: string): string {
  try {
    return JSON.stringify(JSON.parse(payload), null, 2)
  } catch {
    return payload
  }
}

export default function AdminAudit() {
  const toast = useToast()
  const [items, setItems] = useState<AuditLog[]>([])
  const [total, setTotal] = useState(0)
  const [eventType, setEventType] = useState('all')
  const [page, setPage] = useState(1)
  const [loading, setLoading] = useState(true)
  const [expanded, setExpanded] = useState<string | null>(null)

  const load = useCallback(async () => {
    setLoading(true)
    try {
      const result = await adminApi.auditLogs({ eventType, page, pageSize: 20 })
      setItems(result.items)
      setTotal(result.total)
    } catch (error) {
      toast.error('Không thể tải nhật ký kiểm toán', toApiError(error).message)
    } finally {
      setLoading(false)
    }
  }, [eventType, page, toast])

  useEffect(() => {
    void load()
  }, [load])

  const totalPages = Math.max(1, Math.ceil(total / 20))

  return (
    <div className="space-y-5">
      <div className="flex flex-wrap items-end justify-between gap-4">
        <div>
          <p className="text-[11px] font-semibold uppercase tracking-[0.18em] text-cyanx-300/80">Bản ghi bất biến</p>
          <h2 className="heading mt-1 text-xl">Nhật ký kiểm toán</h2>
          <p className="muted mt-1 text-xs">
            Được ghi bởi Audit Worker độc lập từ sự kiện RabbitMQ — không phải tiến trình API.
          </p>
        </div>
        <div className="flex items-center gap-2">
          <span className="inline-flex items-center gap-1.5 rounded-full border border-emerald-400/25 bg-emerald-400/[0.07] px-2.5 py-1 text-[10px] font-semibold text-emerald-300">
            <ShieldCheck size={11} /> {total} sự kiện đã lưu
          </span>
          <button type="button" className="btn-ghost !px-3 !py-2 !text-[11px]" onClick={() => void load()} disabled={loading}>
            {loading ? <Loader2 size={13} className="animate-spin" /> : <RefreshCw size={13} />}
            Làm mới
          </button>
        </div>
      </div>

      <div className="flex flex-wrap gap-2">
        {eventFilters.map((filter) => (
          <button
            key={filter.value}
            type="button"
            onClick={() => {
              setEventType(filter.value)
              setPage(1)
              setExpanded(null)
            }}
            className={`chip ${eventType === filter.value ? 'chip-active' : ''}`}
          >
            {filter.label}
          </button>
        ))}
      </div>

      <div className="table-shell">
        <table className="w-full">
          <thead>
            <tr>
              <th className="th">Sự kiện</th>
              <th className="th">Phiên đấu giá</th>
              <th className="th">Tác nhân</th>
              <th className="th">Thời điểm phát sinh</th>
              <th className="th">Thời điểm xử lý</th>
              <th className="th text-right">Dữ liệu</th>
            </tr>
          </thead>
          <tbody>
            {items.map((log) => {
              const isOpen = expanded === log.id
              return (
                <Fragment key={log.id}>
                  <tr className="border-t border-white/[0.05] transition hover:bg-white/[0.02]">
                    <td className="td">
                      <span
                        className={`inline-flex items-center gap-1.5 rounded-full border px-2 py-0.5 text-[10px] font-semibold ${
                          log.eventType === 'BidAccepted'
                            ? 'border-gold-500/30 bg-gold-500/10 text-gold-200'
                            : 'border-cyanx-400/30 bg-cyanx-400/10 text-cyanx-300'
                        }`}
                      >
                        <Activity size={10} /> {log.eventType}
                      </span>
                      <p className="mt-1 font-mono text-[9px] text-slate-600">{log.eventId.slice(0, 18)}…</p>
                    </td>
                    <td className="td">
                      {log.auctionId ? (
                        <Link to={`/auctions/${log.auctionId}`} className="font-mono text-[11px] text-cyanx-300 hover:text-cyanx-200">
                          {log.auctionId.slice(0, 8)}
                        </Link>
                      ) : (
                        <span className="text-slate-600">—</span>
                      )}
                    </td>
                    <td className="td">
                      {log.userId ? (
                        <span className="font-mono text-[11px] text-slate-400">{log.userId.slice(0, 8)}</span>
                      ) : (
                        <span className="text-slate-600">hệ thống</span>
                      )}
                    </td>
                    <td className="td text-xs text-slate-400">{formatDateTime(log.occurredAtUtc)}</td>
                    <td className="td text-xs text-slate-500">{formatDateTime(log.processedAtUtc)}</td>
                    <td className="td text-right">
                      <button
                        type="button"
                        onClick={() => setExpanded(isOpen ? null : log.id)}
                        className="inline-flex items-center gap-1 rounded-lg border border-white/10 px-2.5 py-1.5 text-[10px] font-semibold text-slate-300 transition hover:border-gold-500/40 hover:text-gold-200"
                      >
                        {isOpen ? <ChevronUp size={11} /> : <ChevronDown size={11} />}
                        {isOpen ? 'Ẩn' : 'Xem'}
                      </button>
                    </td>
                  </tr>
                  {isOpen ? (
                    <tr key={`${log.id}-payload`} className="bg-ink-950/70">
                      <td colSpan={6} className="px-4 py-4">
                        <pre className="max-h-72 overflow-auto rounded-xl border border-white/[0.07] bg-ink-950 p-4 text-[11px] leading-relaxed text-slate-300">
                          {prettyPayload(log.payload)}
                        </pre>
                      </td>
                    </tr>
                  ) : null}
                </Fragment>
              )
            })}
          </tbody>
        </table>

        {loading ? (
          <div className="flex items-center justify-center gap-2 py-12 text-xs text-slate-500">
            <Loader2 size={14} className="animate-spin text-gold-300" /> Đang đọc sổ kiểm toán…
          </div>
        ) : items.length === 0 ? (
          <div className="px-6 py-14 text-center">
            <History size={20} className="mx-auto text-slate-600" />
            <p className="mt-3 text-sm font-semibold text-slate-300">Không có sự kiện kiểm toán trong chế độ xem này</p>
            <p className="mt-1 text-xs text-slate-500">
              Hãy đặt giá hoặc kết thúc một phiên để Audit Worker ghi lại tại đây.
            </p>
          </div>
        ) : null}

        {totalPages > 1 ? (
          <div className="flex items-center justify-between border-t border-white/[0.06] px-4 py-3 text-[11px] text-slate-500">
            <span>{total} sự kiện</span>
            <div className="flex items-center gap-2">
              <button
                type="button"
                className="btn-ghost !px-2.5 !py-1.5"
                disabled={page <= 1}
                onClick={() => setPage((value) => Math.max(1, value - 1))}
              >
                Trước
              </button>
              <span>
                {page} / {totalPages}
              </span>
              <button
                type="button"
                className="btn-ghost !px-2.5 !py-1.5"
                disabled={page >= totalPages}
                onClick={() => setPage((value) => Math.min(totalPages, value + 1))}
              >
                Sau
              </button>
            </div>
          </div>
        ) : null}
      </div>

      <p className="text-[10px] leading-relaxed text-slate-600">
        Consumer loại trùng theo <span className="font-mono text-slate-500">event_id</span>, nên RabbitMQ gửi lại không
        bao giờ tạo bản ghi kiểm toán trùng lặp.
      </p>
    </div>
  )
}

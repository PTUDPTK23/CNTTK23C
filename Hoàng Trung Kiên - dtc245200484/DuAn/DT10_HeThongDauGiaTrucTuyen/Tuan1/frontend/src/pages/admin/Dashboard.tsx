import { useCallback, useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import {
  Activity,
  AlertTriangle,
  ArrowUpRight,
  BarChart3,
  Gavel,
  History,
  Loader2,
  Package,
  RefreshCw,
  TrendingUp,
  Users
} from 'lucide-react'
import { adminApi, toApiError } from '../../lib/api'
import { formatCompact, formatMoney, statusClass, statusLabel, timeAgo } from '../../lib/format'
import type { Dashboard } from '../../lib/types'
import { useToast } from '../../context/ToastContext'

const statusTones: Record<string, string> = {
  active: 'bg-emerald-400',
  scheduled: 'bg-cyanx-400',
  ended: 'bg-slate-500',
  cancelled: 'bg-rose-400'
}

export default function DashboardPage() {
  const toast = useToast()
  const [data, setData] = useState<Dashboard | null>(null)
  const [loading, setLoading] = useState(true)
  const [refreshing, setRefreshing] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const load = useCallback(
    async (silent = false) => {
      silent ? setRefreshing(true) : setLoading(true)
      try {
        const dashboard = await adminApi.dashboard()
        setData(dashboard)
        setError(null)
      } catch (err) {
        setError(toApiError(err).message)
        toast.error('Không thể tải bảng điều khiển', toApiError(err).message)
      } finally {
        setLoading(false)
        setRefreshing(false)
      }
    },
    [toast]
  )

  useEffect(() => {
    void load()
  }, [load])

  if (loading) {
    return (
      <div className="space-y-4">
        <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
          {[0, 1, 2, 3].map((index) => (
            <div key={index} className="skeleton h-28 w-full" />
          ))}
        </div>
        <div className="skeleton h-64 w-full" />
      </div>
    )
  }

  if (error || !data) {
    return (
      <div className="panel p-8 text-center">
        <AlertTriangle size={22} className="mx-auto text-amber-300" />
        <p className="heading mt-3 text-lg">Không thể tải bảng điều khiển</p>
        <p className="muted mt-2">{error ?? 'Lỗi không xác định.'}</p>
        <button type="button" className="btn-ghost mt-5" onClick={() => void load()}>
          <RefreshCw size={14} /> Thử lại
        </button>
      </div>
    )
  }

  const maxBids = Math.max(1, ...data.bidsPerDay.map((point) => point.count))
  const totalStatus = Math.max(1, data.auctionsByStatus.reduce((sum, item) => sum + item.count, 0))

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <p className="text-[11px] font-semibold uppercase tracking-[0.18em] text-cyanx-300/80">Nhịp đập sàn đấu giá</p>
          <h2 className="heading mt-1 text-xl">Tổng quan vận hành trực tiếp</h2>
        </div>
        <div className="flex items-center gap-2">
          <span className="text-[10px] text-slate-600">Cập nhật {timeAgo(data.generatedAtUtc)}</span>
          <button
            type="button"
            onClick={() => void load(true)}
            disabled={refreshing}
            className="btn-ghost !px-3 !py-2 !text-[11px]"
          >
            {refreshing ? <Loader2 size={13} className="animate-spin" /> : <RefreshCw size={13} />}
            Làm mới
          </button>
        </div>
      </div>

      <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
        {[
          {
            label: 'Lượt đặt giá đã xác thực',
            value: formatCompact(data.totalBids),
            sub: `Tổng giá trị ${formatMoney(data.totalBidVolume)}`,
            icon: Activity,
            tone: 'text-gold-300 border-gold-500/25 bg-gold-500/10'
          },
          {
            label: 'Phiên trực tiếp',
            value: String(data.activeAuctions),
            sub: `${data.scheduledAuctions} sắp diễn ra · ${data.endedAuctions} đã kết thúc`,
            icon: Gavel,
            tone: 'text-emerald-300 border-emerald-400/25 bg-emerald-400/10'
          },
          {
            label: 'Người đấu giá',
            value: formatCompact(data.totalBidders),
            sub: `Tổng ${data.totalUsers} tài khoản`,
            icon: Users,
            tone: 'text-cyanx-300 border-cyanx-400/25 bg-cyanx-400/10'
          },
          {
            label: 'Danh mục',
            value: String(data.totalProducts),
            sub: `${data.totalAuctions} phiên đã tạo`,
            icon: Package,
            tone: 'text-slate-300 border-white/10 bg-white/[0.04]'
          }
        ].map((card) => (
          <div key={card.label} className="panel p-5">
            <div className="flex items-start justify-between gap-3">
              <div>
                <p className="text-[10px] font-semibold uppercase tracking-[0.16em] text-slate-500">{card.label}</p>
                <p className="mt-2 font-display text-3xl font-semibold tracking-tight text-white">{card.value}</p>
              </div>
              <span className={`grid h-9 w-9 place-items-center rounded-xl border ${card.tone}`}>
                <card.icon size={16} />
              </span>
            </div>
            <p className="mt-3 truncate text-[11px] text-slate-500">{card.sub}</p>
          </div>
        ))}
      </div>

      <div className="grid gap-5 xl:grid-cols-[1.5fr_1fr]">
        <div className="panel p-5 sm:p-6">
          <div className="flex items-center justify-between gap-3">
            <div>
              <p className="text-[10px] font-semibold uppercase tracking-[0.18em] text-slate-500">Nhu cầu</p>
              <h3 className="heading mt-1 text-base">Lượt đặt giá mỗi ngày · 14 ngày qua</h3>
            </div>
            <span className="inline-flex items-center gap-1 text-[10px] text-emerald-300">
              <TrendingUp size={12} /> {formatCompact(data.bidsPerDay.reduce((sum, point) => sum + point.count, 0))} lượt
            </span>
          </div>

          <div className="mt-6 flex h-44 items-end gap-1.5">
            {data.bidsPerDay.map((point) => (
              <div key={point.date} className="group flex h-full min-w-0 flex-1 flex-col items-center justify-end gap-1.5">
                <span className="text-[9px] font-semibold tabular-nums text-slate-500 opacity-0 transition group-hover:opacity-100">
                  {point.count}
                </span>
                <div
                  className="w-full rounded-t-md bg-gradient-to-t from-gold-600/70 to-gold-300/90 transition group-hover:from-gold-500 group-hover:to-gold-200"
                  style={{ height: `${Math.max(3, (point.count / maxBids) * 100)}%` }}
                  title={`${point.date}: ${point.count} lượt đặt giá`}
                />
                <span className="truncate text-[8px] text-slate-600">
                  {new Date(`${point.date}T00:00:00`).toLocaleDateString('vi-VN', { day: 'numeric' })}
                </span>
              </div>
            ))}
          </div>
        </div>

        <div className="panel p-5 sm:p-6">
          <p className="text-[10px] font-semibold uppercase tracking-[0.18em] text-slate-500">Trạng thái danh mục</p>
          <h3 className="heading mt-1 text-base">Phiên đấu giá theo trạng thái</h3>

          <div className="mt-5 flex h-3 overflow-hidden rounded-full bg-white/[0.05]">
            {data.auctionsByStatus.map((item) => (
              <div
                key={item.status}
                className={statusTones[item.status] ?? 'bg-slate-500'}
                style={{ width: `${(item.count / totalStatus) * 100}%` }}
                title={`${statusLabel(item.status)}: ${item.count}`}
              />
            ))}
          </div>

          <ul className="mt-5 space-y-3">
            {data.auctionsByStatus.map((item) => (
              <li key={item.status} className="flex items-center justify-between gap-3">
                <span className="flex items-center gap-2 text-xs text-slate-400">
                  <span className={`h-2 w-2 rounded-full ${statusTones[item.status] ?? 'bg-slate-500'}`} />
                  {statusLabel(item.status)}
                </span>
                <span className="text-xs font-semibold tabular-nums text-slate-200">{item.count}</span>
              </li>
            ))}
          </ul>

          <div className="mt-5 rounded-xl border border-white/[0.07] bg-white/[0.02] p-3.5">
            <p className="text-[10px] uppercase tracking-[0.16em] text-slate-500">Trung bình lượt đặt giá / phiên</p>
            <p className="mt-1 font-display text-xl font-semibold text-white">
              {(data.totalAuctions ? data.totalBids / data.totalAuctions : 0).toFixed(1)}
            </p>
          </div>
        </div>
      </div>

      <div className="grid gap-5 xl:grid-cols-[1.2fr_1fr]">
        <div className="table-shell">
          <div className="flex items-center justify-between gap-3 border-b border-white/[0.06] px-4 py-3.5">
            <h3 className="flex items-center gap-2 text-xs font-semibold uppercase tracking-[0.14em] text-slate-400">
              <BarChart3 size={13} className="text-gold-300" /> Lô giá trị cao nhất
            </h3>
            <Link to="/admin/auctions" className="inline-flex items-center gap-1 text-[10px] font-semibold text-gold-300 hover:text-gold-200">
              Quản lý <ArrowUpRight size={11} />
            </Link>
          </div>
          <table className="w-full">
            <thead>
              <tr>
                <th className="th">Phiên đấu giá</th>
                <th className="th">Giá</th>
                <th className="th">Lượt đặt</th>
                <th className="th">Trạng thái</th>
              </tr>
            </thead>
            <tbody>
              {data.topAuctions.map((auction) => (
                <tr key={auction.id} className="border-t border-white/[0.05]">
                  <td className="td max-w-[240px]">
                    <Link to={`/auctions/${auction.id}`} className="block truncate font-medium text-slate-200 hover:text-gold-300">
                      {auction.title}
                    </Link>
                    {auction.winnerName ? (
                      <span className="text-[10px] text-slate-600">Người thắng · {auction.winnerName}</span>
                    ) : null}
                  </td>
                  <td className="td font-display font-semibold tabular-nums text-gold-300">{formatMoney(auction.currentPrice)}</td>
                  <td className="td text-slate-400">{auction.bidCount}</td>
                  <td className="td">
                    <span className={`rounded-full border px-2 py-0.5 text-[9px] font-semibold uppercase tracking-wide ${statusClass(auction.status)}`}>
                      {statusLabel(auction.status)}
                    </span>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
          {data.topAuctions.length === 0 ? <p className="px-4 py-8 text-center text-xs text-slate-600">Chưa có phiên đấu giá.</p> : null}
        </div>

        <div className="table-shell">
          <div className="flex items-center justify-between gap-3 border-b border-white/[0.06] px-4 py-3.5">
            <h3 className="flex items-center gap-2 text-xs font-semibold uppercase tracking-[0.14em] text-slate-400">
              <History size={13} className="text-cyanx-300" /> Lượt đặt giá mới nhất
            </h3>
            <Link to="/admin/audit" className="inline-flex items-center gap-1 text-[10px] font-semibold text-cyanx-300 hover:text-cyanx-200">
              Nhật ký kiểm toán <ArrowUpRight size={11} />
            </Link>
          </div>
          <ul className="divide-y divide-white/[0.05]">
            {data.recentBids.map((bid) => (
              <li key={bid.id} className="flex items-center justify-between gap-3 px-4 py-3">
                <div className="min-w-0">
                  <p className="truncate text-xs font-semibold text-slate-200">{bid.bidderName}</p>
                  <p className="truncate text-[10px] text-slate-500">
                    {bid.auctionTitle ?? 'Phiên đấu giá'} · {timeAgo(bid.placedAtUtc)}
                  </p>
                </div>
                <span className="shrink-0 font-display text-sm font-semibold tabular-nums text-gold-300">
                  {formatMoney(bid.amount)}
                </span>
              </li>
            ))}
          </ul>
          {data.recentBids.length === 0 ? <p className="px-4 py-8 text-center text-xs text-slate-600">Chưa có lượt đặt giá.</p> : null}
        </div>
      </div>
    </div>
  )
}

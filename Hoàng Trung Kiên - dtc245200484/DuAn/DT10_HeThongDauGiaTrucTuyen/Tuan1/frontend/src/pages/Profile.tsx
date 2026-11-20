import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { motion } from 'framer-motion'
import {
  Award,
  ChevronLeft,
  ChevronRight,
  Gavel,
  Loader2,
  Medal,
  Sparkles,
  Target,
  Trophy,
  UserRound
} from 'lucide-react'
import { authApi, usersApi, toApiError } from '../lib/api'
import { formatDateTime, formatMoney, statusClass, statusLabel, timeAgo } from '../lib/format'
import type { AuctionSummary, Bid, Paged, UserProfile } from '../lib/types'
import { useAuth } from '../context/AuthContext'
import { useToast } from '../context/ToastContext'

type Tab = 'bids' | 'rooms' | 'wins'

const tabs: { id: Tab; label: string; icon: typeof Gavel }[] = [
  { id: 'bids', label: 'Lượt đặt giá của tôi', icon: Gavel },
  { id: 'rooms', label: 'Phiên đã tham gia', icon: Target },
  { id: 'wins', label: 'Phiên đã thắng', icon: Trophy }
]

function initials(name: string): string {
  return name
    .split(' ')
    .filter(Boolean)
    .map((part) => part[0])
    .slice(0, 2)
    .join('')
    .toUpperCase()
}

export default function Profile() {
  const { user, refresh } = useAuth()
  const toast = useToast()

  const [profile, setProfile] = useState<UserProfile | null>(user)
  const [tab, setTab] = useState<Tab>('bids')
  const [bids, setBids] = useState<Paged<Bid> | null>(null)
  const [rooms, setRooms] = useState<AuctionSummary[]>([])
  const [wins, setWins] = useState<AuctionSummary[]>([])
  const [page, setPage] = useState(1)
  const [loading, setLoading] = useState(true)
  const [loadingBids, setLoadingBids] = useState(false)

  useEffect(() => {
    let cancelled = false
    ;(async () => {
      try {
        const [me, myRooms, myWins] = await Promise.all([
          authApi.me(),
          usersApi.myAuctions(),
          usersApi.myWins()
        ])
        if (cancelled) return
        setProfile(me)
        setRooms(myRooms)
        setWins(myWins)
        void refresh()
      } catch (error) {
        if (!cancelled) toast.error('Không thể tải hồ sơ của bạn', toApiError(error).message)
      } finally {
        if (!cancelled) setLoading(false)
      }
    })()
    return () => {
      cancelled = true
    }
  }, [refresh, toast])

  useEffect(() => {
    let cancelled = false
    setLoadingBids(true)
    ;(async () => {
      try {
        const result = await usersApi.myBids(page, 15)
        if (!cancelled) setBids(result)
      } catch (error) {
        if (!cancelled) toast.error('Không thể tải sổ đặt giá của bạn', toApiError(error).message)
      } finally {
        if (!cancelled) setLoadingBids(false)
      }
    })()
    return () => {
      cancelled = true
    }
  }, [page, toast])

  if (loading && !profile) {
    return (
      <div className="container-lux py-14">
        <div className="skeleton h-40 w-full rounded-3xl" />
        <div className="mt-6 grid gap-4 sm:grid-cols-3">
          {[0, 1, 2].map((index) => (
            <div key={index} className="skeleton h-24 w-full" />
          ))}
        </div>
      </div>
    )
  }

  if (!profile) {
    return (
      <div className="container-lux grid min-h-[50vh] place-items-center py-16">
        <div className="panel max-w-md p-8 text-center">
          <UserRound size={24} className="mx-auto text-slate-500" />
          <h1 className="heading mt-4 text-xl">Không thể tải hồ sơ của bạn</h1>
          <p className="muted mt-2">Hãy đăng nhập lại để mở sổ đặt giá cá nhân.</p>
          <Link to="/login" className="btn-gold mt-5">
            Đăng nhập
          </Link>
        </div>
      </div>
    )
  }

  const totalPages = Math.max(1, bids?.totalPages ?? 1)

  return (
    <div className="container-lux py-10 sm:py-14">
      <motion.div
        initial={{ opacity: 0, y: 12 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ duration: 0.4 }}
        className="panel relative overflow-hidden p-6 sm:p-8"
      >
        <div className="pointer-events-none absolute -right-16 -top-20 h-56 w-56 rounded-full bg-gold-500/[0.08] blur-3xl" />
        <div className="relative flex flex-col gap-6 sm:flex-row sm:items-center sm:justify-between">
          <div className="flex items-center gap-4">
            <span className="grid h-16 w-16 shrink-0 place-items-center rounded-2xl bg-gradient-to-br from-gold-200 via-gold-400 to-gold-600 font-display text-xl font-bold text-ink-950 shadow-luxury">
              {initials(profile.displayName)}
            </span>
            <div>
              <div className="flex flex-wrap items-center gap-2">
                <h1 className="heading text-2xl">{profile.displayName}</h1>
                <span className="rounded-full border border-gold-500/30 bg-gold-500/10 px-2.5 py-0.5 text-[10px] font-semibold uppercase tracking-[0.14em] text-gold-200">
                  {profile.role === 'admin' ? 'Quản trị viên' : 'Người đấu giá'}
                </span>
              </div>
              <p className="mt-1 text-xs text-slate-400">{profile.email}</p>
              <p className="mt-1 text-[11px] text-slate-600">Thành viên từ {formatDateTime(profile.createdAtUtc)}</p>
            </div>
          </div>

          <div className="flex flex-wrap items-center gap-3">
            <div className="rounded-xl border border-white/[0.08] bg-white/[0.03] px-4 py-2.5 text-center">
              <p className="font-display text-lg font-semibold text-white">{profile.totalBids}</p>
              <p className="text-[9px] font-semibold uppercase tracking-[0.16em] text-slate-500">Lượt đã đặt</p>
            </div>
            <div className="rounded-xl border border-white/[0.08] bg-white/[0.03] px-4 py-2.5 text-center">
              <p className="font-display text-lg font-semibold text-white">{profile.auctionsJoined}</p>
              <p className="text-[9px] font-semibold uppercase tracking-[0.16em] text-slate-500">Phiên tham gia</p>
            </div>
            <div className="rounded-xl border border-gold-500/25 bg-gold-500/[0.07] px-4 py-2.5 text-center">
              <p className="font-display text-lg font-semibold text-gold-200">{profile.auctionsWon}</p>
              <p className="text-[9px] font-semibold uppercase tracking-[0.16em] text-gold-300/70">Đã thắng</p>
            </div>
          </div>
        </div>
      </motion.div>

      <div className="mt-7 flex flex-wrap items-center justify-between gap-3">
        <div className="inline-flex rounded-xl border border-white/10 bg-ink-900/60 p-1">
          {tabs.map((item) => (
            <button
              key={item.id}
              type="button"
              onClick={() => setTab(item.id)}
              className={`inline-flex items-center gap-1.5 rounded-lg px-3.5 py-2 text-xs font-semibold transition ${
                tab === item.id ? 'bg-gold-500/15 text-gold-200' : 'text-slate-400 hover:text-white'
              }`}
            >
              <item.icon size={13} />
              {item.label}
            </button>
          ))}
        </div>
        <Link to="/auctions?status=active" className="btn-ghost !py-2 !text-[11px]">
          <Sparkles size={12} /> Tìm phiên trực tiếp
        </Link>
      </div>

      <div className="mt-5">
        {tab === 'bids' ? (
          <div className="table-shell">
            <table className="w-full">
              <thead>
                <tr>
                  <th className="th">Phiên đấu giá</th>
                  <th className="th">Số tiền</th>
                  <th className="th">Thứ tự</th>
                  <th className="th">Thời điểm</th>
                  <th className="th text-right">Trạng thái</th>
                </tr>
              </thead>
              <tbody>
                {(bids?.items ?? []).map((bid) => (
                  <tr key={bid.id} className="border-t border-white/[0.05] transition hover:bg-white/[0.02]">
                    <td className="td max-w-[260px]">
                      <Link to={`/auctions/${bid.auctionId}`} className="block truncate font-medium text-slate-200 hover:text-gold-300">
                        {bid.auctionTitle ?? 'Phiên đấu giá'}
                      </Link>
                    </td>
                    <td className="td font-display font-semibold tabular-nums text-gold-300">{formatMoney(bid.amount)}</td>
                    <td className="td text-slate-500">#{bid.sequence}</td>
                    <td className="td text-xs text-slate-500">{timeAgo(bid.placedAtUtc)}</td>
                    <td className="td text-right">
                      {bid.isWinning ? (
                        <span className="inline-flex items-center gap-1 rounded-full border border-gold-500/30 bg-gold-500/10 px-2 py-0.5 text-[10px] font-semibold text-gold-200">
                          <Trophy size={10} /> Đang thắng
                        </span>
                      ) : (
                        <span className="text-[11px] text-slate-600">Đã xác thực</span>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>

            {loadingBids ? (
              <div className="flex items-center justify-center gap-2 py-10 text-xs text-slate-500">
                <Loader2 size={14} className="animate-spin text-gold-300" /> Đang tải sổ đặt giá…
              </div>
            ) : (bids?.items.length ?? 0) === 0 ? (
              <div className="px-6 py-14 text-center">
                <Gavel size={22} className="mx-auto text-slate-600" />
                <p className="mt-3 text-sm font-semibold text-slate-300">Chưa có lượt đặt giá</p>
                <p className="mt-1 text-xs text-slate-500">Mở một phiên trực tiếp và đặt lượt giá đã xác thực đầu tiên.</p>
                <Link to="/auctions?status=active" className="btn-gold mt-5 !py-2.5 !text-xs">
                  Xem các phiên trực tiếp
                </Link>
              </div>
            ) : null}

            {bids && bids.total > 0 ? (
              <div className="flex items-center justify-between border-t border-white/[0.06] px-4 py-3 text-[11px] text-slate-500">
                <span>{bids.total} lượt đặt giá đã ghi nhận</span>
                <div className="flex items-center gap-2">
                  <button
                    type="button"
                    className="btn-ghost !px-2.5 !py-1.5"
                    disabled={page <= 1}
                    onClick={() => setPage((value) => Math.max(1, value - 1))}
                    aria-label="Trang trước"
                  >
                    <ChevronLeft size={13} />
                  </button>
                  <span>
                    Trang {page} / {totalPages}
                  </span>
                  <button
                    type="button"
                    className="btn-ghost !px-2.5 !py-1.5"
                    disabled={page >= totalPages}
                    onClick={() => setPage((value) => Math.min(totalPages, value + 1))}
                    aria-label="Trang sau"
                  >
                    <ChevronRight size={13} />
                  </button>
                </div>
              </div>
            ) : null}
          </div>
        ) : null}

        {tab === 'rooms' ? (
          <div className="space-y-3">
            {rooms.length === 0 ? (
              <div className="panel px-6 py-14 text-center">
                <Target size={22} className="mx-auto text-slate-600" />
                <p className="mt-3 text-sm font-semibold text-slate-300">Bạn chưa tham gia phiên nào</p>
                <p className="mt-1 text-xs text-slate-500">Lượt đặt giá đầu tiên sẽ đăng ký bạn vào một phiên đấu giá.</p>
              </div>
            ) : (
              rooms.map((auction) => (
                <Link
                  key={auction.id}
                  to={`/auctions/${auction.id}`}
                  className="panel flex flex-col gap-3 p-4 transition hover:border-gold-500/30 sm:flex-row sm:items-center sm:justify-between sm:p-5"
                >
                  <div className="min-w-0">
                    <div className="flex items-center gap-2">
                      <span className={`rounded-full border px-2 py-0.5 text-[9px] font-semibold uppercase tracking-wide ${statusClass(auction.status)}`}>
                        {statusLabel(auction.status)}
                      </span>
                      <span className="text-[10px] uppercase tracking-[0.14em] text-slate-500">{auction.category}</span>
                    </div>
                    <p className="mt-1.5 truncate text-sm font-semibold text-slate-200">{auction.title}</p>
                    <p className="mt-0.5 text-[11px] text-slate-500">
                      {auction.bidCount} lượt đặt giá · kết thúc {formatDateTime(auction.endTimeUtc)}
                    </p>
                  </div>
                  <div className="flex items-center justify-between gap-6 sm:justify-end">
                    <div className="text-right">
                      <p className="text-[9px] uppercase tracking-[0.16em] text-slate-500">
                        {auction.status === 'ended' ? 'Giá chốt' : 'Giá hiện tại'}
                      </p>
                      <p className="font-display text-base font-semibold text-gold-300">{formatMoney(auction.currentPrice)}</p>
                    </div>
                    <ChevronRight size={16} className="text-slate-600" />
                  </div>
                </Link>
              ))
            )}
          </div>
        ) : null}

        {tab === 'wins' ? (
          <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
            {wins.length === 0 ? (
              <div className="panel px-6 py-14 text-center sm:col-span-2 lg:col-span-3">
                <Medal size={22} className="mx-auto text-slate-600" />
                <p className="mt-3 text-sm font-semibold text-slate-300">Chưa có phiên thắng nào</p>
                <p className="mt-1 text-xs text-slate-500">
                  Lượt giá thắng, giá chốt và bản ghi kiểm toán sẽ xuất hiện ở đây sau khi phiên kết thúc.
                </p>
              </div>
            ) : (
              wins.map((auction) => (
                <Link
                  key={auction.id}
                  to={`/auctions/${auction.id}`}
                  className="panel group relative overflow-hidden p-5 transition hover:border-gold-500/30"
                >
                  <div className="pointer-events-none absolute -right-8 -top-8 h-24 w-24 rounded-full bg-gold-500/10 blur-2xl" />
                  <span className="grid h-10 w-10 place-items-center rounded-xl border border-gold-500/30 bg-gold-500/10 text-gold-300">
                    <Award size={18} />
                  </span>
                  <p className="mt-4 text-[10px] uppercase tracking-[0.16em] text-slate-500">{auction.category}</p>
                  <p className="mt-1 line-clamp-2 font-display text-sm font-semibold text-slate-100 group-hover:text-gold-200">
                    {auction.title}
                  </p>
                  <div className="mt-4 flex items-end justify-between border-t border-white/[0.06] pt-3">
                    <div>
                      <p className="text-[9px] uppercase tracking-[0.16em] text-slate-500">Giá thắng</p>
                      <p className="font-display text-lg font-semibold text-gold-300">{formatMoney(auction.currentPrice)}</p>
                    </div>
                    <Trophy size={16} className="text-gold-400/70" />
                  </div>
                </Link>
              ))
            )}
          </div>
        ) : null}
      </div>
    </div>
  )
}

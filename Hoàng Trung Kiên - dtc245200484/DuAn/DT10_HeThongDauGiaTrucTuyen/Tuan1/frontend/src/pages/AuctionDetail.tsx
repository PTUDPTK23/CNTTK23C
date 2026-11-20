import { Suspense, lazy, useCallback, useEffect, useMemo, useRef, useState } from 'react'
import { Link, useLocation, useNavigate, useParams } from 'react-router-dom'
import { AnimatePresence, motion } from 'framer-motion'
import {
  Activity,
  AlertTriangle,
  ArrowLeft,
  Box,
  Clock,
  Gavel,
  History,
  Image as ImageIcon,
  Loader2,
  Lock,
  Radio,
  ShieldCheck,
  Sparkles,
  Trophy,
  Users,
  Zap
} from 'lucide-react'
import { Countdown } from '../components/Countdown'
import { PriceChart } from '../components/PriceChart'
import { SceneShell, ImagePanel } from '../three/SceneShell'
import { useAuctionHub, useServerOffset } from '../hooks/useAuctionHub'
import { auctionsApi, mediaUrl, toApiError } from '../lib/api'
import { formatDateTime, formatMoney, statusClass, statusLabel, timeAgo } from '../lib/format'
import type {
  AuctionDetail as AuctionDetailModel,
  AuctionResult,
  Bid,
  BidAcceptedMessage,
  Participant
} from '../lib/types'
import { useAuth } from '../context/AuthContext'
import { useToast } from '../context/ToastContext'

const ProductScene = lazy(() => import('../three/ProductScene'))

function Dot({ tone = 'emerald' }: { tone?: 'emerald' | 'amber' | 'slate' | 'rose' }) {
  const tones = {
    emerald: 'bg-emerald-400 shadow-[0_0_10px_2px_rgba(52,211,153,0.45)]',
    amber: 'bg-amber-400 shadow-[0_0_10px_2px_rgba(251,191,36,0.4)]',
    rose: 'bg-rose-400 shadow-[0_0_10px_2px_rgba(251,113,133,0.4)]',
    slate: 'bg-slate-500'
  }
  return <span className={`inline-block h-1.5 w-1.5 rounded-full ${tones[tone]}`} />
}

function toBid(message: BidAcceptedMessage): Bid {
  return {
    id: message.bidId,
    auctionId: message.auctionId,
    auctionTitle: message.auctionTitle,
    amount: message.amount,
    bidderId: message.bidderId,
    bidderName: message.bidderName,
    placedAtUtc: message.placedAtUtc,
    sequence: message.sequence,
    isWinning: false
  }
}

export default function AuctionDetail() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const location = useLocation()
  const toast = useToast()
  const { user, isAuthenticated, isAdmin } = useAuth()

  const [auction, setAuction] = useState<AuctionDetailModel | null>(null)
  const [bids, setBids] = useState<Bid[]>([])
  const [participants, setParticipants] = useState<Participant[]>([])
  const [result, setResult] = useState<AuctionResult | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [view, setView] = useState<'scene' | 'photo'>('scene')
  const [amount, setAmount] = useState('')
  const [placing, setPlacing] = useState(false)
  const [flashBid, setFlashBid] = useState<string | null>(null)

  const knownParticipants = useRef<Set<string>>(new Set())
  const endRefresh = useRef<number | undefined>(undefined)

  const load = useCallback(
    async (options?: { silent?: boolean }) => {
      if (!id) return
      if (!options?.silent) setLoading(true)
      try {
        const [detail, bidPage, people, auctionResult] = await Promise.all([
          auctionsApi.get(id),
          auctionsApi.bids(id, 1, 50),
          auctionsApi.participants(id),
          auctionsApi.result(id)
        ])
        setAuction(detail)
        setBids(bidPage.items)
        setParticipants(people)
        setResult(auctionResult)
        knownParticipants.current = new Set(people.map((person) => person.userId))
        setAmount((current) => current || detail.minimumNextBid.toFixed(2))
        setError(null)
      } catch (err) {
        setError(toApiError(err).message)
      } finally {
        setLoading(false)
      }
    },
    [id]
  )

  useEffect(() => {
    void load()
    return () => {
      if (endRefresh.current) window.clearTimeout(endRefresh.current)
    }
  }, [load])

  const serverOffset = useServerOffset(auction?.serverTimeUtc)

  const applyBid = useCallback(
    (message: BidAcceptedMessage) => {
      setAuction((current) => {
        if (!current) return current
        const isNewParticipant = !knownParticipants.current.has(message.bidderId)
        if (isNewParticipant) knownParticipants.current.add(message.bidderId)
        return {
          ...current,
          currentPrice: message.currentPrice,
          minimumNextBid: message.minimumNextBid,
          bidCount: Math.max(current.bidCount + 1, message.sequence),
          highestBidderName: message.bidderName,
          participantCount: current.participantCount + (isNewParticipant ? 1 : 0)
        }
      })

      setBids((current) => {
        if (current.some((bid) => bid.id === message.bidId)) return current
        return [toBid(message), ...current].slice(0, 50)
      })

      setParticipants((current) => {
        const existing = current.find((person) => person.userId === message.bidderId)
        if (existing) {
          return current
            .map((person) =>
              person.userId === message.bidderId
                ? { ...person, bidCount: person.bidCount + 1, lastBidAtUtc: message.placedAtUtc }
                : person
            )
            .sort((a, b) => b.bidCount - a.bidCount)
        }
        return [
          {
            userId: message.bidderId,
            displayName: message.bidderName,
            joinedAtUtc: message.placedAtUtc,
            lastBidAtUtc: message.placedAtUtc,
            bidCount: 1
          },
          ...current
        ]
      })

      if (message.bidderId !== user?.id) {
        setFlashBid(message.bidId)
        window.setTimeout(() => setFlashBid((value) => (value === message.bidId ? null : value)), 1600)
      }
    },
    [user?.id]
  )

  const hubStatus = useAuctionHub(id, {
    onBidAccepted: applyBid,
    onAuctionEnded: (message) => {
      setAuction((current) =>
        current
          ? {
              ...current,
              status: 'ended',
              endedAtUtc: message.endedAtUtc,
              highestBidderName: message.winnerName ?? current.highestBidderName,
              winner: message.winnerId
                ? {
                    userId: message.winnerId,
                    displayName: message.winnerName ?? 'Người thắng',
                    winningAmount: message.winningAmount ?? current.currentPrice,
                    winningBidId: message.winningBidId
                  }
                : null
            }
          : current
      )
      setBids((current) =>
        current.map((bid) => ({ ...bid, isWinning: Boolean(message.winningBidId && bid.id === message.winningBidId) }))
      )
      setResult({
        auctionId: message.auctionId,
        title: message.auctionTitle,
        status: 'ended',
        hasWinner: Boolean(message.winnerId),
        winnerId: message.winnerId,
        winnerName: message.winnerName,
        winningAmount: message.winningAmount,
        winningBidId: message.winningBidId,
        totalBids: message.totalBids,
        endedAtUtc: message.endedAtUtc
      })
      toast.info(
        'Búa đã điểm',
        message.winnerName
          ? `${message.winnerName} thắng với giá ${formatMoney(message.winningAmount)}.`
          : 'Phiên đấu giá kết thúc mà không có lượt đặt giá hợp lệ.'
      )
    }
  })

  const scheduleRefresh = useCallback(() => {
    if (endRefresh.current) window.clearTimeout(endRefresh.current)
    endRefresh.current = window.setTimeout(() => {
      void load({ silent: true })
    }, 6500)
  }, [load])

  const minimumNext = auction?.minimumNextBid ?? 0
  const increment = auction?.minIncrement ?? 0
  const isActive = auction?.status === 'active'
  const numericAmount = Number(amount)
  const amountValid =
    Number.isFinite(numericAmount) && numericAmount >= minimumNext && Math.abs(numericAmount * 100 - Math.round(numericAmount * 100)) < 1e-6

  const quickAmounts = useMemo(() => {
    if (!auction) return []
    const base = auction.minimumNextBid
    const step = auction.minIncrement
    return [
      { label: 'Giá kế tiếp', value: base },
      { label: `+${formatMoney(step)}`, value: base + step },
      { label: `+${formatMoney(step * 2)}`, value: base + step * 2 },
      { label: `+${formatMoney(step * 5)}`, value: base + step * 5 }
    ]
  }, [auction])

  const submitBid = async (event: React.FormEvent) => {
    event.preventDefault()
    if (!id || !auction) return

    if (!isAuthenticated) {
      navigate('/login', { state: { from: `/auctions/${id}` } })
      return
    }

    if (!amountValid) {
      toast.error('Kiểm tra giá đặt', `Giá hợp lệ tối thiểu là ${formatMoney(minimumNext)}.`)
      return
    }

    setPlacing(true)
    try {
      const response = await auctionsApi.placeBid(id, numericAmount)
      applyBid({
        auctionId: response.auctionId,
        bidId: response.bidId,
        bidderId: user?.id ?? '',
        bidderName: response.bidderName,
        amount: response.amount,
        previousPrice: auction.currentPrice,
        currentPrice: response.currentPrice,
        minimumNextBid: response.minimumNextBid,
        placedAtUtc: response.placedAtUtc,
        sequence: auction.bidCount + 1,
        auctionTitle: auction.title
      })
      setAmount(response.minimumNextBid.toFixed(2))
      toast.success('Đã nhận lượt đặt giá', `Bạn đang dẫn đầu với ${formatMoney(response.amount)}.`)
    } catch (err) {
      toast.error('Lượt đặt giá bị từ chối', toApiError(err).message)
    } finally {
      setPlacing(false)
    }
  }

  if (loading) {
    return (
      <div className="container-lux py-14">
        <div className="skeleton h-4 w-40" />
        <div className="mt-6 grid gap-8 lg:grid-cols-[1.35fr_0.65fr]">
          <div className="skeleton aspect-[4/3] w-full rounded-3xl" />
          <div className="space-y-4">
            <div className="skeleton h-10 w-2/3" />
            <div className="skeleton h-24 w-full" />
            <div className="skeleton h-40 w-full" />
          </div>
        </div>
      </div>
    )
  }

  if (error || !auction) {
    return (
      <div className="container-lux grid min-h-[55vh] place-items-center py-16">
        <div className="panel max-w-lg p-9 text-center">
          <span className="mx-auto grid h-12 w-12 place-items-center rounded-full border border-amber-400/30 bg-amber-400/10 text-amber-300">
            <AlertTriangle size={22} />
          </span>
          <h1 className="heading mt-4 text-xl">Không thể mở lô đấu giá này</h1>
          <p className="muted mt-2">{error ?? 'Phiên đấu giá có thể đã bị gỡ khỏi danh mục.'}</p>
          <div className="mt-6 flex justify-center gap-3">
            <button type="button" className="btn-ghost" onClick={() => void load()}>
              Thử lại
            </button>
            <Link to="/auctions" className="btn-gold">
              Về danh mục
            </Link>
          </div>
        </div>
      </div>
    )
  }

  const highest = bids.reduce<Bid | null>((best, bid) => (!best || bid.amount > best.amount ? bid : best), null)
  const ended = auction.status === 'ended'
  const closedWithoutWinner = ended && !result?.hasWinner
  const isParticipant = participants.some((person) => person.userId === user?.id)

  return (
    <div className="container-lux py-8 sm:py-12">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <Link
          to="/auctions"
          className="inline-flex items-center gap-2 text-xs font-semibold text-slate-400 transition hover:text-white"
        >
          <ArrowLeft size={14} /> Về danh mục đấu giá
        </Link>
        <div className="flex items-center gap-2 text-[11px] text-slate-500">
          <span className="inline-flex items-center gap-1.5">
            {hubStatus === 'connected' ? (
              <>
                <Dot /> Đã kết nối kênh trực tiếp
              </>
            ) : hubStatus === 'reconnecting' ? (
              <>
                <Dot tone="amber" /> Đang kết nối lại…
              </>
            ) : (
              <>
                <Dot tone="rose" /> Kênh trực tiếp ngoại tuyến
              </>
            )}
          </span>
          <span className="text-slate-700">·</span>
          <span>Lô {auction.id.slice(0, 8).toUpperCase()}</span>
        </div>
      </div>

      <div className="mt-5 grid gap-6 lg:grid-cols-[1.35fr_0.65fr]">
        <div className="space-y-6">
          <div className="overflow-hidden rounded-3xl border border-white/[0.08] bg-ink-900/60 shadow-card">
            <div className="flex flex-wrap items-center justify-between gap-3 border-b border-white/[0.06] px-4 py-3">
              <div className="flex items-center gap-2">
                <span className={`rounded-full border px-2.5 py-1 text-[10px] font-semibold uppercase tracking-[0.14em] ${statusClass(auction.status)}`}>
                  {auction.status === 'active' ? <Dot /> : null}
                  {statusLabel(auction.status)}
                </span>
                <span className="rounded-full border border-white/10 bg-white/[0.03] px-2.5 py-1 text-[10px] font-medium uppercase tracking-[0.14em] text-slate-400">
                  {auction.category}
                </span>
              </div>

              <div className="flex items-center gap-1 rounded-xl border border-white/10 bg-ink-950/60 p-1">
                <button
                  type="button"
                  onClick={() => setView('scene')}
                  className={`inline-flex items-center gap-1.5 rounded-lg px-3 py-1.5 text-[11px] font-semibold transition ${
                    view === 'scene' ? 'bg-gold-500/15 text-gold-200' : 'text-slate-400 hover:text-white'
                  }`}
                >
                  <Box size={12} /> Sân khấu 3D
                </button>
                <button
                  type="button"
                  onClick={() => setView('photo')}
                  className={`inline-flex items-center gap-1.5 rounded-lg px-3 py-1.5 text-[11px] font-semibold transition ${
                    view === 'photo' ? 'bg-gold-500/15 text-gold-200' : 'text-slate-400 hover:text-white'
                  }`}
                >
                  <ImageIcon size={12} /> Ảnh chụp
                </button>
              </div>
            </div>

            <div className="relative aspect-[4/3] w-full">
              {view === 'scene' ? (
                <SceneShell imageUrl={auction.imageUrl} alt={auction.productName} cameraPosition={[0, 2.2, 6.2]}>
                  <Suspense fallback={null}>
                    <ProductScene category={auction.category} />
                  </Suspense>
                </SceneShell>
              ) : (
                <ImagePanel imageUrl={auction.imageUrl} alt={auction.productName} />
              )}

              <div className="pointer-events-none absolute inset-x-4 bottom-4 flex items-center justify-between gap-3 rounded-xl border border-white/10 bg-ink-950/65 px-4 py-3 backdrop-blur-md">
                <div>
                  <p className="text-[9px] font-semibold uppercase tracking-[0.2em] text-slate-500">Vật phẩm ký gửi</p>
                  <p className="font-display text-xs font-semibold text-gold-200">{auction.productName}</p>
                </div>
                <span className="text-[10px] text-slate-400">
                  {view === 'scene' ? 'Kéo để xoay · cuộn để thu phóng' : mediaUrl(auction.imageUrl) ? 'Ảnh trong danh mục' : 'Không có hình ảnh'}
                </span>
              </div>
            </div>
          </div>

          <div className="panel p-6 sm:p-7">
            <p className="eyebrow">Lô đấu giá</p>
            <h1 className="heading mt-2 text-2xl leading-tight sm:text-3xl">{auction.title}</h1>
            <p className="mt-3 text-sm leading-relaxed text-slate-400">{auction.productDescription}</p>

            <dl className="mt-6 grid gap-4 border-t border-white/[0.06] pt-5 sm:grid-cols-2 lg:grid-cols-4">
              {[
                { label: 'Bộ sưu tập', value: auction.category },
                { label: 'Giá khởi điểm', value: formatMoney(auction.startingPrice) },
                { label: 'Bước giá', value: formatMoney(auction.minIncrement) },
                { label: 'Người ký gửi', value: auction.sellerName }
              ].map((item) => (
                <div key={item.label}>
                  <dt className="text-[9px] font-semibold uppercase tracking-[0.18em] text-slate-500">{item.label}</dt>
                  <dd className="mt-1 text-sm font-semibold text-slate-200">{item.value}</dd>
                </div>
              ))}
            </dl>

            <div className="mt-5 grid gap-3 border-t border-white/[0.06] pt-5 sm:grid-cols-2">
              <div className="flex items-center gap-2 text-xs text-slate-400">
                <Clock size={14} className="text-cyanx-300" />
                Mở cửa {formatDateTime(auction.startTimeUtc)}
              </div>
              <div className="flex items-center gap-2 text-xs text-slate-400">
                <Lock size={14} className="text-gold-300" />
                Kết thúc {formatDateTime(auction.endTimeUtc)}
              </div>
            </div>
          </div>

          <div className="panel p-6 sm:p-7">
            <div className="flex flex-wrap items-center justify-between gap-3">
              <div>
                <p className="eyebrow">Diễn biến giá</p>
                <h2 className="heading mt-1 text-lg">Lịch sử đấu giá đã xác thực</h2>
              </div>
              <span className="inline-flex items-center gap-1.5 rounded-full border border-white/10 bg-white/[0.03] px-3 py-1.5 text-[11px] text-slate-400">
                <History size={12} /> Hiển thị {bids.length} · tổng {auction.bidCount}
              </span>
            </div>

            <div className="mt-5">
              <PriceChart bids={bids} startingPrice={auction.startingPrice} />
            </div>

            <div className="mt-5 overflow-hidden rounded-xl border border-white/[0.07]">
              <div className="max-h-[340px] overflow-y-auto">
                <table className="w-full">
                  <thead className="sticky top-0 z-10 bg-ink-850/95 backdrop-blur">
                    <tr>
                      <th className="th">Người đặt giá</th>
                      <th className="th">Số tiền</th>
                      <th className="th">Thời điểm</th>
                      <th className="th text-right">Trạng thái</th>
                    </tr>
                  </thead>
                  <tbody>
                    <AnimatePresence initial={false}>
                      {bids.map((bid) => {
                        const winning = bid.isWinning || (ended && highest?.id === bid.id)
                        return (
                          <motion.tr
                            key={bid.id}
                            layout
                            initial={{ opacity: 0, backgroundColor: 'rgba(212,175,55,0.16)' }}
                            animate={{ opacity: 1, backgroundColor: 'rgba(212,175,55,0)' }}
                            transition={{ duration: 0.9 }}
                            className={`border-t border-white/[0.05] ${flashBid === bid.id ? 'bg-emerald-400/[0.06]' : ''}`}
                          >
                            <td className="td">
                              <div className="flex items-center gap-2.5">
                                <span className="grid h-7 w-7 place-items-center rounded-full bg-white/[0.06] text-[10px] font-bold text-slate-300">
                                  {bid.bidderName.slice(0, 2).toUpperCase()}
                                </span>
                                <span className="font-medium text-slate-200">
                                  {bid.bidderName}
                                  {bid.bidderId === user?.id ? (
                                    <span className="ml-2 rounded-full border border-cyanx-400/30 bg-cyanx-400/10 px-1.5 py-0.5 text-[9px] font-semibold uppercase tracking-wide text-cyanx-300">
                                      bạn
                                    </span>
                                  ) : null}
                                </span>
                              </div>
                            </td>
                            <td className="td font-display font-semibold tabular-nums text-gold-300">
                              {formatMoney(bid.amount)}
                            </td>
                            <td className="td text-xs text-slate-500">{timeAgo(bid.placedAtUtc)}</td>
                            <td className="td text-right">
                              {winning ? (
                                <span className="inline-flex items-center gap-1 rounded-full border border-gold-500/30 bg-gold-500/10 px-2 py-0.5 text-[10px] font-semibold text-gold-200">
                                  <Trophy size={10} /> Đang thắng
                                </span>
                              ) : (
                                <span className="text-[11px] text-slate-600">#{bid.sequence}</span>
                              )}
                            </td>
                          </motion.tr>
                        )
                      })}
                    </AnimatePresence>
                  </tbody>
                </table>
                {bids.length === 0 ? (
                  <div className="px-5 py-10 text-center">
                    <Gavel size={20} className="mx-auto text-slate-600" />
                    <p className="mt-3 text-sm font-medium text-slate-400">Chưa có lượt đặt giá</p>
                    <p className="mt-1 text-xs text-slate-600">Giá mở đầu là {formatMoney(auction.minimumNextBid)}.</p>
                  </div>
                ) : null}
              </div>
            </div>
          </div>
        </div>

        <aside className="space-y-5 lg:sticky lg:top-24 lg:self-start">
          <div className="panel overflow-hidden">
            <div className="border-b border-white/[0.06] bg-gradient-to-br from-gold-500/[0.08] via-transparent to-cyanx-400/[0.05] px-6 py-5">
              <div className="flex items-center justify-between gap-3">
                <p className="text-[10px] font-semibold uppercase tracking-[0.2em] text-slate-500">
                  {ended ? 'Giá chốt' : 'Giá hiện tại'}
                </p>
                {isActive ? (
                  <span className="inline-flex items-center gap-1.5 rounded-full border border-emerald-400/25 bg-emerald-400/10 px-2.5 py-1 text-[10px] font-semibold text-emerald-300">
                    <Dot /> Trực tiếp
                  </span>
                ) : (
                  <span className={`rounded-full border px-2.5 py-1 text-[10px] font-semibold uppercase tracking-wide ${statusClass(auction.status)}`}>
                    {statusLabel(auction.status)}
                  </span>
                )}
              </div>

              <motion.p
                key={auction.currentPrice}
                initial={{ opacity: 0, y: -6 }}
                animate={{ opacity: 1, y: 0 }}
                className="mt-2 font-display text-4xl font-semibold tabular-nums tracking-tight text-gold-200"
              >
                {formatMoney(auction.currentPrice)}
              </motion.p>

              <div className="mt-3 flex flex-wrap items-center gap-x-4 gap-y-1.5 text-[11px] text-slate-400">
                <span>
                  Giá hợp lệ kế tiếp <span className="font-semibold text-slate-200">{formatMoney(auction.minimumNextBid)}</span>
                </span>
                <span className="text-slate-700">·</span>
                <span className="inline-flex items-center gap-1">
                  <Users size={11} /> {auction.bidCount} lượt · {auction.participantCount} người đặt giá
                </span>
              </div>
            </div>

            <div className="px-6 py-5">
              {isActive ? (
                <>
                  <p className="mb-2 text-[10px] font-semibold uppercase tracking-[0.18em] text-slate-500">Thời gian còn lại</p>
                  <Countdown
                    endTimeUtc={auction.endTimeUtc}
                    serverOffsetMs={serverOffset}
                    variant="panel"
                    onEnded={scheduleRefresh}
                  />
                </>
              ) : ended ? (
                <div className="rounded-xl border border-white/[0.08] bg-white/[0.02] p-4 text-center">
                  {closedWithoutWinner ? (
                    <>
                      <p className="text-sm font-semibold text-slate-300">Kết thúc không có người thắng</p>
                      <p className="mt-1 text-xs text-slate-500">
                        Không có lượt đặt giá nào được xác thực trước khi hết giờ.
                      </p>
                    </>
                  ) : (
                    <>
                      <Trophy size={20} className="mx-auto text-gold-300" />
                      <p className="mt-2 text-xs uppercase tracking-[0.16em] text-slate-500">Người thắng cuộc</p>
                      <p className="mt-1 font-display text-lg font-semibold text-white">
                        {result?.winnerName ?? auction.winner?.displayName ?? auction.highestBidderName ?? '—'}
                      </p>
                      <p className="mt-1 text-sm text-gold-300">
                        {formatMoney(result?.winningAmount ?? auction.winner?.winningAmount ?? auction.currentPrice)}
                      </p>
                    </>
                  )}
                  {auction.endedAtUtc ? (
                    <p className="mt-2 text-[10px] text-slate-600">Kết thúc {formatDateTime(auction.endedAtUtc)}</p>
                  ) : null}
                </div>
              ) : (
                <div className="rounded-xl border border-cyanx-400/20 bg-cyanx-400/[0.05] p-4">
                  <p className="text-sm font-semibold text-cyanx-200">Phiên đấu giá sắp mở</p>
                  <p className="mt-1 text-xs text-slate-400">
                    {formatDateTime(auction.startTimeUtc)} — hệ thống tự động mở đặt giá đúng giờ.
                  </p>
                </div>
              )}

              {isActive ? (
                <form onSubmit={submitBid} className="mt-5">
                  {!isAuthenticated ? (
                    <div className="rounded-xl border border-white/[0.08] bg-white/[0.02] p-4">
                      <p className="text-sm font-semibold text-white">Đăng nhập để đặt giá</p>
                      <p className="mt-1 text-xs text-slate-400">
                        Mỗi lượt đặt giá gắn với tài khoản đã xác thực và phiên đăng nhập được ký.
                      </p>
                      <Link
                        to="/login"
                        state={{ from: location.pathname }}
                        className="btn-gold mt-4 w-full !py-2.5 !text-xs"
                      >
                        <Lock size={13} /> Đăng nhập để đặt giá
                      </Link>
                    </div>
                  ) : isAdmin ? (
                    <div className="rounded-xl border border-amber-400/20 bg-amber-400/[0.05] p-4 text-xs text-amber-200">
                      Tài khoản quản trị chỉ điều hành sàn và không thể đặt giá.
                    </div>
                  ) : (
                    <>
                      <label className="label" htmlFor="bid-amount">
                        Giá bạn muốn đặt (USD)
                      </label>
                      <div className="relative">
                        <span className="pointer-events-none absolute left-4 top-1/2 -translate-y-1/2 text-sm font-semibold text-slate-500">
                          $
                        </span>
                        <input
                          id="bid-amount"
                          type="number"
                          inputMode="decimal"
                          step="0.01"
                          min={minimumNext}
                          value={amount}
                          onChange={(event) => setAmount(event.target.value)}
                          className="input !pl-8 font-display text-lg font-semibold tabular-nums"
                          placeholder={minimumNext.toFixed(2)}
                        />
                      </div>
                      <p className={`mt-2 text-[11px] ${amount && !amountValid ? 'text-rose-300' : 'text-slate-500'}`}>
                        Giá hợp lệ tối thiểu là {formatMoney(minimumNext)} (bước giá {formatMoney(increment)}).
                      </p>

                      <div className="mt-3 grid grid-cols-2 gap-2">
                        {quickAmounts.map((quick) => (
                          <button
                            key={quick.label}
                            type="button"
                            onClick={() => setAmount(quick.value.toFixed(2))}
                            className="rounded-lg border border-white/10 bg-white/[0.03] px-3 py-2 text-[11px] font-semibold text-slate-300 transition hover:border-gold-500/40 hover:text-gold-200"
                          >
                            <span className="block text-[9px] uppercase tracking-wide text-slate-500">{quick.label}</span>
                            {formatMoney(quick.value)}
                          </button>
                        ))}
                      </div>

                      <button
                        type="submit"
                        disabled={placing || !amountValid}
                        className="btn-gold mt-4 w-full"
                      >
                        {placing ? <Loader2 size={16} className="animate-spin" /> : <Zap size={16} />}
                        {placing ? 'Đang xác thực bằng gRPC…' : 'Đặt giá đã xác thực'}
                      </button>

                      <p className="mt-3 flex items-start gap-1.5 text-[10px] leading-relaxed text-slate-600">
                        <ShieldCheck size={12} className="mt-0.5 shrink-0 text-emerald-400/70" />
                        Lượt đặt giá của bạn phải qua xác thực gRPC độc lập, giao dịch cơ sở dữ liệu tuần tự và
                        nhật ký kiểm toán bất biến trước khi trở thành giá dẫn đầu.
                      </p>
                    </>
                  )}
                </form>
              ) : null}
            </div>
          </div>

          <div className="panel p-5">
            <div className="flex items-center justify-between">
              <h2 className="text-xs font-semibold uppercase tracking-[0.16em] text-slate-500">Người tham gia phiên</h2>
              <span className="inline-flex items-center gap-1 text-[11px] text-slate-500">
                <Radio size={11} /> {auction.participantCount}
              </span>
            </div>

            {participants.length === 0 ? (
              <p className="mt-4 text-xs text-slate-600">
                Chưa có ai đặt giá. Lượt đặt giá hợp lệ đầu tiên sẽ dẫn đầu.
              </p>
            ) : (
              <ul className="mt-4 space-y-3">
                {participants.slice(0, 6).map((person, index) => (
                  <li key={person.userId} className="flex items-center justify-between gap-3">
                    <div className="flex min-w-0 items-center gap-2.5">
                      <span className="grid h-8 w-8 shrink-0 place-items-center rounded-full border border-white/10 bg-white/[0.04] text-[10px] font-bold text-slate-300">
                        {person.displayName.slice(0, 2).toUpperCase()}
                      </span>
                      <div className="min-w-0">
                        <p className="truncate text-xs font-semibold text-slate-200">
                          {person.displayName}
                          {person.userId === user?.id ? <span className="ml-1.5 text-[9px] text-cyanx-300">bạn</span> : null}
                        </p>
                        <p className="text-[10px] text-slate-500">
                          {person.bidCount} lượt đặt giá
                          {person.lastBidAtUtc ? ` · ${timeAgo(person.lastBidAtUtc)}` : ''}
                        </p>
                      </div>
                    </div>
                    {index === 0 ? (
                      <span className="shrink-0 rounded-full border border-gold-500/30 bg-gold-500/10 px-2 py-0.5 text-[9px] font-semibold uppercase tracking-wide text-gold-200">
                        Dẫn đầu
                      </span>
                    ) : null}
                  </li>
                ))}
              </ul>
            )}

            {isParticipant ? (
              <p className="mt-4 flex items-center gap-1.5 rounded-lg border border-emerald-400/20 bg-emerald-400/[0.05] px-3 py-2 text-[10px] text-emerald-300">
                <Sparkles size={11} /> Bạn đã tham gia phiên đấu giá này.
              </p>
            ) : null}
          </div>

          <div className="rounded-2xl border border-white/[0.06] bg-ink-950/50 p-5">
            <div className="flex items-center gap-2 text-[11px] text-slate-500">
              <Activity size={13} className="text-cyanx-300" />
              Kênh thời gian thực
            </div>
            <p className="mt-2 text-xs leading-relaxed text-slate-400">
              {hubStatus === 'connected'
                ? 'Giá và sự kiện đặt giá được đẩy từ hub đấu giá ngay khi giao dịch hoàn tất.'
                : hubStatus === 'reconnecting'
                  ? 'Đang thiết lập lại kết nối thời gian thực. Lịch sử đấu giá vẫn tra cứu được qua REST.'
                  : 'Kênh thời gian thực không khả dụng. Hãy tải lại lô đấu giá để đồng bộ với sổ đấu giá.'}
            </p>
          </div>

          <Link to="/auctions?status=active" className="btn-ghost w-full !py-2.5 !text-xs">
            <Gavel size={13} /> Xem các phiên trực tiếp khác
          </Link>
        </aside>
      </div>
    </div>
  )
}

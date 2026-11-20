import { useState } from 'react'
import { Link } from 'react-router-dom'
import { motion, useMotionValue, useSpring, useTransform } from 'framer-motion'
import { ArrowUpRight, Clock3, ImageOff, TrendingUp, Users } from 'lucide-react'
import { mediaUrl } from '../lib/api'
import { formatMoney, statusClass, statusLabel } from '../lib/format'
import type { AuctionSummary } from '../lib/types'
import { Countdown } from './Countdown'

export function AuctionCard({ auction, index = 0 }: { auction: AuctionSummary; index?: number }) {
  const [imageFailed, setImageFailed] = useState(false)
  const rotateX = useMotionValue(0)
  const rotateY = useMotionValue(0)
  const springX = useSpring(rotateX, { stiffness: 180, damping: 22 })
  const springY = useSpring(rotateY, { stiffness: 180, damping: 22 })
  const imageLift = useTransform(springY, (value) => value * 0.6)

  const handleMove = (event: React.MouseEvent<HTMLDivElement>) => {
    const rect = event.currentTarget.getBoundingClientRect()
    const px = (event.clientX - rect.left) / rect.width - 0.5
    const py = (event.clientY - rect.top) / rect.height - 0.5
    rotateY.set(px * 10)
    rotateX.set(py * -10)
  }

  const reset = () => {
    rotateX.set(0)
    rotateY.set(0)
  }

  return (
    <motion.div
      initial={{ opacity: 0, y: 18 }}
      whileInView={{ opacity: 1, y: 0 }}
      viewport={{ once: true, margin: '-60px' }}
      transition={{ duration: 0.45, delay: Math.min(index * 0.06, 0.3), ease: [0.22, 1, 0.36, 1] }}
      style={{ perspective: 1200 }}
    >
      <motion.div
        onMouseMove={handleMove}
        onMouseLeave={reset}
        style={{ rotateX: springX, rotateY: springY, transformStyle: 'preserve-3d' }}
        className="group relative flex h-full flex-col overflow-hidden rounded-2xl border border-white/[0.08] bg-ink-850/70 shadow-card transition-colors duration-300 hover:border-gold-500/30"
      >
        <Link to={`/auctions/${auction.id}`} className="flex h-full flex-col">
          <div className="relative aspect-[4/3] overflow-hidden bg-gradient-to-br from-ink-800 to-ink-950">
            {auction.imageUrl && !imageFailed ? (
              <motion.img
                src={mediaUrl(auction.imageUrl)}
                alt={auction.productName}
                loading="lazy"
                onError={() => setImageFailed(true)}
                style={{ translateZ: imageLift }}
                className="h-full w-full scale-[1.04] object-cover transition-transform duration-700 group-hover:scale-[1.1]"
              />
            ) : (
              <div className="grid h-full place-items-center text-slate-600">
                <ImageOff size={32} />
              </div>
            )}

            <div className="absolute inset-0 bg-gradient-to-t from-ink-950 via-ink-950/20 to-transparent opacity-90" />

            <div className="absolute inset-x-3 top-3 flex items-center justify-between gap-2">
              <span
                className={`inline-flex items-center gap-1.5 rounded-full border px-2.5 py-1 text-[10px] font-semibold uppercase tracking-[0.12em] backdrop-blur-md ${statusClass(
                  auction.status
                )}`}
              >
                {auction.status === 'active' ? (
                  <span className="h-1.5 w-1.5 animate-pulse rounded-full bg-emerald-400" />
                ) : null}
                {statusLabel(auction.status)}
              </span>
              <span className="rounded-full border border-white/10 bg-ink-950/60 px-2.5 py-1 text-[10px] font-medium uppercase tracking-[0.12em] text-slate-300 backdrop-blur-md">
                {auction.category}
              </span>
            </div>

            <div className="absolute inset-x-3 bottom-3 flex items-end justify-between gap-2">
              <Countdown endTimeUtc={auction.endTimeUtc} serverOffsetMs={0} />
              <span className="inline-flex items-center gap-1 rounded-full border border-white/10 bg-ink-950/65 px-2.5 py-1 text-[10px] font-semibold text-slate-300 backdrop-blur-md">
                <Users size={11} /> {auction.bidCount} lượt đặt giá
              </span>
            </div>
          </div>

          <div className="flex flex-1 flex-col p-5" style={{ transform: 'translateZ(20px)' }}>
            <p className="text-[10px] font-semibold uppercase tracking-[0.2em] text-cyanx-300/70">
              Lô · {auction.productName.slice(0, 34)}
              {auction.productName.length > 34 ? '…' : ''}
            </p>
            <h3 className="heading mt-1.5 line-clamp-2 text-base leading-snug transition-colors group-hover:text-gold-200">
              {auction.title}
            </h3>

            <div className="mt-4 flex items-end justify-between gap-3">
              <div>
                <p className="text-[9px] font-semibold uppercase tracking-[0.18em] text-slate-500">
                  {auction.status === 'ended' ? 'Giá chốt' : 'Giá hiện tại'}
                </p>
                <p className="font-display text-xl font-semibold tabular-nums text-gold-300">
                  {formatMoney(auction.currentPrice)}
                </p>
              </div>
              <div className="text-right">
                <p className="text-[9px] font-semibold uppercase tracking-[0.18em] text-slate-500">Giá kế tiếp</p>
                <p className="text-sm font-semibold tabular-nums text-slate-300">
                  {formatMoney(auction.minimumNextBid)}
                </p>
              </div>
            </div>

            <div className="mt-4 flex items-center justify-between border-t border-white/[0.06] pt-3.5">
              <div className="min-w-0">
                <p className="truncate text-[11px] text-slate-500">
                  {auction.highestBidderName ? (
                    <>
                      Dẫn đầu · <span className="text-slate-300">{auction.highestBidderName}</span>
                    </>
                  ) : (
                    <>Giá khởi điểm {formatMoney(auction.startingPrice)}</>
                  )}
                </p>
              </div>
              <span className="inline-flex items-center gap-1 text-[11px] font-semibold text-gold-300 transition group-hover:gap-2">
                Xem chi tiết <ArrowUpRight size={13} />
              </span>
            </div>
          </div>
        </Link>
      </motion.div>
    </motion.div>
  )
}

export function AuctionCardSkeleton() {
  return (
    <div className="overflow-hidden rounded-2xl border border-white/[0.06] bg-ink-850/50">
      <div className="skeleton aspect-[4/3] w-full rounded-none" />
      <div className="space-y-3 p-5">
        <div className="skeleton h-2.5 w-24" />
        <div className="skeleton h-4 w-4/5" />
        <div className="skeleton h-4 w-3/5" />
        <div className="flex justify-between pt-2">
          <div className="skeleton h-6 w-24" />
          <div className="skeleton h-6 w-16" />
        </div>
      </div>
    </div>
  )
}

export function AuctionGridSkeleton({ count = 6 }: { count?: number }) {
  return (
    <div className="grid gap-6 sm:grid-cols-2 lg:grid-cols-3">
      {Array.from({ length: count }).map((_, index) => (
        <AuctionCardSkeleton key={index} />
      ))}
    </div>
  )
}

export function EmptyState({
  title,
  message,
  action
}: {
  title: string
  message: string
  action?: React.ReactNode
}) {
  return (
    <div className="panel flex flex-col items-center justify-center px-6 py-16 text-center">
      <span className="grid h-12 w-12 place-items-center rounded-full border border-white/10 bg-white/[0.04] text-slate-500">
        <Clock3 size={20} />
      </span>
      <h3 className="heading mt-4 text-lg">{title}</h3>
      <p className="muted mt-2 max-w-sm">{message}</p>
      {action ? <div className="mt-5">{action}</div> : null}
    </div>
  )
}

export function TrendPill({ value }: { value: string }) {
  return (
    <span className="inline-flex items-center gap-1 rounded-full border border-emerald-400/20 bg-emerald-400/[0.07] px-2 py-0.5 text-[10px] font-semibold text-emerald-300">
      <TrendingUp size={10} /> {value}
    </span>
  )
}

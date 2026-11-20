import { Suspense, lazy, useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { motion } from 'framer-motion'
import {
  ArrowRight,
  BadgeCheck,
  Gavel,
  Gem,
  Radio,
  ShieldCheck,
  Sparkles,
  Trophy,
  UserPlus
} from 'lucide-react'
import { AuctionCard, AuctionGridSkeleton, EmptyState } from '../components/AuctionCard'
import { auctionsApi, productsApi, toApiError } from '../lib/api'
import { formatCompact } from '../lib/format'
import type { AuctionStats, AuctionSummary } from '../lib/types'
import { useToast } from '../context/ToastContext'
import { SceneShell } from '../three/SceneShell'

const HeroScene = lazy(() => import('../three/HeroScene'))

const steps = [
  {
    icon: UserPlus,
    title: 'Tạo tài khoản',
    body: 'Đăng ký trong vài giây. Phiên JWT giữ cho mọi lượt đặt giá của bạn an toàn trên mọi thiết bị.'
  },
  {
    icon: Gavel,
    title: 'Đặt giá an tâm',
    body: 'Mỗi lượt đặt giá được một dịch vụ gRPC độc lập xác thực, sau đó ghi trong một giao dịch tuần tự.'
  },
  {
    icon: Trophy,
    title: 'Thắng & sở hữu',
    body: 'Khi đồng hồ điểm hết giờ, lượt đặt giá hợp lệ cao nhất tự động thắng và kết quả được kiểm toán vĩnh viễn.'
  }
]

export default function Home() {
  const [ending, setEnding] = useState<AuctionSummary[]>([])
  const [fresh, setFresh] = useState<AuctionSummary[]>([])
  const [stats, setStats] = useState<AuctionStats | null>(null)
  const [categories, setCategories] = useState<string[]>([])
  const [loading, setLoading] = useState(true)
  const toast = useToast()

  useEffect(() => {
    let cancelled = false

    ;(async () => {
      try {
        const [endingResult, freshResult, statsResult, categoryResult] = await Promise.all([
          auctionsApi.list({ status: 'active', sort: 'ending', pageSize: 6 }),
          auctionsApi.list({ status: 'active', sort: 'newest', pageSize: 3 }),
          auctionsApi.stats(),
          productsApi.categories()
        ])

        if (cancelled) return
        setEnding(endingResult.items)
        setFresh(freshResult.items)
        setStats(statsResult)
        setCategories(categoryResult)
      } catch (error) {
        if (!cancelled) toast.error('Không thể tải danh mục đấu giá', toApiError(error).message)
      } finally {
        if (!cancelled) setLoading(false)
      }
    })()

    return () => {
      cancelled = true
    }
  }, [toast])

  return (
    <div>
      {/* Hero */}
      <section className="relative overflow-hidden pb-16 pt-10 sm:pt-16 lg:pb-24">
        <div className="pointer-events-none absolute inset-0 -z-10">
          <div className="absolute left-1/2 top-0 h-[560px] w-[880px] -translate-x-1/2 rounded-full bg-[radial-gradient(ellipse_at_center,rgba(34,211,238,0.10),transparent_65%)]" />
          <div className="absolute inset-x-0 top-0 h-px bg-gradient-to-r from-transparent via-white/10 to-transparent" />
        </div>

        <div className="container-lux grid items-center gap-10 lg:grid-cols-[1.05fr_0.95fr]">
          <div>
            <motion.div
              initial={{ opacity: 0, y: 12 }}
              animate={{ opacity: 1, y: 0 }}
              transition={{ duration: 0.5 }}
              className="inline-flex items-center gap-2 rounded-full border border-gold-500/25 bg-gold-500/[0.07] px-3.5 py-1.5"
            >
              <Radio size={13} className="text-gold-300" />
              <span className="text-[11px] font-semibold uppercase tracking-[0.18em] text-gold-200">
                Nhà đấu giá trực tiếp · {stats?.activeAuctions ?? 0} phiên đang mở
              </span>
            </motion.div>

            <motion.h1
              initial={{ opacity: 0, y: 18 }}
              animate={{ opacity: 1, y: 0 }}
              transition={{ duration: 0.6, delay: 0.05 }}
              className="heading mt-6 text-balance text-4xl leading-[1.05] sm:text-5xl lg:text-6xl"
            >
              Sở hữu
              <span className="bg-gold-gradient bg-clip-text text-transparent"> điều phi thường</span>
              <span className="block text-slate-300/90">trước khi đồng hồ ngừng chạy.</span>
            </motion.h1>

            <motion.p
              initial={{ opacity: 0, y: 18 }}
              animate={{ opacity: 1, y: 0 }}
              transition={{ duration: 0.6, delay: 0.12 }}
              className="muted mt-6 max-w-xl text-[15px]"
            >
              Aurelia là sàn đấu giá tuyển chọn dành cho đồng hồ quý, nghệ thuật, xe sưu tầm và những vật phẩm
              có giá trị đặc biệt. Đặt giá theo thời gian thực, theo dõi thị trường và biết chắc mọi kết quả đều
              đã được xác thực.
            </motion.p>

            <motion.div
              initial={{ opacity: 0, y: 18 }}
              animate={{ opacity: 1, y: 0 }}
              transition={{ duration: 0.6, delay: 0.18 }}
              className="mt-8 flex flex-wrap items-center gap-3"
            >
              <Link to="/auctions?status=active" className="btn-gold">
                Khám phá đấu giá trực tiếp <ArrowRight size={16} />
              </Link>
              <Link to="/auctions?status=scheduled" className="btn-ghost">
                Xem lô sắp diễn ra
              </Link>
            </motion.div>

            <motion.div
              initial={{ opacity: 0 }}
              animate={{ opacity: 1 }}
              transition={{ duration: 0.8, delay: 0.28 }}
              className="mt-10 grid max-w-lg grid-cols-3 gap-4 border-t border-white/[0.07] pt-6"
            >
              {[
                { value: stats ? formatCompact(stats.totalBids) : '—', label: 'Lượt đặt giá đã xác thực' },
                { value: stats ? formatCompact(stats.totalBidders) : '—', label: 'Người đấu giá đã đăng ký' },
                { value: stats ? formatCompact(stats.activeAuctions) : '—', label: 'Phiên trực tiếp' }
              ].map((item) => (
                <div key={item.label}>
                  <p className="stat-value">{item.value}</p>
                  <p className="mt-1 text-[10px] font-semibold uppercase tracking-[0.16em] text-slate-500">
                    {item.label}
                  </p>
                </div>
              ))}
            </motion.div>
          </div>

          <motion.div
            initial={{ opacity: 0, scale: 0.96 }}
            animate={{ opacity: 1, scale: 1 }}
            transition={{ duration: 0.8, delay: 0.1 }}
            className="relative"
          >
            <div className="absolute -inset-6 -z-10 rounded-[3rem] bg-[radial-gradient(circle_at_60%_40%,rgba(212,175,55,0.14),transparent_60%)] blur-2xl" />
            <div className="relative h-[380px] overflow-hidden rounded-[2rem] border border-white/10 bg-ink-900/50 shadow-card sm:h-[460px] lg:h-[520px]">
              <Suspense
                fallback={
                  <div className="flex h-full flex-col items-center justify-center gap-4">
                    <div className="skeleton h-40 w-40 rounded-full" />
                    <div className="skeleton h-3 w-32" />
                    <p className="text-[11px] uppercase tracking-[0.2em] text-slate-600">Đang chuẩn bị sân khấu 3D</p>
                  </div>
                }
              >
                <SceneShell alt="Búa đấu giá vàng 3D" cameraPosition={[0, 2.4, 6.4]}>
                  <HeroScene />
                </SceneShell>
              </Suspense>

              <div className="pointer-events-none absolute inset-x-4 bottom-4 flex items-center justify-between gap-3 rounded-xl border border-white/10 bg-ink-950/60 px-4 py-3 backdrop-blur-md">
                <div>
                  <p className="text-[9px] font-semibold uppercase tracking-[0.2em] text-slate-500">Búa nhà đấu giá</p>
                  <p className="font-display text-xs font-semibold text-gold-200">Kéo để xoay · vàng 18k</p>
                </div>
                <span className="inline-flex items-center gap-1.5 rounded-full border border-emerald-400/25 bg-emerald-400/10 px-2.5 py-1 text-[10px] font-semibold text-emerald-300">
                  <span className="h-1.5 w-1.5 animate-pulse rounded-full bg-emerald-400" /> TRỰC TIẾP
                </span>
              </div>
            </div>
          </motion.div>
        </div>
      </section>

      {/* Trust strip */}
      <section className="border-y border-white/[0.06] bg-ink-900/40">
        <div className="container-lux grid gap-4 py-5 sm:grid-cols-2 lg:grid-cols-4">
          {[
            { icon: ShieldCheck, title: 'Xác thực độc lập', body: 'Mỗi lượt đặt giá đều qua gRPC' },
            { icon: BadgeCheck, title: 'Giao dịch tuần tự', body: 'Không chấp nhận trùng lặp' },
            { icon: Radio, title: 'SignalR thời gian thực', body: 'Giá được đẩy tức thời' },
            { icon: Gem, title: 'Kết quả được kiểm toán', body: 'Nhật ký bất biến' }
          ].map((item) => (
            <div key={item.title} className="flex items-center gap-3">
              <span className="grid h-9 w-9 shrink-0 place-items-center rounded-lg border border-white/10 bg-white/[0.03] text-gold-300">
                <item.icon size={16} />
              </span>
              <div>
                <p className="text-xs font-semibold text-slate-200">{item.title}</p>
                <p className="text-[11px] text-slate-500">{item.body}</p>
              </div>
            </div>
          ))}
        </div>
      </section>

      {/* Categories */}
      <section className="container-lux pt-14">
        <div className="flex flex-wrap items-end justify-between gap-4">
          <div>
            <p className="eyebrow">Khám phá nhà đấu giá</p>
            <h2 className="heading mt-2 text-2xl sm:text-3xl">Các bộ sưu tập tuyển chọn</h2>
          </div>
          <Link to="/auctions" className="text-xs font-semibold text-gold-300 transition hover:text-gold-200">
            Tất cả lô <ArrowRight size={13} className="inline" />
          </Link>
        </div>
        <div className="mt-6 flex flex-wrap gap-2.5">
          <Link to="/auctions" className="chip chip-active">
            <Sparkles size={12} /> Tất cả bộ sưu tập
          </Link>
          {categories.map((category) => (
            <Link
              key={category}
              to={`/auctions?category=${encodeURIComponent(category)}`}
              className="chip"
            >
              {category}
            </Link>
          ))}
        </div>
      </section>

      {/* Ending soon */}
      <section className="container-lux pt-12">
        <div className="flex flex-wrap items-end justify-between gap-4">
          <div>
            <p className="eyebrow">Sắp kết thúc</p>
            <h2 className="heading mt-2 text-2xl sm:text-3xl">Phiên đấu giá sắp kết thúc</h2>
            <p className="muted mt-2">Những phiên có đồng hồ đang chạy nhanh nhất.</p>
          </div>
          <Link to="/auctions?status=active&sort=ending" className="btn-ghost !py-2.5 !text-xs">
            Tất cả phiên trực tiếp <ArrowRight size={14} />
          </Link>
        </div>

        <div className="mt-7">
          {loading ? (
            <AuctionGridSkeleton count={6} />
          ) : ending.length === 0 ? (
            <EmptyState
              title="Hiện chưa có phiên đấu giá trực tiếp"
              message="Phiên tiếp theo đang được chuẩn bị. Hãy xem các lô sắp diễn ra để sẵn sàng."
              action={
                <Link to="/auctions?status=scheduled" className="btn-gold !py-2.5 !text-xs">
                  Xem lô sắp diễn ra
                </Link>
              }
            />
          ) : (
            <div className="grid gap-6 sm:grid-cols-2 lg:grid-cols-3">
              {ending.map((auction, index) => (
                <AuctionCard key={auction.id} auction={auction} index={index} />
              ))}
            </div>
          )}
        </div>
      </section>

      {/* New arrivals */}
      {!loading && fresh.length > 0 ? (
        <section className="container-lux pt-16">
          <div className="flex flex-wrap items-end justify-between gap-4">
            <div>
              <p className="eyebrow">Mới được ký gửi</p>
              <h2 className="heading mt-2 text-2xl sm:text-3xl">Vừa bước vào sàn đấu giá</h2>
            </div>
            <Link to="/auctions?sort=newest" className="text-xs font-semibold text-gold-300 transition hover:text-gold-200">
              Lô mới nhất <ArrowRight size={13} className="inline" />
            </Link>
          </div>
          <div className="mt-7 grid gap-6 sm:grid-cols-2 lg:grid-cols-3">
            {fresh.map((auction, index) => (
              <AuctionCard key={auction.id} auction={auction} index={index} />
            ))}
          </div>
        </section>
      ) : null}

      {/* How it works */}
      <section id="how-it-works" className="container-lux pt-20">
        <div className="panel relative overflow-hidden p-8 sm:p-12">
          <div className="pointer-events-none absolute -right-20 -top-24 h-72 w-72 rounded-full bg-gold-500/[0.07] blur-3xl" />
          <div className="max-w-2xl">
            <p className="eyebrow">Phương thức Aurelia</p>
            <h2 className="heading mt-2 text-2xl sm:text-3xl">Sàn đấu giá được thiết kế cho sự tin cậy</h2>
            <p className="muted mt-3">
              Ba dịch vụ, một lời cam kết: mức giá bạn thấy chính là mức giá hệ thống đã xác thực. Không có thủ
              thuật chờ đồng bộ, không để trình duyệt quyết định, không có kết quả nào bị bỏ sót.
            </p>
          </div>

          <div className="mt-10 grid gap-6 md:grid-cols-3">
            {steps.map((step, index) => (
              <div key={step.title} className="relative rounded-2xl border border-white/[0.07] bg-ink-900/50 p-6">
                <span className="absolute right-5 top-5 font-display text-4xl font-semibold text-white/[0.05]">
                  0{index + 1}
                </span>
                <span className="grid h-10 w-10 place-items-center rounded-xl border border-gold-500/25 bg-gold-500/10 text-gold-300">
                  <step.icon size={18} />
                </span>
                <h3 className="heading mt-4 text-base">{step.title}</h3>
                <p className="muted mt-2 text-[13px]">{step.body}</p>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* CTA */}
      <section className="container-lux pt-16">
        <div className="relative overflow-hidden rounded-3xl border border-gold-500/20 bg-gradient-to-br from-gold-500/[0.12] via-ink-900/60 to-cyanx-400/[0.08] px-6 py-12 text-center sm:px-12">
          <div className="pointer-events-none absolute inset-0 bg-[radial-gradient(circle_at_50%_0%,rgba(246,231,180,0.16),transparent_55%)]" />
          <h2 className="heading relative text-2xl sm:text-3xl">Thương vụ tiếp theo của bạn đang chờ trên sàn.</h2>
          <p className="muted relative mx-auto mt-3 max-w-xl">
            Đăng ký miễn phí, theo dõi một phiên và đặt lượt giá được xác thực đầu tiên trong chưa đầy một phút.
          </p>
          <div className="relative mt-7 flex flex-wrap justify-center gap-3">
            <Link to="/register" className="btn-gold">
              Tạo tài khoản <ArrowRight size={16} />
            </Link>
            <Link to="/auctions" className="btn-ghost">
              Xem danh mục
            </Link>
          </div>
        </div>
      </section>
    </div>
  )
}

import { useEffect, useState } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { ChevronLeft, ChevronRight, Search, SlidersHorizontal, X } from 'lucide-react'
import { AuctionCard, AuctionGridSkeleton, EmptyState } from '../components/AuctionCard'
import { auctionsApi, productsApi, toApiError } from '../lib/api'
import type { AuctionSummary } from '../lib/types'
import { useToast } from '../context/ToastContext'

const statuses = [
  { value: 'all', label: 'Tất cả lô' },
  { value: 'active', label: 'Đang diễn ra' },
  { value: 'scheduled', label: 'Sắp diễn ra' },
  { value: 'ended', label: 'Kết quả' }
]

const sorts = [
  { value: 'ending', label: 'Sắp kết thúc nhất' },
  { value: 'newest', label: 'Mới nhất' },
  { value: 'price', label: 'Giá cao nhất' },
  { value: 'price-asc', label: 'Giá thấp nhất' }
]

export default function Auctions() {
  const [params, setParams] = useSearchParams()
  const toast = useToast()

  const status = params.get('status') ?? 'all'
  const category = params.get('category') ?? 'all'
  const sort = params.get('sort') ?? 'ending'
  const q = params.get('q') ?? ''
  const page = Math.max(1, Number(params.get('page') ?? '1') || 1)

  const [search, setSearch] = useState(q)
  const [items, setItems] = useState<AuctionSummary[]>([])
  const [total, setTotal] = useState(0)
  const [totalPages, setTotalPages] = useState(1)
  const [categories, setCategories] = useState<string[]>([])
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    setSearch(q)
  }, [q])

  useEffect(() => {
    let cancelled = false
    ;(async () => {
      try {
        const result = await productsApi.categories()
        if (!cancelled) setCategories(result)
      } catch {
        /* Bộ lọc vẫn dùng được nếu danh mục tải thất bại. */
      }
    })()
    return () => {
      cancelled = true
    }
  }, [])

  useEffect(() => {
    let cancelled = false
    setLoading(true)

    ;(async () => {
      try {
        const result = await auctionsApi.list({
          status,
          category,
          sort,
          q,
          page,
          pageSize: 9
        })
        if (cancelled) return
        setItems(result.items)
        setTotal(result.total)
        setTotalPages(Math.max(1, result.totalPages))
      } catch (error) {
        if (!cancelled) toast.error('Không thể tải danh sách đấu giá', toApiError(error).message)
      } finally {
        if (!cancelled) setLoading(false)
      }
    })()

    return () => {
      cancelled = true
    }
  }, [status, category, sort, q, page, toast])

  useEffect(() => {
    if (search === q) return
    const timer = window.setTimeout(() => {
      updateParams({ q: search || null, page: null }, true)
    }, 380)
    return () => window.clearTimeout(timer)
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [search])

  const updateParams = (updates: Record<string, string | null>, replace = false) => {
    const next = new URLSearchParams(params)
    for (const [key, value] of Object.entries(updates)) {
      if (value === null || value === '' || value === 'all') next.delete(key)
      else next.set(key, value)
    }
    setParams(next, { replace })
  }

  const activeFilters = [status !== 'all', category !== 'all', Boolean(q)].filter(Boolean).length

  return (
    <div className="container-lux py-10 sm:py-14">
      <div className="flex flex-wrap items-end justify-between gap-5">
        <div>
          <p className="eyebrow">Danh mục</p>
          <h1 className="heading mt-2 text-3xl sm:text-4xl">Các phiên đấu giá</h1>
          <p className="muted mt-2 max-w-xl">
            {loading ? 'Đang tra cứu sổ đấu giá…' : `${total} lô phù hợp với bộ lọc của bạn.`}
          </p>
        </div>
        <Link to="/auctions?status=active" className="btn-gold !py-2.5 !text-xs">
          Chuyển tới phiên trực tiếp
        </Link>
      </div>

      <div className="mt-8 rounded-2xl border border-white/[0.08] bg-ink-900/50 p-4 backdrop-blur-sm sm:p-5">
        <div className="flex flex-wrap items-center gap-2">
          <span className="mr-1 inline-flex items-center gap-1.5 text-[11px] font-semibold uppercase tracking-[0.14em] text-slate-500">
            <SlidersHorizontal size={13} /> Trạng thái
          </span>
          {statuses.map((item) => (
            <button
              key={item.value}
              type="button"
              onClick={() => updateParams({ status: item.value === 'all' ? null : item.value, page: null })}
              className={`chip ${status === item.value ? 'chip-active' : ''}`}
            >
              {item.label}
            </button>
          ))}
        </div>

        <div className="mt-4 grid gap-3 md:grid-cols-[1fr_auto_auto]">
          <div className="relative">
            <Search size={15} className="pointer-events-none absolute left-3.5 top-1/2 -translate-y-1/2 text-slate-500" />
            <input
              value={search}
              onChange={(event) => setSearch(event.target.value)}
              placeholder="Tìm lô, nhà chế tác, nguồn gốc…"
              className="input !pl-10"
              aria-label="Tìm kiếm đấu giá"
            />
            {search ? (
              <button
                type="button"
                onClick={() => setSearch('')}
                className="absolute right-3 top-1/2 -translate-y-1/2 text-slate-500 hover:text-white"
                aria-label="Xóa tìm kiếm"
              >
                <X size={14} />
              </button>
            ) : null}
          </div>

          <select
            value={category}
            onChange={(event) => updateParams({ category: event.target.value, page: null })}
            className="input md:w-48"
            aria-label="Lọc theo bộ sưu tập"
          >
            <option value="all">Tất cả bộ sưu tập</option>
            {categories.map((item) => (
              <option key={item} value={item}>
                {item}
              </option>
            ))}
          </select>

          <select
            value={sort}
            onChange={(event) => updateParams({ sort: event.target.value, page: null })}
            className="input md:w-48"
            aria-label="Sắp xếp đấu giá"
          >
            {sorts.map((item) => (
              <option key={item.value} value={item.value}>
                {item.label}
              </option>
            ))}
          </select>
        </div>

        {activeFilters > 0 ? (
          <div className="mt-3 flex flex-wrap items-center gap-2 text-[11px] text-slate-500">
            <span>{activeFilters} bộ lọc đang áp dụng</span>
            <button
              type="button"
              className="inline-flex items-center gap-1 rounded-full border border-white/10 px-2.5 py-1 transition hover:border-white/25 hover:text-white"
              onClick={() => {
                setSearch('')
                setParams(new URLSearchParams(), { replace: true })
              }}
            >
              <X size={10} /> Đặt lại bộ lọc
            </button>
          </div>
        ) : null}
      </div>

      <div className="mt-8">
        {loading ? (
          <AuctionGridSkeleton count={9} />
        ) : items.length === 0 ? (
          <EmptyState
            title="Không có lô nào trong chế độ xem này"
            message="Hãy thử bộ sưu tập khác, xóa từ khóa tìm kiếm hoặc xem các phiên đang diễn ra."
            action={
              <button type="button" className="btn-gold !py-2.5 !text-xs" onClick={() => setParams(new URLSearchParams())}>
                Đặt lại bộ lọc
              </button>
            }
          />
        ) : (
          <div className="grid gap-6 sm:grid-cols-2 lg:grid-cols-3">
            {items.map((auction, index) => (
              <AuctionCard key={auction.id} auction={auction} index={index} />
            ))}
          </div>
        )}
      </div>

      {!loading && totalPages > 1 ? (
        <div className="mt-10 flex items-center justify-center gap-3">
          <button
            type="button"
            disabled={page <= 1}
            onClick={() => updateParams({ page: String(page - 1) })}
            className="btn-ghost !px-3.5 !py-2.5"
            aria-label="Trang trước"
          >
            <ChevronLeft size={15} />
          </button>
          <span className="text-xs font-semibold text-slate-400">
            Trang <span className="text-white">{page}</span> / {totalPages}
          </span>
          <button
            type="button"
            disabled={page >= totalPages}
            onClick={() => updateParams({ page: String(page + 1) })}
            className="btn-ghost !px-3.5 !py-2.5"
            aria-label="Trang sau"
          >
            <ChevronRight size={15} />
          </button>
        </div>
      ) : null}
    </div>
  )
}

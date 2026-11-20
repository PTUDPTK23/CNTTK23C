import { useCallback, useEffect, useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import {
  AlertTriangle,
  CalendarClock,
  CheckCircle2,
  Gavel,
  Loader2,
  Plus,
  Square,
  TimerReset
} from 'lucide-react'
import { Modal } from '../../components/Modal'
import { auctionsApi, productsApi, toApiError } from '../../lib/api'
import { formatDateTime, formatMoney, statusClass, statusLabel, toLocalInputValue } from '../../lib/format'
import type { AuctionSummary, CreateAuctionInput, Product } from '../../lib/types'
import { useToast } from '../../context/ToastContext'

const statusFilters = [
  { value: 'all', label: 'Tất cả' },
  { value: 'active', label: 'Trực tiếp' },
  { value: 'scheduled', label: 'Sắp diễn ra' },
  { value: 'ended', label: 'Đã kết thúc' }
]

interface AuctionForm {
  productId: string
  title: string
  startPrice: string
  minIncrement: string
  startTime: string
  endTime: string
}

function defaultForm(): AuctionForm {
  const start = new Date(Date.now() + 60_000)
  const end = new Date(Date.now() + 24 * 60 * 60 * 1000)
  return {
    productId: '',
    title: '',
    startPrice: '',
    minIncrement: '100',
    startTime: toLocalInputValue(start),
    endTime: toLocalInputValue(end)
  }
}

export default function AdminAuctions() {
  const toast = useToast()
  const [items, setItems] = useState<AuctionSummary[]>([])
  const [total, setTotal] = useState(0)
  const [status, setStatus] = useState('all')
  const [page, setPage] = useState(1)
  const [loading, setLoading] = useState(true)

  const [products, setProducts] = useState<Product[]>([])
  const [formOpen, setFormOpen] = useState(false)
  const [form, setForm] = useState<AuctionForm>(defaultForm)
  const [saving, setSaving] = useState(false)
  const [formError, setFormError] = useState<string | null>(null)

  const [endTarget, setEndTarget] = useState<AuctionSummary | null>(null)
  const [ending, setEnding] = useState(false)

  const load = useCallback(async () => {
    setLoading(true)
    try {
      const result = await auctionsApi.list({ status, sort: 'newest', page, pageSize: 12 })
      setItems(result.items)
      setTotal(result.total)
    } catch (error) {
      toast.error('Không thể tải danh sách phiên đấu giá', toApiError(error).message)
    } finally {
      setLoading(false)
    }
  }, [status, page, toast])

  useEffect(() => {
    void load()
  }, [load])

  useEffect(() => {
    let cancelled = false
    ;(async () => {
      try {
        const result = await productsApi.list({ page: 1, pageSize: 100 })
        if (!cancelled) setProducts(result.items)
      } catch {
        /* Biểu mẫu sẽ hiển thị danh sách sản phẩm trống nếu tải thất bại. */
      }
    })()
    return () => {
      cancelled = true
    }
  }, [])

  const selectedProduct = useMemo(
    () => products.find((product) => product.id === form.productId) ?? null,
    [products, form.productId]
  )

  const openCreate = () => {
    setForm(defaultForm())
    setFormError(null)
    setFormOpen(true)
  }

  const submitForm = async (event: React.FormEvent) => {
    event.preventDefault()
    setFormError(null)

    if (!form.productId) return setFormError('Hãy chọn sản phẩm được đấu giá.')
    if (form.title.trim().length < 3) return setFormError('Tiêu đề phiên đấu giá phải có ít nhất 3 ký tự.')

    const startPrice = Number(form.startPrice)
    const increment = Number(form.minIncrement)
    if (!(startPrice > 0)) return setFormError('Giá khởi điểm phải lớn hơn 0.')
    if (!(increment > 0)) return setFormError('Bước giá tối thiểu phải lớn hơn 0.')

    const start = new Date(form.startTime)
    const end = new Date(form.endTime)
    if (Number.isNaN(start.getTime()) || Number.isNaN(end.getTime())) {
      return setFormError('Cần nhập thời gian mở và đóng phiên.')
    }
    if (end <= start) return setFormError('Phiên đấu giá phải kết thúc sau khi mở.')
    if (end.getTime() <= Date.now()) return setFormError('Thời gian kết thúc phải ở tương lai.')

    const input: CreateAuctionInput = {
      productId: form.productId,
      title: form.title.trim(),
      startPrice,
      minIncrement: increment,
      startTimeUtc: start.toISOString(),
      endTimeUtc: end.toISOString()
    }

    setSaving(true)
    try {
      const created = await auctionsApi.create(input)
      toast.success('Đã tạo phiên đấu giá', `${created.title} hiện ${statusLabel(created.status).toLowerCase()}.`)
      setFormOpen(false)
      setStatus('all')
      setPage(1)
      await load()
    } catch (error) {
      const message = toApiError(error).message
      setFormError(message)
      toast.error('Không thể tạo phiên đấu giá', message)
    } finally {
      setSaving(false)
    }
  }

  const confirmEnd = async () => {
    if (!endTarget) return
    setEnding(true)
    try {
      const result = await auctionsApi.end(endTarget.id)
      toast.success(
        'Đã đóng phiên đấu giá',
        result.hasWinner
          ? `${result.winnerName} thắng với giá ${formatMoney(result.winningAmount)}.`
          : 'Phiên đấu giá kết thúc mà không có người thắng.'
      )
      setEndTarget(null)
      await load()
    } catch (error) {
      toast.error('Không thể đóng phiên đấu giá', toApiError(error).message)
    } finally {
      setEnding(false)
    }
  }

  const totalPages = Math.max(1, Math.ceil(total / 12))

  return (
    <div className="space-y-5">
      <div className="flex flex-wrap items-end justify-between gap-4">
        <div>
          <p className="text-[11px] font-semibold uppercase tracking-[0.18em] text-cyanx-300/80">Chương trình bán</p>
          <h2 className="heading mt-1 text-xl">Phiên đấu giá</h2>
          <p className="muted mt-1 text-xs">
            {loading ? 'Đang tải chương trình bán…' : `${total} phiên đấu giá trong chế độ xem này.`}
          </p>
        </div>
        <button type="button" className="btn-gold !px-3.5 !py-2.5 !text-xs" onClick={openCreate}>
          <Plus size={14} /> Tạo phiên đấu giá
        </button>
      </div>

      <div className="flex flex-wrap gap-2">
        {statusFilters.map((filter) => (
          <button
            key={filter.value}
            type="button"
            onClick={() => {
              setStatus(filter.value)
              setPage(1)
            }}
            className={`chip ${status === filter.value ? 'chip-active' : ''}`}
          >
            {filter.label}
          </button>
        ))}
      </div>

      <div className="table-shell">
        <table className="w-full">
          <thead>
            <tr>
              <th className="th">Phiên đấu giá</th>
              <th className="th">Giá</th>
              <th className="th">Lượt đặt</th>
              <th className="th">Mở cửa</th>
              <th className="th">Kết thúc</th>
              <th className="th">Trạng thái</th>
              <th className="th text-right">Thao tác</th>
            </tr>
          </thead>
          <tbody>
            {items.map((auction) => (
              <tr key={auction.id} className="border-t border-white/[0.05] transition hover:bg-white/[0.02]">
                <td className="td max-w-[280px]">
                  <Link to={`/auctions/${auction.id}`} className="block truncate font-semibold text-slate-200 hover:text-gold-300">
                    {auction.title}
                  </Link>
                  <span className="text-[10px] text-slate-500">
                    {auction.productName} · {auction.category}
                  </span>
                </td>
                <td className="td font-display font-semibold tabular-nums text-gold-300">
                  {formatMoney(auction.currentPrice)}
                </td>
                <td className="td text-slate-400">{auction.bidCount}</td>
                <td className="td text-xs text-slate-500">{formatDateTime(auction.startTimeUtc)}</td>
                <td className="td text-xs text-slate-500">{formatDateTime(auction.endTimeUtc)}</td>
                <td className="td">
                  <span className={`rounded-full border px-2 py-0.5 text-[9px] font-semibold uppercase tracking-wide ${statusClass(auction.status)}`}>
                    {statusLabel(auction.status)}
                  </span>
                </td>
                <td className="td text-right">
                  {auction.status === 'active' || auction.status === 'scheduled' ? (
                    <button
                      type="button"
                      onClick={() => setEndTarget(auction)}
                      className="inline-flex items-center gap-1.5 rounded-lg border border-white/10 px-2.5 py-1.5 text-[10px] font-semibold text-slate-300 transition hover:border-rose-400/40 hover:text-rose-300"
                    >
                      <Square size={11} /> Kết thúc ngay
                    </button>
                  ) : (
                    <Link
                      to={`/auctions/${auction.id}`}
                      className="inline-flex items-center gap-1.5 rounded-lg border border-white/10 px-2.5 py-1.5 text-[10px] font-semibold text-slate-300 transition hover:border-gold-500/40 hover:text-gold-200"
                    >
                      <CheckCircle2 size={11} /> Kết quả
                    </Link>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>

        {loading ? (
          <div className="flex items-center justify-center gap-2 py-12 text-xs text-slate-500">
            <Loader2 size={14} className="animate-spin text-gold-300" /> Đang tải phiên đấu giá…
          </div>
        ) : items.length === 0 ? (
          <div className="px-6 py-14 text-center">
            <Gavel size={20} className="mx-auto text-slate-600" />
            <p className="mt-3 text-sm font-semibold text-slate-300">Không có phiên đấu giá trong chế độ xem này</p>
            <p className="mt-1 text-xs text-slate-500">Hãy tạo phiên bán từ một sản phẩm đã có để mở phòng đấu giá.</p>
          </div>
        ) : null}

        {totalPages > 1 ? (
          <div className="flex items-center justify-between border-t border-white/[0.06] px-4 py-3 text-[11px] text-slate-500">
            <span>{total} phiên đấu giá</span>
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

      <Modal
        open={formOpen}
        onClose={() => setFormOpen(false)}
        title="Tạo phiên đấu giá"
        description="Phiên được mở từ một sản phẩm ký gửi và tự động đóng theo thời gian đã hẹn."
        width="max-w-2xl"
      >
        <form onSubmit={submitForm} className="space-y-4">
          <div>
            <label className="label" htmlFor="auction-product">
              Sản phẩm
            </label>
            <select
              id="auction-product"
              className="input"
              value={form.productId}
              onChange={(event) => {
                const product = products.find((item) => item.id === event.target.value)
                setForm((current) => ({
                  ...current,
                  productId: event.target.value,
                  title: current.title || (product ? product.name : ''),
                  startPrice: current.startPrice || (product ? String(product.startingPrice) : '')
                }))
              }}
              required
            >
              <option value="">Chọn sản phẩm…</option>
              {products.map((product) => (
                <option key={product.id} value={product.id}>
                  {product.name} · {product.category} · {formatMoney(product.startingPrice)}
                </option>
              ))}
            </select>
            {selectedProduct ? (
              <p className="mt-1.5 line-clamp-2 text-[11px] text-slate-500">{selectedProduct.description}</p>
            ) : null}
          </div>

          <div>
            <label className="label" htmlFor="auction-title">
              Tiêu đề phiên đấu giá
            </label>
            <input
              id="auction-title"
              className="input"
              value={form.title}
              maxLength={180}
              onChange={(event) => setForm((current) => ({ ...current, title: event.target.value }))}
              placeholder="Đồng hồ Chronograph Sapphire Đêm — Số 07"
              required
            />
          </div>

          <div className="grid gap-4 sm:grid-cols-2">
            <div>
              <label className="label" htmlFor="auction-start-price">
                Giá khởi điểm (USD)
              </label>
              <input
                id="auction-start-price"
                type="number"
                step="0.01"
                min="0.01"
                className="input font-display font-semibold tabular-nums"
                value={form.startPrice}
                onChange={(event) => setForm((current) => ({ ...current, startPrice: event.target.value }))}
                required
              />
            </div>
            <div>
              <label className="label" htmlFor="auction-increment">
                Bước giá tối thiểu (USD)
              </label>
              <input
                id="auction-increment"
                type="number"
                step="0.01"
                min="0.01"
                className="input font-display font-semibold tabular-nums"
                value={form.minIncrement}
                onChange={(event) => setForm((current) => ({ ...current, minIncrement: event.target.value }))}
                required
              />
            </div>
          </div>

          <div className="grid gap-4 sm:grid-cols-2">
            <div>
              <label className="label" htmlFor="auction-start-time">
                Mở cửa (giờ địa phương)
              </label>
              <input
                id="auction-start-time"
                type="datetime-local"
                className="input"
                value={form.startTime}
                onChange={(event) => setForm((current) => ({ ...current, startTime: event.target.value }))}
                required
              />
            </div>
            <div>
              <label className="label" htmlFor="auction-end-time">
                Kết thúc (giờ địa phương)
              </label>
              <input
                id="auction-end-time"
                type="datetime-local"
                className="input"
                value={form.endTime}
                onChange={(event) => setForm((current) => ({ ...current, endTime: event.target.value }))}
                required
              />
            </div>
          </div>

          <div className="flex items-start gap-2 rounded-xl border border-white/[0.07] bg-white/[0.02] px-3.5 py-2.5 text-[11px] text-slate-500">
            <CalendarClock size={13} className="mt-0.5 shrink-0 text-cyanx-300" />
            Tiến trình vòng đời sẽ kích hoạt các phiên đã hẹn, đóng đúng giờ và tự động công bố người thắng qua
            RabbitMQ.
          </div>

          {formError ? (
            <p className="rounded-xl border border-rose-400/25 bg-rose-400/[0.07] px-3.5 py-2.5 text-xs text-rose-300">
              {formError}
            </p>
          ) : null}

          <div className="flex justify-end gap-2 pt-1">
            <button type="button" className="btn-ghost !py-2.5 !text-xs" onClick={() => setFormOpen(false)}>
              Hủy
            </button>
            <button type="submit" className="btn-gold !py-2.5 !text-xs" disabled={saving}>
              {saving ? <Loader2 size={14} className="animate-spin" /> : <Gavel size={14} />}
              Mở phiên đấu giá
            </button>
          </div>
        </form>
      </Modal>

      <Modal
        open={Boolean(endTarget)}
        onClose={() => setEndTarget(null)}
        title="Đóng phiên đấu giá ngay"
        description="Lượt đặt giá hợp lệ cao nhất sẽ thắng ngay lập tức."
      >
        <div className="space-y-4">
          <div className="flex items-start gap-3 rounded-xl border border-amber-400/20 bg-amber-400/[0.05] p-4">
            <AlertTriangle size={18} className="mt-0.5 shrink-0 text-amber-300" />
            <p className="text-xs leading-relaxed text-slate-300">
              <span className="font-semibold text-white">{endTarget?.title}</span> hiện ở mức{' '}
              {formatMoney(endTarget?.currentPrice)} với {endTarget?.bidCount ?? 0} lượt đặt giá. Thao tác đóng không
              thể hoàn tác.
            </p>
          </div>
          <div className="flex justify-end gap-2">
            <button type="button" className="btn-ghost !py-2.5 !text-xs" onClick={() => setEndTarget(null)}>
              Giữ mở phiên
            </button>
            <button type="button" className="btn-danger !py-2.5 !text-xs" onClick={() => void confirmEnd()} disabled={ending}>
              {ending ? <Loader2 size={14} className="animate-spin" /> : <TimerReset size={14} />}
              Đóng & xác định người thắng
            </button>
          </div>
        </div>
      </Modal>
    </div>
  )
}

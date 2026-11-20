import { useCallback, useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import {
  AlertTriangle,
  ImageOff,
  Loader2,
  Pencil,
  Plus,
  Search,
  Trash2
} from 'lucide-react'
import { Modal } from '../../components/Modal'
import { mediaUrl, productsApi, toApiError } from '../../lib/api'
import { formatDateTime, formatMoney } from '../../lib/format'
import type { CreateProductInput, Product } from '../../lib/types'
import { useToast } from '../../context/ToastContext'

const emptyForm: CreateProductInput = {
  name: '',
  description: '',
  category: '',
  imageUrl: '',
  startingPrice: 1000
}

const sampleImages = [
  '/media/watch.svg',
  '/media/ring.svg',
  '/media/car.svg',
  '/media/sculpture.svg',
  '/media/manuscript.svg',
  '/media/synth.svg',
  '/media/chair.svg',
  '/media/meteorite.svg',
  '/media/katana.svg',
  '/media/necklace.svg'
]

export default function AdminProducts() {
  const toast = useToast()
  const [items, setItems] = useState<Product[]>([])
  const [total, setTotal] = useState(0)
  const [search, setSearch] = useState('')
  const [query, setQuery] = useState('')
  const [page, setPage] = useState(1)
  const [loading, setLoading] = useState(true)

  const [formOpen, setFormOpen] = useState(false)
  const [editing, setEditing] = useState<Product | null>(null)
  const [form, setForm] = useState<CreateProductInput>(emptyForm)
  const [saving, setSaving] = useState(false)
  const [formError, setFormError] = useState<string | null>(null)

  const [deleteTarget, setDeleteTarget] = useState<Product | null>(null)
  const [deleting, setDeleting] = useState(false)

  const load = useCallback(async () => {
    setLoading(true)
    try {
      const result = await productsApi.list({ q: query || undefined, page, pageSize: 12 })
      setItems(result.items)
      setTotal(result.total)
    } catch (error) {
      toast.error('Không thể tải sản phẩm', toApiError(error).message)
    } finally {
      setLoading(false)
    }
  }, [query, page, toast])

  useEffect(() => {
    void load()
  }, [load])

  useEffect(() => {
    const timer = window.setTimeout(() => {
      setPage(1)
      setQuery(search.trim())
    }, 350)
    return () => window.clearTimeout(timer)
  }, [search])

  const openCreate = () => {
    setEditing(null)
    setForm(emptyForm)
    setFormError(null)
    setFormOpen(true)
  }

  const openEdit = (product: Product) => {
    setEditing(product)
    setForm({
      name: product.name,
      description: product.description,
      category: product.category,
      imageUrl: product.imageUrl,
      startingPrice: product.startingPrice
    })
    setFormError(null)
    setFormOpen(true)
  }

  const submitForm = async (event: React.FormEvent) => {
    event.preventDefault()
    setFormError(null)

    if (form.name.trim().length < 2) return setFormError('Tên sản phẩm phải có ít nhất 2 ký tự.')
    if (form.description.trim().length < 10) return setFormError('Vui lòng nhập mô tả dài ít nhất 10 ký tự.')
    if (form.category.trim().length < 2) return setFormError('Cần chọn bộ sưu tập / danh mục.')
    if (!(form.startingPrice > 0)) return setFormError('Giá khởi điểm phải lớn hơn 0.')

    setSaving(true)
    try {
      if (editing) {
        await productsApi.update(editing.id, form)
        toast.success('Đã cập nhật sản phẩm', `${form.name} đã được lưu.`)
      } else {
        await productsApi.create(form)
        toast.success('Đã tạo sản phẩm', `${form.name} đã được thêm vào danh mục.`)
      }
      setFormOpen(false)
      await load()
    } catch (error) {
      const message = toApiError(error).message
      setFormError(message)
      toast.error('Không thể lưu sản phẩm', message)
    } finally {
      setSaving(false)
    }
  }

  const confirmDelete = async () => {
    if (!deleteTarget) return
    setDeleting(true)
    try {
      await productsApi.remove(deleteTarget.id)
      toast.success('Đã xóa sản phẩm', `${deleteTarget.name} đã được gỡ khỏi danh mục.`)
      setDeleteTarget(null)
      await load()
    } catch (error) {
      toast.error('Không thể xóa sản phẩm', toApiError(error).message)
    } finally {
      setDeleting(false)
    }
  }

  const totalPages = Math.max(1, Math.ceil(total / 12))

  return (
    <div className="space-y-5">
      <div className="flex flex-wrap items-end justify-between gap-4">
        <div>
          <p className="text-[11px] font-semibold uppercase tracking-[0.18em] text-cyanx-300/80">Danh mục</p>
          <h2 className="heading mt-1 text-xl">Sản phẩm</h2>
          <p className="muted mt-1 text-xs">
            {loading ? 'Đang tải danh mục…' : `${total} sản phẩm đang được ký gửi tại sàn.`}
          </p>
        </div>
        <div className="flex items-center gap-2">
          <div className="relative">
            <Search size={14} className="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 text-slate-500" />
            <input
              value={search}
              onChange={(event) => setSearch(event.target.value)}
              placeholder="Tìm sản phẩm…"
              className="input !w-56 !py-2.5 !pl-9 !text-xs"
              aria-label="Tìm sản phẩm"
            />
          </div>
          <button type="button" className="btn-gold !px-3.5 !py-2.5 !text-xs" onClick={openCreate}>
            <Plus size={14} /> Sản phẩm mới
          </button>
        </div>
      </div>

      <div className="table-shell">
        <table className="w-full">
          <thead>
            <tr>
              <th className="th">Sản phẩm</th>
              <th className="th">Bộ sưu tập</th>
              <th className="th">Giá khởi điểm</th>
              <th className="th">Phiên đấu giá</th>
              <th className="th">Cập nhật</th>
              <th className="th text-right">Thao tác</th>
            </tr>
          </thead>
          <tbody>
            {items.map((product) => (
              <tr key={product.id} className="border-t border-white/[0.05] transition hover:bg-white/[0.02]">
                <td className="td">
                  <div className="flex items-center gap-3">
                    <span className="grid h-11 w-11 shrink-0 place-items-center overflow-hidden rounded-lg border border-white/10 bg-ink-900">
                      {product.imageUrl ? (
                        <img src={mediaUrl(product.imageUrl)} alt="" className="h-full w-full object-cover" />
                      ) : (
                        <ImageOff size={15} className="text-slate-600" />
                      )}
                    </span>
                    <div className="min-w-0 max-w-[260px]">
                      <p className="truncate font-semibold text-slate-200">{product.name}</p>
                      <p className="truncate text-[10px] text-slate-500">{product.description}</p>
                    </div>
                  </div>
                </td>
                <td className="td">
                  <span className="rounded-full border border-white/10 bg-white/[0.03] px-2 py-0.5 text-[10px] text-slate-400">
                    {product.category}
                  </span>
                </td>
                <td className="td font-display font-semibold tabular-nums text-gold-300">
                  {formatMoney(product.startingPrice)}
                </td>
                <td className="td text-slate-400">
                  {product.auctionCount > 0 ? (
                    <Link to="/admin/auctions" className="text-cyanx-300 hover:text-cyanx-200">
                      {product.auctionCount}
                    </Link>
                  ) : (
                    '0'
                  )}
                </td>
                <td className="td text-xs text-slate-500">{formatDateTime(product.updatedAtUtc)}</td>
                <td className="td text-right">
                  <div className="inline-flex items-center gap-1.5">
                    <button
                      type="button"
                      onClick={() => openEdit(product)}
                      className="rounded-lg border border-white/10 p-2 text-slate-400 transition hover:border-cyanx-400/40 hover:text-cyanx-300"
                      aria-label={`Sửa ${product.name}`}
                    >
                      <Pencil size={13} />
                    </button>
                    <button
                      type="button"
                      onClick={() => setDeleteTarget(product)}
                      className="rounded-lg border border-white/10 p-2 text-slate-400 transition hover:border-rose-400/40 hover:text-rose-300"
                      aria-label={`Xóa ${product.name}`}
                    >
                      <Trash2 size={13} />
                    </button>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>

        {loading ? (
          <div className="flex items-center justify-center gap-2 py-12 text-xs text-slate-500">
            <Loader2 size={14} className="animate-spin text-gold-300" /> Đang tải danh mục…
          </div>
        ) : items.length === 0 ? (
          <div className="px-6 py-14 text-center">
            <Search size={20} className="mx-auto text-slate-600" />
            <p className="mt-3 text-sm font-semibold text-slate-300">Không có sản phẩm phù hợp</p>
            <p className="mt-1 text-xs text-slate-500">Hãy xóa từ khóa tìm kiếm hoặc tạo một sản phẩm ký gửi mới.</p>
          </div>
        ) : null}

        {totalPages > 1 ? (
          <div className="flex items-center justify-between border-t border-white/[0.06] px-4 py-3 text-[11px] text-slate-500">
            <span>{total} sản phẩm</span>
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
        title={editing ? 'Sửa sản phẩm' : 'Tạo sản phẩm'}
        description="Sản phẩm là vật phẩm ký gửi; phiên đấu giá được tạo từ sản phẩm."
        width="max-w-2xl"
      >
        <form onSubmit={submitForm} className="space-y-4">
          <div className="grid gap-4 sm:grid-cols-2">
            <div>
              <label className="label" htmlFor="product-name">
                Tên sản phẩm
              </label>
              <input
                id="product-name"
                className="input"
                value={form.name}
                maxLength={160}
                onChange={(event) => setForm((current) => ({ ...current, name: event.target.value }))}
                required
              />
            </div>
            <div>
              <label className="label" htmlFor="product-category">
                Bộ sưu tập / danh mục
              </label>
              <input
                id="product-category"
                className="input"
                value={form.category}
                maxLength={60}
                onChange={(event) => setForm((current) => ({ ...current, category: event.target.value }))}
                placeholder="Đồng hồ, Nghệ thuật, Xe…"
                required
              />
            </div>
          </div>

          <div>
            <label className="label" htmlFor="product-description">
              Mô tả
            </label>
            <textarea
              id="product-description"
              className="input min-h-[110px] resize-y"
              value={form.description}
              maxLength={4000}
              onChange={(event) => setForm((current) => ({ ...current, description: event.target.value }))}
              required
            />
          </div>

          <div className="grid gap-4 sm:grid-cols-[1fr_170px]">
            <div>
              <label className="label" htmlFor="product-image">
                URL hình ảnh hoặc đường dẫn media
              </label>
              <input
                id="product-image"
                className="input"
                value={form.imageUrl}
                maxLength={1024}
                onChange={(event) => setForm((current) => ({ ...current, imageUrl: event.target.value }))}
                placeholder="/media/watch.svg"
              />
              <div className="mt-2 flex flex-wrap gap-1.5">
                {sampleImages.map((path) => (
                  <button
                    key={path}
                    type="button"
                    onClick={() => setForm((current) => ({ ...current, imageUrl: path }))}
                    className="rounded-md border border-white/10 bg-white/[0.03] px-2 py-1 text-[9px] text-slate-400 transition hover:border-gold-500/40 hover:text-gold-200"
                  >
                    {path.replace('/media/', '').replace('.svg', '')}
                  </button>
                ))}
              </div>
            </div>
            <div>
              <label className="label" htmlFor="product-price">
                Giá khởi điểm (USD)
              </label>
              <input
                id="product-price"
                type="number"
                step="0.01"
                min="0.01"
                className="input font-display font-semibold tabular-nums"
                value={form.startingPrice}
                onChange={(event) =>
                  setForm((current) => ({ ...current, startingPrice: Number(event.target.value) }))
                }
                required
              />
            </div>
          </div>

          {form.imageUrl ? (
            <div className="overflow-hidden rounded-xl border border-white/10 bg-ink-900">
              <img src={mediaUrl(form.imageUrl)} alt="Xem trước sản phẩm" className="h-36 w-full object-cover" />
            </div>
          ) : null}

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
              {saving ? <Loader2 size={14} className="animate-spin" /> : null}
              {editing ? 'Lưu thay đổi' : 'Tạo sản phẩm'}
            </button>
          </div>
        </form>
      </Modal>

      <Modal
        open={Boolean(deleteTarget)}
        onClose={() => setDeleteTarget(null)}
        title="Xóa sản phẩm"
        description="Thao tác này không thể hoàn tác."
      >
        <div className="space-y-4">
          <div className="flex items-start gap-3 rounded-xl border border-rose-400/20 bg-rose-400/[0.05] p-4">
            <AlertTriangle size={18} className="mt-0.5 shrink-0 text-rose-300" />
            <p className="text-xs leading-relaxed text-slate-300">
              <span className="font-semibold text-white">{deleteTarget?.name}</span> sẽ bị gỡ khỏi danh mục. Không thể
              xóa sản phẩm đã có phiên đấu giá.
            </p>
          </div>
          <div className="flex justify-end gap-2">
            <button type="button" className="btn-ghost !py-2.5 !text-xs" onClick={() => setDeleteTarget(null)}>
              Hủy
            </button>
            <button type="button" className="btn-danger !py-2.5 !text-xs" onClick={() => void confirmDelete()} disabled={deleting}>
              {deleting ? <Loader2 size={14} className="animate-spin" /> : <Trash2 size={14} />}
              Xóa sản phẩm
            </button>
          </div>
        </div>
      </Modal>
    </div>
  )
}

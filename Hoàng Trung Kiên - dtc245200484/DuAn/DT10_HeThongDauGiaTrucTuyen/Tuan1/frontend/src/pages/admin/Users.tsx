import { useCallback, useEffect, useState } from 'react'
import { Loader2, Search, ShieldCheck, UserRound } from 'lucide-react'
import { adminApi, toApiError } from '../../lib/api'
import { formatDate } from '../../lib/format'
import type { AdminUser, Role } from '../../lib/types'
import { useAuth } from '../../context/AuthContext'
import { useToast } from '../../context/ToastContext'

export default function AdminUsers() {
  const { user: currentUser } = useAuth()
  const toast = useToast()

  const [items, setItems] = useState<AdminUser[]>([])
  const [total, setTotal] = useState(0)
  const [search, setSearch] = useState('')
  const [query, setQuery] = useState('')
  const [page, setPage] = useState(1)
  const [loading, setLoading] = useState(true)
  const [busyId, setBusyId] = useState<string | null>(null)

  const load = useCallback(async () => {
    setLoading(true)
    try {
      const result = await adminApi.users({ q: query || undefined, page, pageSize: 15 })
      setItems(result.items)
      setTotal(result.total)
    } catch (error) {
      toast.error('Không thể tải người dùng', toApiError(error).message)
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

  const changeRole = async (account: AdminUser, role: Role) => {
    setBusyId(account.id)
    try {
      const updated = await adminApi.updateRole(account.id, role)
      setItems((current) => current.map((item) => (item.id === updated.id ? updated : item)))
      toast.success(
        'Đã cập nhật vai trò',
        `${updated.displayName} hiện là ${updated.role === 'admin' ? 'Quản trị viên' : 'Người đấu giá'}.`
      )
    } catch (error) {
      toast.error('Không thể đổi vai trò', toApiError(error).message)
    } finally {
      setBusyId(null)
    }
  }

  const toggleStatus = async (account: AdminUser) => {
    setBusyId(account.id)
    try {
      const updated = await adminApi.updateStatus(account.id, !account.isActive)
      setItems((current) => current.map((item) => (item.id === updated.id ? updated : item)))
      toast.success(
        updated.isActive ? 'Đã mở khóa tài khoản' : 'Đã khóa tài khoản',
        `${updated.displayName} ${updated.isActive ? 'có thể' : 'không thể'} đăng nhập và đặt giá.`
      )
    } catch (error) {
      toast.error('Không thể đổi trạng thái tài khoản', toApiError(error).message)
    } finally {
      setBusyId(null)
    }
  }

  const totalPages = Math.max(1, Math.ceil(total / 15))

  return (
    <div className="space-y-5">
      <div className="flex flex-wrap items-end justify-between gap-4">
        <div>
          <p className="text-[11px] font-semibold uppercase tracking-[0.18em] text-cyanx-300/80">Thành viên</p>
          <h2 className="heading mt-1 text-xl">Người dùng &amp; phân quyền</h2>
          <p className="muted mt-1 text-xs">
            {loading ? 'Đang tải tài khoản…' : `${total} tài khoản đã đăng ký tại sàn.`}
          </p>
        </div>
        <div className="relative">
          <Search size={14} className="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 text-slate-500" />
          <input
            value={search}
            onChange={(event) => setSearch(event.target.value)}
            placeholder="Tìm tên hoặc email…"
            className="input !w-64 !py-2.5 !pl-9 !text-xs"
            aria-label="Tìm người dùng"
          />
        </div>
      </div>

      <div className="table-shell">
        <table className="w-full">
          <thead>
            <tr>
              <th className="th">Tài khoản</th>
              <th className="th">Vai trò</th>
              <th className="th">Lượt đặt giá</th>
              <th className="th">Tham gia</th>
              <th className="th">Trạng thái</th>
              <th className="th text-right">Thao tác</th>
            </tr>
          </thead>
          <tbody>
            {items.map((account) => {
              const isSelf = account.id === currentUser?.id
              const busy = busyId === account.id
              return (
                <tr key={account.id} className="border-t border-white/[0.05] transition hover:bg-white/[0.02]">
                  <td className="td">
                    <div className="flex items-center gap-3">
                      <span
                        className={`grid h-9 w-9 shrink-0 place-items-center rounded-full border text-[10px] font-bold ${
                          account.role === 'admin'
                            ? 'border-gold-500/30 bg-gold-500/10 text-gold-200'
                            : 'border-white/10 bg-white/[0.04] text-slate-300'
                        }`}
                      >
                        {account.displayName.slice(0, 2).toUpperCase()}
                      </span>
                      <div className="min-w-0">
                        <p className="flex items-center gap-1.5 text-sm font-semibold text-slate-200">
                          {account.displayName}
                          {isSelf ? (
                            <span className="rounded-full border border-cyanx-400/30 bg-cyanx-400/10 px-1.5 py-0.5 text-[9px] font-semibold uppercase tracking-wide text-cyanx-300">
                              bạn
                            </span>
                          ) : null}
                        </p>
                        <p className="truncate text-[10px] text-slate-500">{account.email}</p>
                      </div>
                    </div>
                  </td>
                  <td className="td">
                    {account.role === 'admin' ? (
                      <span className="inline-flex items-center gap-1 rounded-full border border-gold-500/30 bg-gold-500/10 px-2 py-0.5 text-[10px] font-semibold text-gold-200">
                        <ShieldCheck size={10} /> Quản trị
                      </span>
                    ) : (
                      <span className="inline-flex items-center gap-1 rounded-full border border-white/10 bg-white/[0.03] px-2 py-0.5 text-[10px] font-semibold text-slate-400">
                        <UserRound size={10} /> Người đấu giá
                      </span>
                    )}
                  </td>
                  <td className="td font-display tabular-nums text-slate-300">{account.bidCount}</td>
                  <td className="td text-xs text-slate-500">{formatDate(account.createdAtUtc)}</td>
                  <td className="td">
                    <span
                      className={`inline-flex items-center gap-1.5 rounded-full border px-2 py-0.5 text-[10px] font-semibold ${
                        account.isActive
                          ? 'border-emerald-400/25 bg-emerald-400/10 text-emerald-300'
                          : 'border-rose-400/25 bg-rose-400/10 text-rose-300'
                      }`}
                    >
                      <span className={`h-1.5 w-1.5 rounded-full ${account.isActive ? 'bg-emerald-400' : 'bg-rose-400'}`} />
                      {account.isActive ? 'Đang hoạt động' : 'Đã khóa'}
                    </span>
                  </td>
                  <td className="td text-right">
                    <div className="inline-flex items-center gap-2">
                      <select
                        value={account.role}
                        disabled={busy || isSelf}
                        onChange={(event) => void changeRole(account, event.target.value as Role)}
                        className="rounded-lg border border-white/10 bg-ink-900 px-2 py-1.5 text-[11px] text-slate-300 focus:border-cyanx-400/60 focus:outline-none disabled:opacity-40"
                        aria-label={`Vai trò của ${account.displayName}`}
                      >
                        <option value="bidder">Người đấu giá</option>
                        <option value="admin">Quản trị</option>
                      </select>
                      <button
                        type="button"
                        disabled={busy || isSelf}
                        onClick={() => void toggleStatus(account)}
                        className={`rounded-lg border px-2.5 py-1.5 text-[10px] font-semibold transition disabled:opacity-40 ${
                          account.isActive
                            ? 'border-white/10 text-slate-300 hover:border-rose-400/40 hover:text-rose-300'
                            : 'border-emerald-400/30 bg-emerald-400/[0.06] text-emerald-300 hover:bg-emerald-400/10'
                        }`}
                      >
                        {busy ? <Loader2 size={11} className="animate-spin" /> : account.isActive ? 'Khóa' : 'Mở khóa'}
                      </button>
                    </div>
                  </td>
                </tr>
              )
            })}
          </tbody>
        </table>

        {loading ? (
          <div className="flex items-center justify-center gap-2 py-12 text-xs text-slate-500">
            <Loader2 size={14} className="animate-spin text-gold-300" /> Đang tải tài khoản…
          </div>
        ) : items.length === 0 ? (
          <div className="px-6 py-14 text-center">
            <UserRound size={20} className="mx-auto text-slate-600" />
            <p className="mt-3 text-sm font-semibold text-slate-300">Không có tài khoản phù hợp</p>
            <p className="mt-1 text-xs text-slate-500">Hãy thử một phần tên hoặc email khác.</p>
          </div>
        ) : null}

        {totalPages > 1 ? (
          <div className="flex items-center justify-between border-t border-white/[0.06] px-4 py-3 text-[11px] text-slate-500">
            <span>{total} tài khoản</span>
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
        Sàn ngăn việc tự gỡ quyền quản trị của chính bạn, tự khóa tài khoản của mình, hoặc để nền tảng không còn
        quản trị viên nào.
      </p>
    </div>
  )
}

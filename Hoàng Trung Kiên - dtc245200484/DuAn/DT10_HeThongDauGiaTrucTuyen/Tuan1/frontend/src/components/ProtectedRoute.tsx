import type { ReactNode } from 'react'
import { Navigate, useLocation } from 'react-router-dom'
import { Loader2, ShieldAlert } from 'lucide-react'
import { useAuth } from '../context/AuthContext'

export function ProtectedRoute({
  children,
  requireAdmin = false
}: {
  children: ReactNode
  requireAdmin?: boolean
}) {
  const { user, initializing } = useAuth()
  const location = useLocation()

  if (initializing) {
    return (
      <div className="container-lux grid min-h-[60vh] place-items-center">
        <div className="flex items-center gap-3 text-slate-400">
          <Loader2 size={18} className="animate-spin text-gold-300" />
          <span className="text-sm">Đang xác thực phiên đăng nhập…</span>
        </div>
      </div>
    )
  }

  if (!user) {
    return <Navigate to="/login" replace state={{ from: location.pathname + location.search }} />
  }

  if (requireAdmin && user.role !== 'admin') {
    return (
      <div className="container-lux grid min-h-[60vh] place-items-center">
        <div className="panel max-w-md p-8 text-center">
          <span className="mx-auto grid h-12 w-12 place-items-center rounded-full border border-rose-400/30 bg-rose-400/10 text-rose-300">
            <ShieldAlert size={22} />
          </span>
          <h1 className="heading mt-4 text-xl">Chỉ dành cho quản trị viên</h1>
          <p className="muted mt-2">Tài khoản của bạn không có quyền mở bảng điều khiển quản trị.</p>
        </div>
      </div>
    )
  }

  return <>{children}</>
}

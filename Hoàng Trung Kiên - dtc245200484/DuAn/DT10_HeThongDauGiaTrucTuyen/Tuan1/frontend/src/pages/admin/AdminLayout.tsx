import { NavLink, Outlet, Link } from 'react-router-dom'
import { Gavel, History, LayoutDashboard, Package, ShieldCheck, Users } from 'lucide-react'
import { useAuth } from '../../context/AuthContext'

const links = [
  { to: '/admin', label: 'Bảng điều khiển', icon: LayoutDashboard, end: true },
  { to: '/admin/products', label: 'Sản phẩm', icon: Package, end: false },
  { to: '/admin/auctions', label: 'Phiên đấu giá', icon: Gavel, end: false },
  { to: '/admin/users', label: 'Người dùng', icon: Users, end: false },
  { to: '/admin/audit', label: 'Nhật ký kiểm toán', icon: History, end: false }
]

export default function AdminLayout() {
  const { user } = useAuth()

  return (
    <div className="container-lux py-8 sm:py-10">
      <div className="flex flex-wrap items-end justify-between gap-4">
        <div>
          <p className="eyebrow">Điều hành sàn</p>
          <h1 className="heading mt-2 text-3xl">Bảng điều khiển quản trị</h1>
          <p className="muted mt-1.5 text-[13px]">
            Đăng nhập với tên <span className="font-semibold text-slate-200">{user?.displayName}</span>. Mọi thao tác
            đều được sàn ghi nhận.
          </p>
        </div>
        <span className="inline-flex items-center gap-1.5 rounded-full border border-emerald-400/25 bg-emerald-400/[0.07] px-3 py-1.5 text-[10px] font-semibold uppercase tracking-[0.14em] text-emerald-300">
          <ShieldCheck size={12} /> Đã xác thực quyền quản trị
        </span>
      </div>

      <div className="mt-7 grid gap-6 lg:grid-cols-[220px_1fr]">
        <aside className="lg:sticky lg:top-24 lg:self-start">
          <nav className="flex gap-1.5 overflow-x-auto rounded-2xl border border-white/[0.07] bg-ink-900/60 p-1.5 lg:flex-col lg:overflow-visible">
            {links.map((link) => (
              <NavLink
                key={link.to}
                to={link.to}
                end={link.end}
                className={({ isActive }) =>
                  `inline-flex shrink-0 items-center gap-2.5 rounded-xl px-3.5 py-2.5 text-xs font-semibold transition ${
                    isActive
                      ? 'bg-gold-500/15 text-gold-200'
                      : 'text-slate-400 hover:bg-white/[0.04] hover:text-white'
                  }`
                }
              >
                <link.icon size={15} />
                {link.label}
              </NavLink>
            ))}
            <div className="hidden h-px w-full bg-white/[0.06] lg:block" />
            <Link
              to="/auctions"
              className="inline-flex shrink-0 items-center gap-2.5 rounded-xl px-3.5 py-2.5 text-xs font-semibold text-slate-500 transition hover:bg-white/[0.04] hover:text-white"
            >
              <Gavel size={15} /> Sàn công khai
            </Link>
          </nav>
        </aside>

        <section className="min-w-0">
          <Outlet />
        </section>
      </div>
    </div>
  )
}

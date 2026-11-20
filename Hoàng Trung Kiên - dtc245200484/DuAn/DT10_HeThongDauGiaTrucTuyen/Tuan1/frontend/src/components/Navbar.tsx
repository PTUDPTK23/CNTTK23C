import { useEffect, useState } from 'react'
import { Link, NavLink, useLocation, useNavigate } from 'react-router-dom'
import { AnimatePresence, motion } from 'framer-motion'
import { LayoutDashboard, LogOut, Menu, UserRound, X } from 'lucide-react'
import { useAuth } from '../context/AuthContext'
import { useToast } from '../context/ToastContext'
import { Logo } from './Logo'

const navigation = [
  { to: '/', label: 'Trang chủ', end: true },
  { to: '/auctions', label: 'Đấu giá', end: false }
]

export function Navbar() {
  const { user, isAuthenticated, isAdmin, logout } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const toast = useToast()
  const [scrolled, setScrolled] = useState(false)
  const [open, setOpen] = useState(false)

  useEffect(() => {
    const onScroll = () => setScrolled(window.scrollY > 16)
    onScroll()
    window.addEventListener('scroll', onScroll, { passive: true })
    return () => window.removeEventListener('scroll', onScroll)
  }, [])

  useEffect(() => {
    setOpen(false)
  }, [location.pathname, location.search])

  const handleLogout = async () => {
    await logout()
    toast.info('Đã đăng xuất', 'Phiên đăng nhập của bạn đã được xóa an toàn.')
    navigate('/')
  }

  const initials = user?.displayName
    ?.split(' ')
    .map((part) => part[0])
    .slice(0, 2)
    .join('')
    .toUpperCase()

  return (
    <header
      className={`fixed inset-x-0 top-0 z-[100] transition-all duration-300 ${
        scrolled ? 'border-b border-white/[0.07] bg-ink-950/85 backdrop-blur-2xl' : 'bg-transparent'
      }`}
    >
      <div className="container-lux flex h-20 items-center justify-between gap-6">
        <Logo />

        <nav className="hidden items-center gap-1 md:flex">
          {navigation.map((item) => (
            <NavLink
              key={item.to}
              to={item.to}
              end={item.end}
              className={({ isActive }) => `nav-link ${isActive ? 'nav-link-active' : ''}`}
            >
              {item.label}
              {item.to === '/auctions' ? (
                <span className="ml-2 inline-block h-1.5 w-1.5 rounded-full bg-emerald-400 align-middle shadow-[0_0_10px_2px_rgba(52,211,153,0.45)]" />
              ) : null}
            </NavLink>
          ))}
          <a href="/#how-it-works" className="nav-link">
            Cách hoạt động
          </a>
        </nav>

        <div className="hidden items-center gap-3 md:flex">
          {isAuthenticated && user ? (
            <>
              {isAdmin ? (
                <Link to="/admin" className="btn-ghost !px-4 !py-2.5">
                  <LayoutDashboard size={15} />
                  Quản trị
                </Link>
              ) : null}
              <Link
                to="/profile"
                className="flex items-center gap-2.5 rounded-xl border border-white/10 bg-white/[0.03] px-3 py-2 transition hover:border-white/20 hover:bg-white/[0.06]"
              >
                <span className="grid h-7 w-7 place-items-center rounded-full bg-gradient-to-br from-gold-300 to-gold-600 text-[11px] font-bold text-ink-950">
                  {initials || <UserRound size={13} />}
                </span>
                <span className="text-xs font-semibold text-slate-200">{user.displayName}</span>
              </Link>
              <button
                type="button"
                onClick={handleLogout}
                className="rounded-xl border border-white/10 p-2.5 text-slate-400 transition hover:border-rose-400/40 hover:text-rose-300"
                aria-label="Đăng xuất"
                title="Đăng xuất"
              >
                <LogOut size={16} />
              </button>
            </>
          ) : (
            <>
              <Link to="/login" className="btn-ghost !px-4 !py-2.5">
                Đăng nhập
              </Link>
              <Link to="/register" className="btn-gold !px-4 !py-2.5">
                Tạo tài khoản
              </Link>
            </>
          )}
        </div>

        <button
          type="button"
          className="rounded-xl border border-white/10 p-2.5 text-slate-200 md:hidden"
          onClick={() => setOpen((value) => !value)}
          aria-label={open ? 'Đóng menu' : 'Mở menu'}
          aria-expanded={open}
        >
          {open ? <X size={18} /> : <Menu size={18} />}
        </button>
      </div>

      <AnimatePresence>
        {open ? (
          <motion.div
            initial={{ opacity: 0, height: 0 }}
            animate={{ opacity: 1, height: 'auto' }}
            exit={{ opacity: 0, height: 0 }}
            transition={{ duration: 0.22 }}
            className="overflow-hidden border-t border-white/[0.07] bg-ink-950/95 backdrop-blur-2xl md:hidden"
          >
            <div className="container-lux flex flex-col gap-2 py-5">
              {navigation.map((item) => (
                <NavLink
                  key={item.to}
                  to={item.to}
                  end={item.end}
                  className={({ isActive }) =>
                    `rounded-xl px-3 py-3 text-sm font-medium ${
                      isActive ? 'bg-white/[0.06] text-white' : 'text-slate-400'
                    }`
                  }
                >
                  {item.label}
                </NavLink>
              ))}
              <a href="/#how-it-works" className="rounded-xl px-3 py-3 text-sm font-medium text-slate-400">
                Cách hoạt động
              </a>
              <div className="divider my-2" />
              {isAuthenticated && user ? (
                <>
                  <Link to="/profile" className="btn-ghost w-full">
                    <UserRound size={15} />
                    {user.displayName}
                  </Link>
                  {isAdmin ? (
                    <Link to="/admin" className="btn-ghost w-full">
                      <LayoutDashboard size={15} />
                      Bảng điều khiển quản trị
                    </Link>
                  ) : null}
                  <button type="button" className="btn-danger w-full" onClick={handleLogout}>
                    <LogOut size={15} />
                    Đăng xuất
                  </button>
                </>
              ) : (
                <>
                  <Link to="/login" className="btn-ghost w-full">
                    Đăng nhập
                  </Link>
                  <Link to="/register" className="btn-gold w-full">
                    Tạo tài khoản
                  </Link>
                </>
              )}
            </div>
          </motion.div>
        ) : null}
      </AnimatePresence>
    </header>
  )
}

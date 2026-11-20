import { useEffect, useState } from 'react'
import { Link, useLocation, useNavigate } from 'react-router-dom'
import { motion } from 'framer-motion'
import {
  ArrowRight,
  Eye,
  EyeOff,
  Gavel,
  Loader2,
  Lock,
  Mail,
  ShieldCheck,
  Sparkles,
  UserRound
} from 'lucide-react'
import { useAuth } from '../context/AuthContext'
import { useToast } from '../context/ToastContext'
import { toApiError } from '../lib/api'

const demos = [
  { label: 'Quản trị viên', email: 'admin@auction.local', password: 'Admin@123', tone: 'gold' },
  { label: 'Người đấu giá · Nguyễn Minh Anh', email: 'alice@auction.local', password: 'Bidder@123', tone: 'cyan' },
  { label: 'Người đấu giá · Trần Quốc Bảo', email: 'bob@auction.local', password: 'Bidder@123', tone: 'cyan' }
]

export default function Login() {
  const { login, isAuthenticated, user } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const toast = useToast()

  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [showPassword, setShowPassword] = useState(false)
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const from = (location.state as { from?: string } | null)?.from

  useEffect(() => {
    if (isAuthenticated && user) {
      navigate(from ?? (user.role === 'admin' ? '/admin' : '/profile'), { replace: true })
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [isAuthenticated, user])

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault()
    setError(null)
    setSubmitting(true)
    try {
      const profile = await login(email.trim(), password)
      toast.success('Chào mừng trở lại', `Bạn đã đăng nhập với tên ${profile.displayName}.`)
      navigate(from ?? (profile.role === 'admin' ? '/admin' : '/profile'), { replace: true })
    } catch (err) {
      const apiError = toApiError(err)
      setError(apiError.message)
      toast.error('Đăng nhập thất bại', apiError.message)
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <div className="container-lux grid min-h-[70vh] items-center gap-10 py-10 lg:grid-cols-[1.05fr_0.95fr] lg:py-16">
      <motion.div
        initial={{ opacity: 0, x: -16 }}
        animate={{ opacity: 1, x: 0 }}
        transition={{ duration: 0.5 }}
        className="hidden lg:block"
      >
        <span className="inline-flex items-center gap-2 rounded-full border border-gold-500/25 bg-gold-500/[0.07] px-3.5 py-1.5 text-[11px] font-semibold uppercase tracking-[0.18em] text-gold-200">
          <Sparkles size={12} /> Lối vào hội viên
        </span>
        <h1 className="heading mt-6 text-4xl leading-[1.08] xl:text-5xl">
          Sàn đấu giá ghi nhớ
          <span className="block bg-gold-gradient bg-clip-text text-transparent">mọi lượt đặt giá nghiêm túc.</span>
        </h1>
        <p className="muted mt-5 max-w-md">
          Đăng nhập để theo dõi các phiên trực tiếp, đặt giá được xác thực độc lập và ghi lại mọi thương vụ
          trong sổ cá nhân của bạn.
        </p>

        <div className="mt-9 space-y-4">
          {[
            { icon: ShieldCheck, title: 'Xác thực bằng gRPC', body: 'Một dịch vụ chuyên trách kiểm tra mọi lượt đặt giá trước khi ghi vào cơ sở dữ liệu.' },
            { icon: Gavel, title: 'Giao dịch tuần tự', body: 'Khóa dòng bảo đảm tại mỗi thời điểm chỉ có một lượt đặt giá dẫn đầu.' },
            { icon: UserRound, title: 'Sổ đặt giá riêng', body: 'Toàn bộ lịch sử, các phiên tham gia và chiến thắng nằm trong một hồ sơ.' }
          ].map((item) => (
            <div key={item.title} className="flex gap-3.5">
              <span className="mt-0.5 grid h-9 w-9 shrink-0 place-items-center rounded-lg border border-white/10 bg-white/[0.03] text-gold-300">
                <item.icon size={16} />
              </span>
              <div>
                <p className="text-sm font-semibold text-slate-200">{item.title}</p>
                <p className="mt-0.5 text-xs leading-relaxed text-slate-500">{item.body}</p>
              </div>
            </div>
          ))}
        </div>
      </motion.div>

      <motion.div
        initial={{ opacity: 0, y: 16 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ duration: 0.5, delay: 0.08 }}
        className="panel mx-auto w-full max-w-md p-7 sm:p-8"
      >
        <div className="flex items-center gap-2 text-[10px] font-semibold uppercase tracking-[0.22em] text-slate-500">
          <Lock size={12} className="text-gold-300" /> Phiên bảo mật
        </div>
        <h2 className="heading mt-3 text-2xl">Đăng nhập Aurelia</h2>
        <p className="muted mt-1.5 text-[13px]">Dùng tài khoản của bạn để bước vào sàn đấu giá.</p>

        <form onSubmit={handleSubmit} className="mt-7 space-y-4">
          <div>
            <label className="label" htmlFor="email">
              Địa chỉ email
            </label>
            <div className="relative">
              <Mail size={15} className="pointer-events-none absolute left-3.5 top-1/2 -translate-y-1/2 text-slate-500" />
              <input
                id="email"
                type="email"
                autoComplete="email"
                required
                value={email}
                onChange={(event) => setEmail(event.target.value)}
                className="input !pl-10"
                placeholder="ban@vidu.com"
              />
            </div>
          </div>

          <div>
            <div className="flex items-center justify-between">
              <label className="label" htmlFor="password">
                Mật khẩu
              </label>
              <span className="mb-2 text-[10px] text-slate-600">Tối thiểu 8 ký tự</span>
            </div>
            <div className="relative">
              <Lock size={15} className="pointer-events-none absolute left-3.5 top-1/2 -translate-y-1/2 text-slate-500" />
              <input
                id="password"
                type={showPassword ? 'text' : 'password'}
                autoComplete="current-password"
                required
                value={password}
                onChange={(event) => setPassword(event.target.value)}
                className="input !pl-10 !pr-11"
                placeholder="••••••••"
              />
              <button
                type="button"
                onClick={() => setShowPassword((value) => !value)}
                className="absolute right-3 top-1/2 -translate-y-1/2 rounded-md p-1 text-slate-500 transition hover:text-white"
                aria-label={showPassword ? 'Ẩn mật khẩu' : 'Hiện mật khẩu'}
              >
                {showPassword ? <EyeOff size={15} /> : <Eye size={15} />}
              </button>
            </div>
          </div>

          {error ? (
            <p className="rounded-xl border border-rose-400/25 bg-rose-400/[0.07] px-3.5 py-2.5 text-xs text-rose-300">
              {error}
            </p>
          ) : null}

          <button type="submit" disabled={submitting} className="btn-gold w-full">
            {submitting ? <Loader2 size={16} className="animate-spin" /> : <ArrowRight size={16} />}
            {submitting ? 'Đang mở sàn đấu giá…' : 'Đăng nhập'}
          </button>
        </form>

        <div className="mt-6">
          <div className="flex items-center gap-3">
            <span className="h-px flex-1 bg-white/[0.07]" />
            <span className="text-[10px] font-semibold uppercase tracking-[0.16em] text-slate-600">Tài khoản dùng thử</span>
            <span className="h-px flex-1 bg-white/[0.07]" />
          </div>
          <div className="mt-3 grid gap-2">
            {demos.map((demo) => (
              <button
                key={demo.email}
                type="button"
                onClick={() => {
                  setEmail(demo.email)
                  setPassword(demo.password)
                  setError(null)
                }}
                className="flex items-center justify-between rounded-xl border border-white/[0.08] bg-white/[0.02] px-3.5 py-2.5 text-left transition hover:border-gold-500/30 hover:bg-white/[0.04]"
              >
                <span>
                  <span className="block text-xs font-semibold text-slate-200">{demo.label}</span>
                  <span className="block text-[10px] text-slate-500">{demo.email}</span>
                </span>
                <span className={`text-[10px] font-semibold uppercase tracking-wide ${demo.tone === 'gold' ? 'text-gold-300' : 'text-cyanx-300'}`}>
                  Dùng
                </span>
              </button>
            ))}
          </div>
          <p className="mt-2 text-center text-[10px] leading-relaxed text-slate-600">
            Tài khoản dùng thử chỉ được tạo sẵn cho môi trường phát triển cục bộ.
          </p>
        </div>

        <p className="mt-6 text-center text-xs text-slate-500">
          Chưa có tài khoản?{' '}
          <Link to="/register" className="link-gold font-semibold">
            Tạo tài khoản
          </Link>
        </p>
      </motion.div>
    </div>
  )
}

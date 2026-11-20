import { useEffect, useMemo, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { motion } from 'framer-motion'
import {
  ArrowRight,
  Check,
  Eye,
  EyeOff,
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

export default function Register() {
  const { register, isAuthenticated, user } = useAuth()
  const navigate = useNavigate()
  const toast = useToast()

  const [displayName, setDisplayName] = useState('')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [accepted, setAccepted] = useState(false)
  const [showPassword, setShowPassword] = useState(false)
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (isAuthenticated && user) {
      navigate(user.role === 'admin' ? '/admin' : '/profile', { replace: true })
    }
  }, [isAuthenticated, user, navigate])

  const passwordChecks = useMemo(
    () => [
      { label: 'Ít nhất 8 ký tự', valid: password.length >= 8 },
      { label: 'Có chữ hoa và chữ thường', valid: /[a-z]/.test(password) && /[A-Z]/.test(password) },
      { label: 'Có chữ số hoặc ký tự đặc biệt', valid: /[0-9!@#$%^&*(),.?":{}|<>_\-+=]/.test(password) },
      { label: 'Mật khẩu trùng khớp', valid: password.length > 0 && password === confirmPassword }
    ],
    [password, confirmPassword]
  )

  const strength = passwordChecks.filter((check) => check.valid).length
  const canSubmit =
    displayName.trim().length >= 2 &&
    /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email.trim()) &&
    passwordChecks.every((check) => check.valid) &&
    accepted &&
    !submitting

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault()
    setError(null)

    if (!accepted) {
      setError('Vui lòng xác nhận rằng bạn đã hiểu cách thức đặt giá.')
      return
    }

    if (password !== confirmPassword) {
      setError('Hai mật khẩu không khớp nhau.')
      return
    }

    setSubmitting(true)
    try {
      const profile = await register(email.trim(), password, displayName.trim())
      toast.success('Đã tạo tài khoản', `Chào mừng bạn đến với Aurelia, ${profile.displayName}.`)
      navigate('/auctions?status=active')
    } catch (err) {
      const apiError = toApiError(err)
      setError(apiError.message)
      toast.error('Đăng ký thất bại', apiError.message)
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <div className="container-lux grid min-h-[70vh] items-center gap-10 py-10 lg:grid-cols-[0.95fr_1.05fr] lg:py-16">
      <motion.div
        initial={{ opacity: 0, y: 16 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ duration: 0.5 }}
        className="panel mx-auto w-full max-w-md p-7 sm:p-8"
      >
        <div className="flex items-center gap-2 text-[10px] font-semibold uppercase tracking-[0.22em] text-slate-500">
          <Sparkles size={12} className="text-gold-300" /> Hội viên
        </div>
        <h1 className="heading mt-3 text-2xl">Tạo tài khoản đấu giá</h1>
        <p className="muted mt-1.5 text-[13px]">
          Tham gia miễn phí. Bạn chỉ cần tài khoản khi sẵn sàng đặt giá.
        </p>

        <form onSubmit={handleSubmit} className="mt-7 space-y-4">
          <div>
            <label className="label" htmlFor="displayName">
              Tên hiển thị
            </label>
            <div className="relative">
              <UserRound size={15} className="pointer-events-none absolute left-3.5 top-1/2 -translate-y-1/2 text-slate-500" />
              <input
                id="displayName"
                type="text"
                autoComplete="name"
                required
                minLength={2}
                maxLength={80}
                value={displayName}
                onChange={(event) => setDisplayName(event.target.value)}
                className="input !pl-10"
                placeholder="Tên mọi người sẽ thấy trong phiên đấu giá"
              />
            </div>
          </div>

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
            <label className="label" htmlFor="password">
              Mật khẩu
            </label>
            <div className="relative">
              <Lock size={15} className="pointer-events-none absolute left-3.5 top-1/2 -translate-y-1/2 text-slate-500" />
              <input
                id="password"
                type={showPassword ? 'text' : 'password'}
                autoComplete="new-password"
                required
                minLength={8}
                maxLength={72}
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

            {password ? (
              <div className="mt-2.5 space-y-1.5">
                <div className="flex gap-1">
                  {[0, 1, 2, 3].map((index) => (
                    <span
                      key={index}
                      className={`h-1 flex-1 rounded-full transition ${
                        index < strength
                          ? strength < 2
                            ? 'bg-rose-400'
                            : strength < 4
                              ? 'bg-amber-400'
                              : 'bg-emerald-400'
                          : 'bg-white/10'
                      }`}
                    />
                  ))}
                </div>
                <div className="grid gap-1 sm:grid-cols-2">
                  {passwordChecks.map((check) => (
                    <span
                      key={check.label}
                      className={`inline-flex items-center gap-1.5 text-[10px] ${
                        check.valid ? 'text-emerald-300' : 'text-slate-600'
                      }`}
                    >
                      <Check size={10} className={check.valid ? 'opacity-100' : 'opacity-30'} />
                      {check.label}
                    </span>
                  ))}
                </div>
              </div>
            ) : null}
          </div>

          <div>
            <label className="label" htmlFor="confirmPassword">
              Nhập lại mật khẩu
            </label>
            <div className="relative">
              <Lock size={15} className="pointer-events-none absolute left-3.5 top-1/2 -translate-y-1/2 text-slate-500" />
              <input
                id="confirmPassword"
                type={showPassword ? 'text' : 'password'}
                autoComplete="new-password"
                required
                value={confirmPassword}
                onChange={(event) => setConfirmPassword(event.target.value)}
                className="input !pl-10"
                placeholder="Nhập lại mật khẩu"
              />
            </div>
          </div>

          <label className="flex cursor-pointer items-start gap-2.5 rounded-xl border border-white/[0.07] bg-white/[0.02] px-3.5 py-3">
            <input
              type="checkbox"
              checked={accepted}
              onChange={(event) => setAccepted(event.target.checked)}
              className="mt-0.5 h-4 w-4 rounded border-white/20 bg-ink-900 accent-gold-500"
            />
            <span className="text-[11px] leading-relaxed text-slate-400">
              Tôi hiểu rằng lượt đặt giá đã gửi là ràng buộc, được nhà đấu giá xác thực và ghi vào nhật ký kiểm
              toán vĩnh viễn.
            </span>
          </label>

          {error ? (
            <p className="rounded-xl border border-rose-400/25 bg-rose-400/[0.07] px-3.5 py-2.5 text-xs text-rose-300">
              {error}
            </p>
          ) : null}

          <button type="submit" disabled={!canSubmit} className="btn-gold w-full">
            {submitting ? <Loader2 size={16} className="animate-spin" /> : <ArrowRight size={16} />}
            {submitting ? 'Đang tạo tài khoản…' : 'Tạo tài khoản'}
          </button>
        </form>

        <p className="mt-6 text-center text-xs text-slate-500">
          Đã có tài khoản?{' '}
          <Link to="/login" className="link-gold font-semibold">
            Đăng nhập
          </Link>
        </p>
      </motion.div>

      <motion.div
        initial={{ opacity: 0, x: 16 }}
        animate={{ opacity: 1, x: 0 }}
        transition={{ duration: 0.5, delay: 0.08 }}
        className="hidden lg:block"
      >
        <span className="inline-flex items-center gap-2 rounded-full border border-cyanx-400/25 bg-cyanx-400/[0.07] px-3.5 py-1.5 text-[11px] font-semibold uppercase tracking-[0.18em] text-cyanx-200">
          <ShieldCheck size={12} /> Xây dựng trên sự tin cậy
        </span>
        <h2 className="heading mt-6 text-4xl leading-[1.08] xl:text-5xl">
          Một tài khoản.
          <span className="block bg-gold-gradient bg-clip-text text-transparent">Mọi phiên, mọi kết quả.</span>
        </h2>
        <p className="muted mt-5 max-w-md">
          Tham gia cùng các nhà sưu tập đang đấu giá đồng hồ, nghệ thuật, xe cổ và những vật phẩm giá trị. Tài
          khoản của bạn lưu lại mọi lượt đặt giá, chiến thắng và lịch sử kiểm toán.
        </p>

        <div className="mt-9 grid gap-3 sm:grid-cols-2">
          {[
            { title: 'Giá theo thời gian thực', body: 'SignalR đẩy mỗi lượt đặt giá được chấp nhận tới mọi màn hình đang mở.' },
            { title: 'An toàn khi đồng thời', body: 'Khóa dòng PostgreSQL ngăn việc chấp nhận trùng lặp.' },
            { title: 'Kiểm toán vĩnh viễn', body: 'Một tiến trình độc lập ghi lại từng sự kiện.' },
            { title: 'Kết quả minh bạch', body: 'Người thắng và giá chốt được công bố khi phiên kết thúc.' }
          ].map((item) => (
            <div key={item.title} className="rounded-2xl border border-white/[0.07] bg-ink-900/50 p-4">
              <p className="text-sm font-semibold text-slate-200">{item.title}</p>
              <p className="mt-1 text-xs leading-relaxed text-slate-500">{item.body}</p>
            </div>
          ))}
        </div>
      </motion.div>
    </div>
  )
}

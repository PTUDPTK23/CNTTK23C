import { Link } from 'react-router-dom'
import { ExternalLink, Gavel, Mail, ShieldCheck } from 'lucide-react'
import { API_BASE } from '../lib/api'

export function Footer() {
  return (
    <footer className="relative mt-24 border-t border-white/[0.06] bg-ink-950/70">
      <div className="pointer-events-none absolute inset-x-0 -top-px h-px bg-gradient-to-r from-transparent via-gold-500/40 to-transparent" />
      <div className="container-lux grid gap-12 py-16 md:grid-cols-[1.4fr_1fr_1fr_1fr]">
        <div>
          <div className="flex items-center gap-3">
            <span className="grid h-10 w-10 place-items-center rounded-xl border border-gold-500/40 bg-gold-500/10">
              <Gavel size={18} className="text-gold-300" />
            </span>
            <div>
              <p className="font-display text-base font-semibold tracking-[0.22em] text-white">AURELIA</p>
              <p className="text-[9px] uppercase tracking-[0.3em] text-slate-500">Nhà đấu giá trực tuyến</p>
            </div>
          </div>
          <p className="muted mt-5 max-w-sm">
            Sàn đấu giá tuyển chọn dành cho những vật phẩm đặc biệt. Đặt giá minh bạch, cập nhật giá theo
            thời gian thực và kết quả được kiểm toán — vận hành trên kiến trúc .NET phân tán.
          </p>
          <p className="mt-5 inline-flex items-center gap-2 rounded-full border border-emerald-400/20 bg-emerald-400/[0.06] px-3 py-1.5 text-[11px] font-medium text-emerald-300">
            <ShieldCheck size={13} />
            Mọi lượt đặt giá đều được xác thực và kiểm toán
          </p>
        </div>

        <div>
          <p className="text-[11px] font-semibold uppercase tracking-[0.2em] text-slate-500">Khám phá</p>
          <ul className="mt-4 space-y-3 text-sm text-slate-400">
            <li>
              <Link to="/auctions?status=active" className="transition hover:text-gold-300">
                Đấu giá trực tiếp
              </Link>
            </li>
            <li>
              <Link to="/auctions?status=scheduled" className="transition hover:text-gold-300">
                Sắp diễn ra
              </Link>
            </li>
            <li>
              <Link to="/auctions?status=ended" className="transition hover:text-gold-300">
                Kho kết quả
              </Link>
            </li>
            <li>
              <Link to="/auctions" className="transition hover:text-gold-300">
                Toàn bộ danh mục
              </Link>
            </li>
          </ul>
        </div>

        <div>
          <p className="text-[11px] font-semibold uppercase tracking-[0.2em] text-slate-500">Tài khoản</p>
          <ul className="mt-4 space-y-3 text-sm text-slate-400">
            <li>
              <Link to="/login" className="transition hover:text-gold-300">
                Đăng nhập
              </Link>
            </li>
            <li>
              <Link to="/register" className="transition hover:text-gold-300">
                Tạo tài khoản
              </Link>
            </li>
            <li>
              <Link to="/profile" className="transition hover:text-gold-300">
                Lượt đấu giá của tôi
              </Link>
            </li>
            <li>
              <Link to="/profile" className="transition hover:text-gold-300">
                Phiên đấu giá đã thắng
              </Link>
            </li>
          </ul>
        </div>

        <div>
          <p className="text-[11px] font-semibold uppercase tracking-[0.2em] text-slate-500">Dành cho lập trình viên</p>
          <ul className="mt-4 space-y-3 text-sm text-slate-400">
            <li>
              <a
                href={`${API_BASE}/swagger`}
                target="_blank"
                rel="noreferrer"
                className="inline-flex items-center gap-1.5 transition hover:text-gold-300"
              >
                REST API &amp; Swagger <ExternalLink size={12} />
              </a>
            </li>
            <li>
              <a href={`${API_BASE}/health`} target="_blank" rel="noreferrer" className="transition hover:text-gold-300">
                Tình trạng API
              </a>
            </li>
            <li className="inline-flex items-center gap-1.5 text-slate-500">
              <Mail size={13} /> hotro@aurelia.auction
            </li>
          </ul>
        </div>
      </div>

      <div className="border-t border-white/[0.05]">
        <div className="container-lux flex flex-col items-center justify-between gap-3 py-6 text-[11px] text-slate-500 sm:flex-row">
          <p>© {new Date().getFullYear()} Nhà đấu giá Aurelia. Dự án minh họa.</p>
          <p className="text-slate-600">
            ASP.NET Core · gRPC · RabbitMQ · SignalR · PostgreSQL · React &amp; Three.js
          </p>
        </div>
      </div>
    </footer>
  )
}

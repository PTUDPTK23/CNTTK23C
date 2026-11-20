import { Link } from 'react-router-dom'
import { Gavel } from 'lucide-react'

export function Logo({ compact = false }: { compact?: boolean }) {
  return (
    <Link to="/" className="group flex items-center gap-3" aria-label="Trang chủ Aurelia">
      <span className="relative grid h-10 w-10 place-items-center rounded-xl border border-gold-500/40 bg-gold-500/10 shadow-luxury transition group-hover:border-gold-400/70">
        <Gavel size={19} className="text-gold-300" />
        <span className="absolute -right-1 -top-1 h-1.5 w-1.5 rounded-full bg-cyanx-400 shadow-glow" />
      </span>
      {!compact ? (
        <span className="leading-tight">
          <span className="block font-display text-[17px] font-semibold tracking-[0.22em] text-white">
            AURELIA
          </span>
          <span className="block text-[8.5px] font-semibold uppercase tracking-[0.34em] text-slate-500">
            Nhà đấu giá trực tuyến
          </span>
        </span>
      ) : null}
    </Link>
  )
}

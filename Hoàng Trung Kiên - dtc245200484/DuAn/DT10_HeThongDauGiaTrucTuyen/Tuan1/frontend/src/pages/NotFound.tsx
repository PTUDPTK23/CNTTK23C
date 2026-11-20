import { Link } from 'react-router-dom'
import { ArrowLeft, Compass, Gavel, Search } from 'lucide-react'

export default function NotFound() {
  return (
    <div className="container-lux grid min-h-[60vh] place-items-center py-16">
      <div className="panel relative w-full max-w-xl overflow-hidden p-9 text-center">
        <div className="pointer-events-none absolute -right-14 -top-16 h-48 w-48 rounded-full bg-gold-500/[0.08] blur-3xl" />
        <span className="mx-auto grid h-14 w-14 place-items-center rounded-2xl border border-gold-500/30 bg-gold-500/10 text-gold-300">
          <Gavel size={24} />
        </span>
        <p className="eyebrow mt-6">404 · Không tìm thấy lô đấu giá</p>
        <h1 className="heading mt-2 text-3xl">Phiên đấu giá này không có trong chương trình</h1>
        <p className="muted mx-auto mt-3 max-w-sm">
          Trang bạn yêu cầu không tồn tại, hoặc lô hàng đã được rút khỏi danh mục.
        </p>
        <div className="mt-7 flex flex-wrap justify-center gap-3">
          <Link to="/auctions" className="btn-gold">
            <Compass size={15} /> Xem danh mục
          </Link>
          <Link to="/" className="btn-ghost">
            <ArrowLeft size={15} /> Về trang chủ
          </Link>
        </div>
        <p className="mt-6 inline-flex items-center gap-1.5 text-[11px] text-slate-600">
          <Search size={11} /> Đang tìm một lô cụ thể? Hãy dùng ô tìm kiếm trong danh mục.
        </p>
      </div>
    </div>
  )
}
